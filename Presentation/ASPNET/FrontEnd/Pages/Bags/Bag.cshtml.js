const App = {
    setup() {
        const BRASILIA_TIME_ZONE = 'America/Sao_Paulo';

        const state = Vue.reactive({
            mainData: [],
            summary: {
                total: 0,
                active: 0,
                closed: 0,
                abandoned: 0
            },
            mainTitle: 'Editar Sacola',
            id: '',
            customerName: '',
            status: 'Active',
            expirationDate: '',
            lastInteractionAt: '',
            closedAt: '',
            totalItemsValue: 0,
            shippingCost: 0,
            totalWeight: 0,
            allItemsPaid: false,
            notes: '',
            settings: {
                defaultDurationValue: 60,
                defaultDurationUnit: 'months',
                extensionDurationValue: 30,
                extensionDurationUnit: 'months',
                extensionResponseDeadlineValue: 7,
                extensionResponseDeadlineUnit: 'days',
                isSubmitting: false
            },
            duration: {
                bagId: '',
                customerName: '',
                status: '',
                expirationDate: '',
                newExpirationDate: '',
                addValue: null,
                addUnit: 'days',
                history: [],
                isSubmitting: false
            },
            bagStatuses: ['Active', 'Closed', 'Expired', 'Abandoned', 'ReadyToShip', 'Shipped'],
            sort: {
                field: 'createdAt',
                direction: 'desc'
            },
            pagination: {
                page: 1,
                pageSize: 30
            },
            isSubmitting: false
        });

        const mainModalRef = Vue.ref(null);
        const settingsModalRef = Vue.ref(null);
        const durationModalRef = Vue.ref(null);

        const bagStatusLabels = {
            Active: 'Ativa',
            Closed: 'Fechada',
            Expired: 'Expirada',
            Abandoned: 'Abandonada',
            ReadyToShip: 'Pronta para Envio',
            Shipped: 'Enviada'
        };

        const translateBagStatus = (status) => bagStatusLabels[status] ?? status;

        const services = {
            getMainData: async () => AxiosManager.get('/Bag/GetBagList', {}),
            getSingleData: async (id) => AxiosManager.get('/Bag/GetBagSingle', { params: { id } }),
            updateMainData: async (payload) => AxiosManager.post('/Bag/UpdateBag', payload),
            getSettings: async () => AxiosManager.get('/Bag/GetBagSettings', {}),
            updateSettings: async (payload) => AxiosManager.post('/Bag/UpdateBagSettings', payload),
            updateExpiration: async (payload) => AxiosManager.post('/Bag/UpdateBagExpiration', payload)
        };

        const mainGrid = {
            refresh: () => {}
        };

        let bagNotificationsConnection = null;
        let bagNotificationsRefreshTimeout = null;

        const mainModal = {
            obj: null,
            create: () => {
                mainModal.obj = new bootstrap.Modal(mainModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        const settingsModal = {
            obj: null,
            create: () => {
                settingsModal.obj = new bootstrap.Modal(settingsModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        const durationModal = {
            obj: null,
            create: () => {
                durationModal.obj = new bootstrap.Modal(durationModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        const parseUtcDate = (rawDate) => {
            if (!rawDate) return null;

            if (typeof rawDate === 'string') {
                const hasTimeZone = /Z$/i.test(rawDate) || /[+-]\d{2}:\d{2}$/.test(rawDate);
                const utcDateText = hasTimeZone ? rawDate : `${rawDate}Z`;
                const parsedDate = new Date(utcDateText);

                return Number.isNaN(parsedDate.getTime()) ? null : parsedDate;
            }

            const parsedDate = new Date(rawDate);

            return Number.isNaN(parsedDate.getTime()) ? null : parsedDate;
        };

        const formatDateTimeValue = (value) => {
            if (!value) return '';
            const date = parseUtcDate(value);
            if (!date) return '';
            const pad = (n) => String(n).padStart(2, '0');
            const parts = new Intl.DateTimeFormat('pt-BR', {
                timeZone: BRASILIA_TIME_ZONE,
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit',
                hour12: false
            }).formatToParts(date).reduce((result, part) => {
                result[part.type] = part.value;
                return result;
            }, {});

            return `${parts.year}-${parts.month}-${parts.day}T${pad(parts.hour)}:${pad(parts.minute)}`;
        };

        const formatCurrency = (value) =>
            Number(value || 0).toLocaleString('pt-BR', {
                style: 'currency',
                currency: 'BRL'
            });

        const formatNumber = (value, decimals = 2) =>
            Number(value || 0).toLocaleString('pt-BR', {
                minimumFractionDigits: decimals,
                maximumFractionDigits: decimals
            });

        const escapeHtml = (value) => String(value ?? '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');

        const getProductImageUrl = (imageName) =>
            imageName
                ? `/api/FileImage/GetImage?imageName=${encodeURIComponent(imageName)}`
                : '/noimage.png';

        const formatPostCode = (value) => {
            const digits = String(value ?? '').replace(/\D/g, '');

            if (digits.length === 8) {
                return `${digits.slice(0, 5)}-${digits.slice(5)}`;
            }

            return value || '';
        };

        const toDateInputValue = (value) => {
            const date = parseUtcDate(value);
            if (!date) return '';

            const parts = new Intl.DateTimeFormat('pt-BR', {
                timeZone: BRASILIA_TIME_ZONE,
                year: 'numeric',
                month: '2-digit',
                day: '2-digit'
            }).formatToParts(date).reduce((result, part) => {
                result[part.type] = part.value;
                return result;
            }, {});

            return `${parts.year}-${parts.month}-${parts.day}`;
        };

        const getDaysUntil = (value) => {
            const date = parseUtcDate(value);
            if (!date) return null;

            const today = new Date();
            const start = new Date(today.getFullYear(), today.getMonth(), today.getDate());
            const end = new Date(date.getFullYear(), date.getMonth(), date.getDate());
            return Math.ceil((end - start) / 86400000);
        };

        const formatShortDate = (value) => {
            const date = parseUtcDate(value);
            if (!date) return '-';

            return date.toLocaleDateString('pt-BR', {
                timeZone: BRASILIA_TIME_ZONE
            });
        };

        const getExpirationSummary = (value) => {
            const days = getDaysUntil(value);
            const dateText = formatShortDate(value);

            if (days === null) return 'Expira em: -';
            if (days < 0) return `Expirou em: ${dateText} (${Math.abs(days)} dias atras)`;
            if (days === 0) return `Expira em: ${dateText} (hoje)`;
            if (days === 1) return `Expira em: ${dateText} (1 dia restante)`;

            return `Expira em: ${dateText} (${days} dias restantes)`;
        };

        const getExpirationRemainingText = (value) => {
            const days = getDaysUntil(value);

            if (days === null) return '-';
            if (days < 0) return `${Math.abs(days)} dias atras`;
            if (days === 0) return 'hoje';
            if (days === 1) return '1 dia restante';

            return `${days} dias restantes`;
        };

        const formatHistoryEntry = (entry) => {
            const oldDays = getDaysUntil(entry.oldExpirationDate);
            const newDays = getDaysUntil(entry.newExpirationDate);
            const changedBy = entry.changedBy || 'admin';
            const changedAt = formatShortDate(entry.changedAtUtc);

            if (oldDays === null || newDays === null) {
                return `Prazo alterado por ${changedBy} em ${changedAt}: ${formatShortDate(entry.oldExpirationDate)} -> ${formatShortDate(entry.newExpirationDate)}`;
            }

            return `Prazo estendido por ${changedBy} em ${changedAt}: ${oldDays} -> ${newDays} dias`;
        };

        const addDuration = (value, amount, unit) => {
            const date = parseUtcDate(value);
            const quantity = Number(amount);
            if (!date || !Number.isFinite(quantity) || quantity <= 0) return null;

            const result = new Date(date);
            if (unit === 'months') {
                result.setMonth(result.getMonth() + quantity);
            } else {
                result.setDate(result.getDate() + quantity);
            }

            return result;
        };

        const getBagItemPaymentStatus = (item) => {
            if (item.isPaid) {
                return {
                    label: 'Pago',
                    className: 'bag-detail-status--paid'
                };
            }

            const reservationExpiresAt = parseUtcDate(item.reservationExpiresAt);
            if (item.isReserved && reservationExpiresAt && reservationExpiresAt < new Date()) {
                return {
                    label: 'Reserva expirada',
                    className: 'bag-detail-status--expired'
                };
            }

            if (item.isReserved) {
                return {
                    label: 'Reservado',
                    className: 'bag-detail-status--reserved'
                };
            }

            return {
                label: 'Pendente',
                className: 'bag-detail-status--pending'
            };
        };

        const methods = {
            updateSummaryCards: () => {
                const total = state.mainData.length;
                const active = state.mainData.filter(x => x?.status === 'Active').length;
                const closed = state.mainData.filter(x => x?.status === 'Closed').length;
                const abandoned = state.mainData.filter(x => x?.status === 'Abandoned').length;

                state.summary = { total, active, closed, abandoned };
            },
            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = (response?.data?.content?.data ?? []).map((item) => ({
                    ...item,
                    statusDisplay: translateBagStatus(item.status),
                    createdAt: parseUtcDate(item.createdAt),
                    lastInteractionAt: parseUtcDate(item.lastInteractionAt)
                }));
                state.pagination.page = Math.min(state.pagination.page, Math.max(totalPages.value, 1));
                methods.updateSummaryCards();
            },
            loadSettings: async () => {
                const response = await services.getSettings();
                const settings = response?.data?.content ?? {};

                Object.assign(state.settings, {
                    defaultDurationValue: settings.defaultDurationValue ?? 60,
                    defaultDurationUnit: settings.defaultDurationUnit ?? 'months',
                    extensionDurationValue: settings.extensionDurationValue ?? 30,
                    extensionDurationUnit: settings.extensionDurationUnit ?? 'months',
                    extensionResponseDeadlineValue: settings.extensionResponseDeadlineValue ?? 7,
                    extensionResponseDeadlineUnit: settings.extensionResponseDeadlineUnit ?? 'days'
                });
            },
            showSettingsModal: async () => {
                try {
                    await methods.loadSettings();
                    settingsModal.obj.show();
                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Configuracoes da sacolinha',
                        text: error.response?.data?.message ?? 'Nao foi possivel carregar as configuracoes.'
                    });
                }
            },
            saveSettings: async () => {
                try {
                    state.settings.isSubmitting = true;

                    await services.updateSettings({
                        defaultDurationValue: state.settings.defaultDurationValue,
                        defaultDurationUnit: state.settings.defaultDurationUnit,
                        extensionDurationValue: state.settings.extensionDurationValue,
                        extensionDurationUnit: state.settings.extensionDurationUnit,
                        extensionResponseDeadlineValue: state.settings.extensionResponseDeadlineValue,
                        extensionResponseDeadlineUnit: state.settings.extensionResponseDeadlineUnit
                    });

                    settingsModal.obj.hide();
                    Swal.fire({
                        icon: 'success',
                        title: 'Configuracoes atualizadas com sucesso',
                        timer: 1200,
                        showConfirmButton: false
                    });
                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Erro',
                        text: error.response?.data?.message ?? 'Nao foi possivel salvar as configuracoes.'
                    });
                } finally {
                    state.settings.isSubmitting = false;
                }
            },
            connectBagNotifications: async () => {
                if (!window.signalR || bagNotificationsConnection) {
                    return;
                }

                bagNotificationsConnection = new signalR.HubConnectionBuilder()
                    .withUrl('/hubs/orders', {
                        accessTokenFactory: () => StorageManager.getAccessToken() ?? ''
                    })
                    .withAutomaticReconnect()
                    .build();

                bagNotificationsConnection.on('BagChanged', () => {
                    clearTimeout(bagNotificationsRefreshTimeout);
                    bagNotificationsRefreshTimeout = setTimeout(async () => {
                        try {
                            await methods.populateMainData();
                            mainGrid.refresh();
                        } catch (error) {
                            console.error('Nao foi possivel atualizar as sacolas em tempo real.', error);
                        }
                    }, 250);
                });

                try {
                    await bagNotificationsConnection.start();
                } catch (error) {
                    console.error('Nao foi possivel conectar as notificacoes de sacolas.', error);
                    bagNotificationsConnection = null;
                }
            },
            formatDateTime: (value) => {
                if (!value) {
                    return {
                        date: '-',
                        time: ''
                    };
                }

                const date = parseUtcDate(value);
                if (!date) {
                    return {
                        date: '-',
                        time: ''
                    };
                }

                return {
                    date: date.toLocaleDateString('pt-BR', {
                        timeZone: BRASILIA_TIME_ZONE
                    }),
                    time: date.toLocaleTimeString('pt-BR', {
                        timeZone: BRASILIA_TIME_ZONE,
                        hour: '2-digit',
                        minute: '2-digit'
                    })
                };
            },
            getSortIcon: (field) => {
                if (state.sort.field !== field) return 'fa-sort';

                return state.sort.direction === 'asc'
                    ? 'fa-sort-up'
                    : 'fa-sort-down';
            },
            getStatusClass: (status) => ({
                Active: 'bag-status--active',
                Closed: 'bag-status--closed',
                Expired: 'bag-status--expired',
                Abandoned: 'bag-status--abandoned',
                ReadyToShip: 'bag-status--readytoship',
                Shipped: 'bag-status--shipped'
            }[status] || 'bag-status--active'),
            formatPostCode: (value) => formatPostCode(value),
            formatShortDate: (value) => formatShortDate(value),
            getExpirationSummary: (value) => getExpirationSummary(value),
            getExpirationRemainingText: (value) => getExpirationRemainingText(value),
            formatHistoryEntry: (entry) => formatHistoryEntry(entry),
            getSortValue: (bag, field) => {
                if (field === 'createdAt') {
                    const date = parseUtcDate(bag?.createdAt);
                    return date ? date.getTime() : 0;
                }

                if (field === 'customerName') return String(bag?.customerName ?? '').toLowerCase();
                if (field === 'status') return translateBagStatus(bag?.status).toLowerCase();

                return '';
            },
            getPagerText: () => {
                const total = sortedBags.value.length;
                if (!total) return 'Nenhuma sacola';

                const start = ((state.pagination.page - 1) * state.pagination.pageSize) + 1;
                const end = Math.min(start + state.pagination.pageSize - 1, total);
                return `${start}-${end} de ${total} sacolas`;
            },
            loadBag: async (id) => {
                const response = await services.getSingleData(id);
                const bag = response?.data?.content?.data;
                if (!bag) return;

                state.id = bag.id ?? '';
                state.customerName = bag.customerName ?? '';
                state.status = bag.status ?? 'Active';
                state.expirationDate = formatDateTimeValue(bag.expirationDate);
                state.lastInteractionAt = formatDateTimeValue(bag.lastInteractionAt);
                state.closedAt = formatDateTimeValue(bag.closedAt);
                state.totalItemsValue = bag.totalItemsValue ?? 0;
                state.shippingCost = bag.shippingCost ?? 0;
                state.totalWeight = bag.totalWeight ?? 0;
                state.allItemsPaid = bag.allItemsPaid ?? false;
                state.notes = bag.notes ?? '';
            },
            showDurationModal: async (bag) => {
                if (!bag?.id) return;

                try {
                    const response = await services.getSingleData(bag.id);
                    const singleBag = response?.data?.content?.data;
                    if (!singleBag) return;

                    Object.assign(state.duration, {
                        bagId: singleBag.id ?? '',
                        customerName: singleBag.customerName ?? '',
                        status: singleBag.status ?? 'Active',
                        expirationDate: singleBag.expirationDate ?? '',
                        newExpirationDate: toDateInputValue(singleBag.expirationDate),
                        addValue: null,
                        addUnit: 'days',
                        history: singleBag.expirationHistory ?? [],
                        isSubmitting: false
                    });

                    durationModal.obj.show();
                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Prazo da sacola',
                        text: error.response?.data?.message ?? 'Nao foi possivel carregar o prazo desta sacola.'
                    });
                }
            },
            applyDurationChange: async () => {
                const currentBag = state.mainData.find(x => x.id === state.duration.bagId);
                const customerName = state.duration.customerName || currentBag?.customerName || 'esta cliente';
                const addValue = Number(state.duration.addValue);
                const addedDate = addDuration(state.duration.expirationDate, addValue, state.duration.addUnit);
                const newDate = addedDate
                    ?? (state.duration.newExpirationDate ? new Date(`${state.duration.newExpirationDate}T23:59:59`) : null);

                if (!newDate || Number.isNaN(newDate.getTime())) {
                    Swal.fire({
                        icon: 'warning',
                        title: 'Informe um novo prazo',
                        text: 'Defina uma nova data ou informe uma quantidade de dias/meses.'
                    });
                    return;
                }

                const result = await Swal.fire({
                    icon: 'warning',
                    title: 'Confirmar novo prazo?',
                    text: `Isso vai sobrescrever o prazo padrao apenas para a sacola de ${customerName}. Confirmar?`,
                    showCancelButton: true,
                    confirmButtonText: 'Confirmar',
                    cancelButtonText: 'Cancelar'
                });

                if (!result.isConfirmed) return;

                try {
                    state.duration.isSubmitting = true;

                    const response = await services.updateExpiration({
                        bagId: state.duration.bagId,
                        newExpirationDate: newDate,
                        addValue: null,
                        addUnit: state.duration.addUnit
                    });

                    const content = response?.data?.content ?? {};
                    state.duration.expirationDate = content.expirationDate ?? newDate.toISOString();
                    state.duration.newExpirationDate = toDateInputValue(state.duration.expirationDate);
                    state.duration.addValue = null;
                    state.duration.history = content.history ?? state.duration.history;

                    await methods.populateMainData();

                    Swal.fire({
                        icon: 'success',
                        title: 'Prazo atualizado',
                        timer: 1200,
                        showConfirmButton: false
                    });
                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Erro',
                        text: error.response?.data?.message ?? 'Nao foi possivel atualizar o prazo.'
                    });
                } finally {
                    state.duration.isSubmitting = false;
                }
            },
            showBagDetails: async (id) => {
                try {
                    const response = await services.getSingleData(id);
                    const bag = response?.data?.content?.data;
                    const items = bag?.items ?? [];

                    if (!items.length) {
                        return Swal.fire({ icon: 'info', title: 'Detalhes da Sacola', text: 'Nenhum item encontrado para esta sacola.' });
                    }

                    const itemCards = items.map((item) => {
                        const paymentStatus = getBagItemPaymentStatus(item);

                        return `
                        <div class="bag-detail-card">
                            <img class="bag-detail-card__image"
                                 src="${getProductImageUrl(item.productImageUrl)}"
                                 alt="${escapeHtml(item.productName || 'Roupa')}"
                                 onerror="this.onerror=null;this.src='/noimage.png';">
                            <div class="bag-detail-card__body">
                                <div class="bag-detail-card__name">${escapeHtml(item.productName || 'Desconhecido')}</div>
                                <div class="bag-detail-card__meta">
                                    <span>Qtd: ${Number(item.quantity || 0)}</span>
                                    <span>${formatCurrency(item.price)}</span>
                                    <span class="bag-detail-status ${paymentStatus.className}">${paymentStatus.label}</span>
                                </div>
                            </div>
                        </div>
                    `;
                    }).join('');

                    await Swal.fire({
                        title: `${items.length} ${items.length === 1 ? 'item' : 'itens'} na sacola`,
                        html: `
                            <style>
                                .bag-detail-grid {
                                    display: grid;
                                    gap: 12px;
                                    grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
                                    text-align: left;
                                }

                                .bag-detail-card {
                                    border: 1px solid #e5e7eb;
                                    border-radius: 8px;
                                    overflow: hidden;
                                    background: #fff;
                                }

                                .bag-detail-card__image {
                                    aspect-ratio: 4 / 5;
                                    background: #f3f4f6;
                                    display: block;
                                    object-fit: cover;
                                    width: 100%;
                                }

                                .bag-detail-card__body {
                                    padding: 10px;
                                }

                                .bag-detail-card__name {
                                    color: #111827;
                                    font-size: 14px;
                                    font-weight: 800;
                                    line-height: 1.25;
                                    margin-bottom: 8px;
                                }

                                .bag-detail-card__meta {
                                    color: #6b7280;
                                    display: flex;
                                    flex-wrap: wrap;
                                    font-size: 12px;
                                    font-weight: 700;
                                    gap: 6px 10px;
                                }

                                .bag-detail-status {
                                    border-radius: 999px;
                                    display: inline-flex;
                                    font-size: 11px;
                                    font-weight: 900;
                                    line-height: 1;
                                    padding: 5px 8px;
                                }

                                .bag-detail-status--paid {
                                    background: #dcfce7;
                                    color: #166534;
                                }

                                .bag-detail-status--pending {
                                    background: #fef3c7;
                                    color: #92400e;
                                }

                                .bag-detail-status--reserved {
                                    background: #dbeafe;
                                    color: #1d4ed8;
                                }

                                .bag-detail-status--expired {
                                    background: #fee2e2;
                                    color: #991b1b;
                                }
                            </style>
                            <div class="bag-detail-grid">
                                ${itemCards}
                            </div>
                        `,
                        width: 840,
                        confirmButtonText: 'Fechar'
                    });
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'Erro', text: error.response?.data?.message ?? 'Nao foi possivel carregar os detalhes da sacola' });
                }
            }
        };

        const sortedBags = Vue.computed(() => {
            const direction = state.sort.direction === 'desc' ? -1 : 1;

            return [...state.mainData].sort((first, second) => {
                const firstValue = methods.getSortValue(first, state.sort.field);
                const secondValue = methods.getSortValue(second, state.sort.field);

                if (typeof firstValue === 'number' && typeof secondValue === 'number') {
                    return (firstValue - secondValue) * direction;
                }

                return String(firstValue).localeCompare(String(secondValue), 'pt-BR', {
                    numeric: true,
                    sensitivity: 'base'
                }) * direction;
            });
        });

        const totalPages = Vue.computed(() =>
            Math.max(Math.ceil(sortedBags.value.length / state.pagination.pageSize), 1)
        );

        const pagedBags = Vue.computed(() => {
            if (state.pagination.page > totalPages.value) {
                state.pagination.page = totalPages.value;
            }

            const start = (state.pagination.page - 1) * state.pagination.pageSize;
            return sortedBags.value.slice(start, start + state.pagination.pageSize);
        });

        const handler = {
            handleSort: (field) => {
                if (state.sort.field === field) {
                    state.sort.direction = state.sort.direction === 'asc' ? 'desc' : 'asc';
                } else {
                    state.sort.field = field;
                    state.sort.direction = field === 'createdAt' ? 'desc' : 'asc';
                }

                state.pagination.page = 1;
            },
            handlePreviousPage: () => {
                state.pagination.page = Math.max(state.pagination.page - 1, 1);
            },
            handleNextPage: () => {
                state.pagination.page = Math.min(state.pagination.page + 1, totalPages.value);
            },
            handleEdit: async (bag) => {
                if (!bag?.id) return;

                await methods.loadBag(bag.id);
                mainModal.obj.show();
            },
            handleDelete: async (bag) => {
                if (!bag?.id) return;

                const result = await Swal.fire({
                    icon: 'warning',
                    title: 'Excluir Sacola?',
                    text: 'Esta sacola deixara de aparecer na listagem principal.',
                    showCancelButton: true,
                    confirmButtonText: 'Excluir',
                    cancelButtonText: 'Cancelar',
                    confirmButtonColor: '#dc2626'
                });

                if (!result.isConfirmed) return;

                try {
                    state.isSubmitting = true;

                    const response = await services.updateMainData({
                        id: bag.id,
                        status: bag.status,
                        expirationDate: bag.expirationDate ? new Date(bag.expirationDate) : null,
                        lastInteractionAt: bag.lastInteractionAt ? new Date(bag.lastInteractionAt) : null,
                        closedAt: bag.closedAt ? new Date(bag.closedAt) : null,
                        totalItemsValue: bag.totalItemsValue ?? 0,
                        shippingCost: bag.shippingCost ?? 0,
                        totalWeight: bag.totalWeight ?? 0,
                        allItemsPaid: bag.allItemsPaid ?? false,
                        notes: bag.notes ?? '',
                        isDeleted: true
                    });

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();
                        Swal.fire({
                            icon: 'success',
                            title: 'Excluida',
                            text: 'Sacola excluida com sucesso',
                            timer: 1200,
                            showConfirmButton: false
                        });
                    }
                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Erro',
                        text: error.response?.data?.message ?? 'Nao foi possivel excluir a sacola'
                    });
                } finally {
                    state.isSubmitting = false;
                }
            },
            handleSubmit: async () => {
                try {
                    state.isSubmitting = true;

                    const payload = {
                        id: state.id,
                        status: state.status,
                        expirationDate: state.expirationDate ? new Date(state.expirationDate) : null,
                        lastInteractionAt: state.lastInteractionAt ? new Date(state.lastInteractionAt) : null,
                        closedAt: state.closedAt ? new Date(state.closedAt) : null,
                        totalItemsValue: state.totalItemsValue,
                        shippingCost: state.shippingCost,
                        totalWeight: state.totalWeight,
                        allItemsPaid: state.allItemsPaid,
                        notes: state.notes
                    };

                    const response = await services.updateMainData(payload);
                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();
                        Swal.fire({
                            icon: 'success',
                            title: 'Sucesso',
                            text: 'Sacola atualizada com sucesso',
                            timer: 1200,
                            showConfirmButton: false
                        });
                        mainModal.obj.hide();
                    }
                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Erro',
                        text: error.response?.data?.message ?? 'Nao foi possivel atualizar a sacola'
                    });
                } finally {
                    state.isSubmitting = false;
                }
            }
        };

        Vue.onMounted(async () => {
            await methods.populateMainData();
            await methods.connectBagNotifications();
            mainModal.create();
            settingsModal.create();
            durationModal.create();
        });

        return {
            state,
            mainModalRef,
            settingsModalRef,
            durationModalRef,
            sortedBags,
            pagedBags,
            totalPages,
            translateBagStatus,
            formatCurrency,
            formatNumber,
            methods,
            handler
        };
    }
};

Vue.createApp(App).mount('#app');
