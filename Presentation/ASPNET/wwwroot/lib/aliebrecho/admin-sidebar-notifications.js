(function () {
    const resources = {
        orders: {
            endpoint: '/Order/GetOrderList',
            menuUrl: '/Orders/Order',
            eventName: 'OrderChanged',
            idProperty: 'orderId'
        },
        bags: {
            endpoint: '/Bag/GetBagList',
            menuUrl: '/Bags/Bag',
            eventName: 'BagChanged',
            idProperty: 'bagId'
        }
    };

    let initialized = false;
    let orderConnection = null;
    let messageConnection = null;
    const notificationOwnerKey = 'aliebrecho:admin-notifications:owner';

    const getNotificationOwner = () => {
        const storedOwner = localStorage.getItem(notificationOwnerKey);
        if (storedOwner) return storedOwner;

        const currentOwner = StorageManager.getUserId() || StorageManager.getEmail();
        if (currentOwner) {
            const normalizedOwner = String(currentOwner);
            localStorage.setItem(notificationOwnerKey, normalizedOwner);
            return normalizedOwner;
        }

        return 'anonymous';
    };

    const userStorageKey = resource =>
        `aliebrecho:admin-notifications:${getNotificationOwner()}:${resource}`;

    const readState = (resource) => {
        try {
            const stored = JSON.parse(localStorage.getItem(userStorageKey(resource)) || 'null');
            const knownIds = Array.isArray(stored?.knownIds) ? stored.knownIds.map(String) : null;
            const unreadIds = Array.isArray(stored?.unreadIds) ? stored.unreadIds.map(String) : [];
            const viewedIds = Array.isArray(stored?.viewedIds)
                ? stored.viewedIds.map(String)
                : (knownIds || []).filter(id => !unreadIds.includes(id));

            return { knownIds, unreadIds, viewedIds };
        } catch {
            return { knownIds: null, unreadIds: [], viewedIds: [] };
        }
    };

    const saveState = (resource, state) => {
        localStorage.setItem(userStorageKey(resource), JSON.stringify(state));
    };

    const updateBadge = (menuUrl, count) => {
        const badge = document.querySelector(
            `.app-sidebar__menu-item[data-menu-url="${menuUrl}"] [data-sidebar-unread-count]`);
        if (!badge) return;

        const normalizedCount = Math.max(0, Number(count) || 0);
        badge.textContent = normalizedCount > 99
            ? '99+'
            : (normalizedCount > 0 ? String(normalizedCount) : '');
    };

    const updateResourceBadge = (resource) => {
        const state = readState(resource);
        updateBadge(resources[resource].menuUrl, state.unreadIds.length);
    };

    const syncResource = async (resource) => {
        const config = resources[resource];
        const response = await AxiosManager.get(config.endpoint, {});
        const currentIds = (response?.data?.content?.data ?? [])
            .map(item => String(item.id || ''))
            .filter(Boolean);
        const currentIdSet = new Set(currentIds);
        const state = readState(resource);

        if (state.knownIds === null) {
            state.knownIds = currentIds;
            state.unreadIds = currentIds.filter(id => !state.viewedIds.includes(id));
        } else {
            const knownIdSet = new Set(state.knownIds);
            currentIds.forEach((id) => {
                if (!knownIdSet.has(id) &&
                    !state.unreadIds.includes(id) &&
                    !state.viewedIds.includes(id)) {
                    state.unreadIds.push(id);
                }
            });
            state.knownIds = Array.from(new Set([...state.knownIds, ...currentIds]));
            state.unreadIds = state.unreadIds.filter(id => currentIdSet.has(id));
        }

        saveState(resource, state);
        updateResourceBadge(resource);
    };

    const registerCreatedItem = (resource, payload) => {
        if (String(payload?.changeType || '').toLowerCase() !== 'created') return;

        const id = String(payload?.[resources[resource].idProperty] || '');
        if (!id) return;

        const state = readState(resource);
        state.knownIds = Array.from(new Set([...(state.knownIds || []), id]));
        if (!state.viewedIds.includes(id)) {
            state.unreadIds = Array.from(new Set([...state.unreadIds, id]));
        }
        saveState(resource, state);
        updateResourceBadge(resource);
    };

    const markViewed = (resource, id) => {
        if (!resources[resource] || !id) return;

        const normalizedId = String(id);
        const state = readState(resource);
        state.knownIds = Array.from(new Set([...(state.knownIds || []), normalizedId]));
        state.viewedIds = Array.from(new Set([...state.viewedIds, normalizedId]));
        state.unreadIds = state.unreadIds.filter(itemId => itemId !== normalizedId);
        saveState(resource, state);
        updateResourceBadge(resource);
    };

    const refreshMessageCount = async () => {
        const response = await AxiosManager.get('/contact-messages', {});
        const content = response?.data?.content ?? {};
        const count = content.unreadCount
            ?? (content.data ?? []).filter(message => !message.isRead).length;
        updateBadge('/Messages/Message', count);
    };

    const connectOrderNotifications = async () => {
        if (!window.signalR || orderConnection) return;

        orderConnection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/orders', {
                accessTokenFactory: () => StorageManager.getAccessToken() ?? ''
            })
            .withAutomaticReconnect()
            .build();

        orderConnection.on(resources.orders.eventName, payload => registerCreatedItem('orders', payload));
        orderConnection.on(resources.bags.eventName, payload => registerCreatedItem('bags', payload));
        orderConnection.onreconnected(() => Promise.all([
            syncResource('orders'),
            syncResource('bags')
        ]));

        await orderConnection.start();
    };

    const connectMessageNotifications = async () => {
        if (!window.signalR || messageConnection) return;

        messageConnection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/messages', {
                accessTokenFactory: () => StorageManager.getAccessToken() ?? ''
            })
            .withAutomaticReconnect()
            .build();

        messageConnection.on('MessageReceived', () => refreshMessageCount());
        messageConnection.onreconnected(() => refreshMessageCount());
        await messageConnection.start();
    };

    const init = async () => {
        if (initialized) return;
        initialized = true;

        window.addEventListener('contact-messages:unread-count-changed', event => {
            updateBadge('/Messages/Message', event.detail?.count);
        });
        window.addEventListener('admin-items-viewed', event => {
            markViewed(event.detail?.resource, event.detail?.id);
        });
        window.addEventListener('storage', () => {
            updateResourceBadge('orders');
            updateResourceBadge('bags');
        });

        const tasks = [
            syncResource('orders'),
            syncResource('bags'),
            refreshMessageCount()
        ];
        await Promise.allSettled(tasks);

        await Promise.allSettled([
            connectOrderNotifications(),
            connectMessageNotifications()
        ]);
    };

    window.AdminSidebarNotifications = { init, markViewed };
})();
