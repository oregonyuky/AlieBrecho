const App = {
    setup() {
        const BRASILIA_TIME_ZONE = 'America/Sao_Paulo';

        const emptyPayment = () => ({
            id: '',
            name: '',
            description: '',
            status: 'Pending',
            paymentDateTime: '',
            amount: 0,
            paymentTypeId: '',
            paymentMethod: '',
            transactionId: '',
            authorizationCode: '',
            referenceNumber: '',
            cardHolderName: '',
            cardLast4: ''
        });

        const emptyShipping = () => ({
            firstName: '',
            lastName: '',
            email: '',
            phoneNumber: '',
            street: '',
            number: '',
            neighborhood: '',
            complement: '',
            city: '',
            state: '',
            postCode: ''
        });

        const emptyLabel = () => ({
            orderId: '',
            recipientName: '',
            postCode: '',
            city: '',
            state: '',
            dimensions: '',
            weight: 0,
            shippingCost: 0,
            totalAmount: 0,
            labelId: '',
            cartAddedAt: '',
            checkoutAt: '',
            generatedAt: '',
            error: '',
            rawResponse: '',
            isGenerating: false,
            isBuying: false,
            isGeneratingPurchased: false,
            isDownloading: false
        });

        const emptyState = () => ({
            mainData: [],
            summary: {
                total: 0,
                pending: 0,
                paid: 0,
                cancelled: 0
            },
            melhorEnvioBalance: {
                balance: 0,
                reserved: 0,
                debts: 0,
                isLoading: false,
                isInserting: false,
                error: ''
            },
            deleteMode: false,
            mainTitle: 'Pedido',
            id: '',
            customerId: '',
            status: 'Pending',
            orderDate: new Date().toISOString(),
            discount: 0,
            taxes: 0,
            shippingCost: 0,
            totalAmount: 0,
            notes: '',
            shippingBoxId: '',
            payment: emptyPayment(),
            shipping: emptyShipping(),
            orderDetails: [],
            customers: [],
            products: [],
            shippingBoxes: [],
            paymentTypes: [],
            label: emptyLabel(),
            paymentStatuses: ['Pending', 'Paid', 'Cancelled'],
            orderStatuses: ['Pending', 'Paid', 'Dispatched', 'Shipped', 'Delivered', 'Cancelled'],
            errors: {
                customerId: '',
                orderDetails: ''
            },
            sort: {
                field: 'orderDate',
                direction: 'desc'
            },
            pagination: {
                page: 1,
                pageSize: 30
            },
            isSubmitting: false
        });

        const state = Vue.reactive(emptyState());

        const statusLabels = {
            Pending: 'Aguardando pagamento',
            Paid: 'Pagamento confirmado',
            Dispatched: 'Postado na transportadora',
            Shipped: 'Em transporte',
            Delivered: 'Entregue',
            Cancelled: 'Cancelado',
            Packaging: 'Preparando embalagem',
            LabelGenerated: 'Etiqueta gerada'
        };

        const translateStatus = (status) => statusLabels[status] ?? status;

        const orderTimelineSteps = [
            {
                key: 'Pending',
                label: 'Aguardando pagamento',
                iconClass: 'fas fa-clock',
                description: 'Pedido criado, mas o pagamento ainda nao foi confirmado.'
            },
            {
                key: 'Paid',
                label: 'Pagamento confirmado',
                iconClass: 'fas fa-credit-card',
                description: 'Pagamento recebido. O pedido ja pode ser separado.'
            },
            {
                key: 'Packaging',
                label: 'Preparando embalagem',
                iconClass: 'fas fa-box-open',
                description: 'Produtos sendo separados, conferidos e embalados.'
            },
            {
                key: 'LabelGenerated',
                label: 'Etiqueta gerada',
                iconClass: 'fas fa-tag',
                description: 'Etiqueta de envio criada e pronta para impressao.'
            },
            {
                key: 'Dispatched',
                label: 'Postado na transportadora',
                iconClass: 'fas fa-truck-loading',
                description: 'Pacote entregue para a transportadora.'
            },
            {
                key: 'Shipped',
                label: 'Em transporte',
                iconClass: 'fas fa-shipping-fast',
                description: 'Pacote em deslocamento ate a cliente.'
            },
            {
                key: 'Delivered',
                label: 'Entregue',
                iconClass: 'fas fa-check',
                description: 'Pedido entregue para a cliente.'
            }
        ];

        const mainModalRef = Vue.ref(null);
        const labelModalRef = Vue.ref(null);

        const services = {
            getMainData: async () => AxiosManager.get('/Order/GetOrderList', {}),
            getSingleData: async (id) => AxiosManager.get('/Order/GetOrderSingle', { params: { id } }),
            createMainData: async (request) => AxiosManager.post('/Order/CreateOrder', request),
            updateMainData: async (request) => AxiosManager.post('/Order/UpdateOrder', request),
            deleteMainData: async (id) => AxiosManager.post('/Order/DeleteOrder', { id }),
            getCustomers: async () => AxiosManager.get('/Customer/GetCustomerList', {}),
            getProducts: async () => AxiosManager.get('/Product/GetProductList', {}),
            getShippingBoxes: async () => AxiosManager.get('/ShippingBox/GetShippingBoxList', {}),
            getPaymentTypes: async () => AxiosManager.get('/PaymentType/GetPaymentTypeList', {}),
            getMelhorEnvioBalance: async () => AxiosManager.get('/Order/GetMelhorEnvioBalance', {}),
            insertMelhorEnvioBalance: async (request) => AxiosManager.post('/Order/InsertMelhorEnvioBalance', request),
            generateShippingLabel: async (request) => AxiosManager.post('/Order/GenerateShippingLabel', request),
            markShippingCart: async (request) => AxiosManager.post('/Order/MarkShippingCart', request),
            buyShippingCart: async (request) => AxiosManager.post('/Order/BuyShippingCart', request),
            generatePurchasedShippingLabel: async (request) => AxiosManager.post('/Order/GeneratePurchasedShippingLabel', request),
            createMercadoPagoPixPayment: async (request) => AxiosManager.post('/pix/criar-pagamento', request),
            getMercadoPagoPixPaymentStatus: async (paymentId) => AxiosManager.get(`/pix/status/${encodeURIComponent(paymentId)}`, {}),
            downloadShippingLabel: async (labelId) => AxiosManager.get('/Order/DownloadShippingLabel', {
                params: { labelId },
                responseType: 'blob'
            }),
            calculateShippingCost: async (shippingBoxId, destinationPostCode) => AxiosManager.get('/Order/CalculateShippingCost', {
                params: { shippingBoxId, destinationPostCode }
            })
        };

        const calculateItemsTotal = () => state.orderDetails.reduce((total, item) => {
            const quantity = Number(item.quantity || 0);
            const unitPrice = Number(item.unitPrice || 0);
            return total + (quantity * unitPrice);
        }, 0);

        const getSelectedShippingBox = () =>
            state.shippingBoxes.find(x => x.id === state.shippingBoxId) ?? null;

        const calculateCubicWeight = (shippingBox) => {
            if (!shippingBox) return 0;

            return (
                Number(shippingBox.width || 0) *
                Number(shippingBox.length || 0) *
                Number(shippingBox.height || 0)
            ) / 6000;
        };

        const calculateChargedWeight = (shippingBox) => {
            if (!shippingBox) return 0;

            return Math.max(
                Number(shippingBox.weight || 0),
                calculateCubicWeight(shippingBox)
            );
        };

        const calculateShippingCost = () => {
            const shippingBox = getSelectedShippingBox();
            if (!shippingBox) return 0;

            const chargedWeight = calculateChargedWeight(shippingBox);
            const insurance = Number(shippingBox.insuranceValue || 0) * 0.01;

            return Number(((chargedWeight * 10) + insurance).toFixed(2));
        };

        const getItemTotal = (item) =>
            Number(item.quantity || 0) * Number(item.unitPrice || 0);

        const getItemShippingCost = (item) => {
            const itemsTotal = calculateItemsTotal();
            if (!itemsTotal || !state.shippingCost) return 0;

            return Number(((getItemTotal(item) / itemsTotal) * state.shippingCost).toFixed(2));
        };

        const formatCurrency = (value) =>
            Number(value || 0).toLocaleString('pt-BR', {
                style: 'currency',
                currency: 'BRL'
            });

        const formatNumber = (value, decimals = 2) =>
            Number(value || 0).toFixed(decimals);

        const formatCompactNumber = (value) =>
            parseFloat(Number(value || 0).toFixed(2)).toString();

        const formatShippingBoxData = (shippingBox) => {
            if (!shippingBox) return '';

            return `${formatCompactNumber(shippingBox.width)}cm x ${formatCompactNumber(shippingBox.length)}cm x ${formatCompactNumber(shippingBox.height)}cm ${formatCompactNumber(shippingBox.weight)}kg`;
        };

        const formatRowShippingBoxData = (order) => {
            const width = Number(order.shippingBoxWidth || 0);
            const length = Number(order.shippingBoxLength || 0);
            const height = Number(order.shippingBoxHeight || 0);
            const weight = Number(order.shippingBoxWeight || 0);

            if (!width || !length || !height) {
                return order.shippingBoxData || '';
            }

            return `${formatCompactNumber(width)}cm x ${formatCompactNumber(length)}cm x ${formatCompactNumber(height)}cm ${formatCompactNumber(weight)}kg`;
        };

        const splitCustomerName = (name) => {
            const parts = String(name || '').trim().split(/\s+/).filter(Boolean);
            if (!parts.length) {
                return { firstName: '', lastName: '' };
            }

            return {
                firstName: parts[0],
                lastName: parts.slice(1).join(' ')
            };
        };

        const getApiErrorMessage = (error, fallback = 'Erro inesperado') => {
            const message = error?.response?.data?.message ?? error?.message ?? fallback;
            const cleanMessage = String(message).replace(/^Exception:\s*/i, '');

            try {
                const parsed = JSON.parse(cleanMessage);
                if (parsed.message) return parsed.message;
                if (parsed.error) return parsed.error;
            } catch {
                return cleanMessage;
            }

            return cleanMessage;
        };

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

        const getOrderItemCount = (order) =>
            (order?.orderDetails ?? []).reduce((total, item) => total + Number(item?.quantity || 0), 0);

        const parseMoneyInput = (value) => Number(String(value || '')
            .trim()
            .replace(/\./g, '')
            .replace(',', '.'));

        const isImageSource = (value) => {
            const text = String(value || '').trim();
            return /^data:image\//i.test(text) ||
                /^https?:\/\/.+\.(png|jpg|jpeg|gif|webp)(\?.*)?$/i.test(text);
        };

        const buildInsertBalanceResultHtml = (content) => {
            const paymentUrl = content.paymentUrl || '';
            const pixQrCode = content.pixQrCode || '';
            const pixCopyPaste = content.pixCopyPaste || (!isImageSource(pixQrCode) ? pixQrCode : '');
            const parts = [];

            if (paymentUrl) {
                parts.push(`
                    <a href="${escapeHtml(paymentUrl)}" target="_blank" rel="noopener noreferrer" class="btn btn-primary w-100 mb-3">
                        Abrir pagamento
                    </a>
                `);
            }

            if (isImageSource(pixQrCode)) {
                parts.push(`
                    <div class="text-center mb-3">
                        <img src="${escapeHtml(pixQrCode)}" alt="QR Code Pix" class="img-fluid" style="max-width:260px;">
                    </div>
                `);
            }

            if (pixCopyPaste) {
                parts.push(`
                    <label class="form-label text-start d-block">Pix copia e cola</label>
                    <textarea id="insert-balance-pix-copy" class="form-control mb-2" rows="4" readonly>${escapeHtml(pixCopyPaste)}</textarea>
                    <button type="button" id="insert-balance-copy-button" class="btn btn-outline-primary w-100">
                        Copiar Pix
                    </button>
                `);
            }

            if (!parts.length) {
                parts.push('<p class="mb-0">A solicitacao foi criada no Melhor Envio, mas a resposta nao trouxe link ou codigo Pix reconhecido.</p>');
            }

            return `<div class="text-start">${parts.join('')}</div>`;
        };

        const buildPixPaymentHtml = (content) => {
            const qrCodeBase64 = content.qrCodeBase64 || content.qr_code_base64 || '';
            const qrCode = content.qrCode || content.qr_code || '';
            const expiresAt = content.expiracao || content.expiration || '';
            const qrImage = qrCodeBase64
                ? `data:image/png;base64,${qrCodeBase64.replace(/^data:image\/png;base64,/i, '')}`
                : '';

            return `
                <div class="text-start">
                    <div class="text-center mb-3">
                        ${qrImage ? `<img src="${escapeHtml(qrImage)}" alt="QR Code Pix" class="img-fluid border rounded p-2" style="max-width:260px;">` : '<div class="alert alert-warning mb-0">QR Code nao retornado pelo Mercado Pago.</div>'}
                    </div>
                    <label class="form-label" for="pix-copy-code">Pix copia e cola</label>
                    <textarea id="pix-copy-code" class="form-control mb-2" rows="4" readonly>${escapeHtml(qrCode)}</textarea>
                    <button type="button" id="pix-copy-button" class="btn btn-outline-primary w-100 mb-3">
                        Copiar Pix
                    </button>
                    <div class="d-flex align-items-center justify-content-center gap-2 text-muted mb-2">
                        <span class="spinner-border spinner-border-sm" id="pix-status-spinner"></span>
                        <span id="pix-status-text">Aguardando confirmacao do Mercado Pago...</span>
                    </div>
                    <div class="text-center small text-muted" data-expiration="${escapeHtml(expiresAt)}">
                        Expira em <strong id="pix-countdown">--:--</strong>
                    </div>
                    <button type="button" id="pix-new-button" class="btn btn-primary w-100 mt-3 d-none">
                        Gerar novo QR Code
                    </button>
                </div>
            `;
        };

        const resetLabel = () => {
            state.label = emptyLabel();
        };

        const recalculateTotal = (shippingCost = state.shippingCost) => {
            if (!shippingCost && state.shippingBoxId) {
                shippingCost = calculateShippingCost();
            }

            state.shippingCost = shippingCost;
            state.totalAmount = calculateItemsTotal()
                - Number(state.discount || 0)
                + Number(state.taxes || 0)
                + Number(state.shippingCost || 0);
            state.payment.amount = state.totalAmount;
        };

        const resetForm = () => {
            const initial = emptyState();
            Object.keys(initial).forEach((key) => {
                if ([
                    'mainData',
                    'summary',
                    'melhorEnvioBalance',
                    'customers',
                    'products',
                    'shippingBoxes',
                    'paymentTypes'
                ].includes(key)) {
                    return;
                }
                state[key] = initial[key];
            });
        };

        const fillOrder = (order) => {
            state.id = order.id ?? '';
            state.customerId = order.customerId ?? '';
            state.status = order.status ?? 'Pending';
            state.orderDate = order.orderDate ?? new Date().toISOString();
            state.discount = order.discount ?? 0;
            state.taxes = order.taxes ?? 0;
            state.shippingCost = order.shippingCost ?? 0;
            state.totalAmount = order.totalAmount ?? 0;
            state.notes = order.notes ?? '';
            state.shippingBoxId = order.shippingBoxId ?? '';

            state.payment.id = order.payment?.id ?? '';
            state.payment.name = order.payment?.name ?? '';
            state.payment.description = order.payment?.description ?? '';
            state.payment.status = order.payment?.status ?? 'Pending';
            state.payment.paymentDateTime = order.payment?.paymentDateTime ? formatDateTimeValue(order.payment.paymentDateTime) : '';
                state.payment.amount = order.payment?.amount ?? state.totalAmount;
            state.payment.paymentTypeId = order.payment?.paymentTypeId ?? '';
            state.payment.paymentMethod = order.payment?.paymentDetail?.paymentMethod ?? '';
            state.payment.transactionId = order.payment?.paymentDetail?.transactionId ?? '';
            state.payment.authorizationCode = order.payment?.paymentDetail?.authorizationCode ?? '';
            state.payment.referenceNumber = order.payment?.paymentDetail?.referenceNumber ?? '';
            state.payment.cardHolderName = order.payment?.paymentDetail?.cardHolderName ?? '';
            state.payment.cardLast4 = order.payment?.paymentDetail?.cardLast4 ?? '';

            state.shipping.firstName = order.shippingDetail?.firstName ?? '';
            state.shipping.lastName = order.shippingDetail?.lastName ?? '';
            state.shipping.email = order.shippingDetail?.email ?? '';
            state.shipping.phoneNumber = order.shippingDetail?.phoneNumber ?? '';
            state.shipping.street = order.shippingDetail?.street ?? '';
            state.shipping.number = order.shippingDetail?.number ?? '';
            state.shipping.neighborhood = order.shippingDetail?.neighborhood ?? '';
            state.shipping.complement = order.shippingDetail?.complement ?? '';
            state.shipping.city = order.shippingDetail?.city ?? '';
            state.shipping.state = order.shippingDetail?.state ?? '';
            state.shipping.postCode = order.shippingDetail?.postCode ?? '';

            state.orderDetails = (order.orderDetails ?? []).map((item) => ({
                id: item.id ?? '',
                productId: item.productId ?? '',
                quantity: item.quantity ?? 1,
                unitPrice: item.unitPrice ?? 0
            }));

            recalculateTotal(state.shippingCost);
        };

        const buildPayload = () => ({
            id: state.id,
            customerId: state.customerId,
            status: state.status,
            discount: state.discount,
            taxes: state.taxes,
            shippingCost: state.shippingCost,
            totalAmount: state.totalAmount,
            notes: state.notes,
            shippingBoxId: state.shippingBoxId || null,
            payment: {
                id: state.payment.id,
                name: state.payment.name,
                description: state.payment.description,
                status: state.payment.status,
                paymentDateTime: state.payment.paymentDateTime ? new Date(state.payment.paymentDateTime) : null,
                amount: state.payment.amount,
                paymentTypeId: state.payment.paymentTypeId || null,
                paymentMethod: state.payment.paymentMethod,
                transactionId: state.payment.transactionId,
                authorizationCode: state.payment.authorizationCode,
                referenceNumber: state.payment.referenceNumber,
                cardHolderName: state.payment.cardHolderName,
                cardLast4: state.payment.cardLast4
            },
            shippingDetail: {
                firstName: state.shipping.firstName,
                lastName: state.shipping.lastName,
                email: state.shipping.email,
                phoneNumber: state.shipping.phoneNumber,
                street: state.shipping.street,
                number: state.shipping.number,
                neighborhood: state.shipping.neighborhood,
                complement: state.shipping.complement,
                city: state.shipping.city,
                state: state.shipping.state,
                postCode: state.shipping.postCode
            },
            orderDetails: state.orderDetails.map((item) => ({
                id: item.id,
                productId: item.productId,
                quantity: Number(item.quantity || 1),
                unitPrice: Number(item.unitPrice || 0)
            }))
        });

        const mainGrid = {
            refresh: () => {}
        };

        let orderNotificationsConnection = null;
        let orderNotificationsRefreshTimeout = null;
        let catalogNotificationsConnection = null;
        let catalogNotificationsRefreshTimeout = null;

        const mainModal = {
            obj: null,
            create: () => {
                mainModal.obj = new bootstrap.Modal(mainModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        const labelModal = {
            obj: null,
            create: () => {
                labelModal.obj = new bootstrap.Modal(labelModalRef.value, {
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

        const getOrderTimelineCurrentIndex = (order) => {
            if (order?.status === 'Cancelled') return -1;
            if (order?.status === 'Delivered') return 6;
            if (order?.status === 'Shipped') return 5;
            if (order?.status === 'Dispatched') return 4;
            if (order?.isMelhorEnvioGenerated || order?.melhorEnvioGeneratedAt) return 3;
            if (order?.isMelhorEnvioCheckedOut || order?.melhorEnvioCheckoutAt || order?.isMelhorEnvioCartAdded || order?.melhorEnvioCartId) return 2;
            if (order?.status === 'Paid') return 1;

            return 0;
        };

        const getOrderTimelineCurrent = (order) => {
            if (order?.status === 'Cancelled') {
                return {
                    key: 'Cancelled',
                    label: translateStatus('Cancelled'),
                    description: 'Pedido cancelado. A timeline de envio foi interrompida.'
                };
            }

            const currentIndex = getOrderTimelineCurrentIndex(order);
            return orderTimelineSteps[currentIndex] ?? orderTimelineSteps[0];
        };

        const getOrderTimelineSteps = (order) => {
            const currentIndex = getOrderTimelineCurrentIndex(order);

            return orderTimelineSteps.map((step, index) => ({
                ...step,
                index: index + 1,
                isComplete: currentIndex >= 0 && index < currentIndex,
                isCurrent: index === currentIndex,
                isLast: index === orderTimelineSteps.length - 1
            }));
        };

        const methods = {
            updateSummaryCards: () => {
                const total = state.mainData.length;
                const pending = state.mainData.filter(x => x?.status === 'Pending').length;
                const paid = state.mainData.filter(x => x?.status === 'Paid').length;
                const cancelled = state.mainData.filter(x => x?.status === 'Cancelled').length;

                state.summary = { total, pending, paid, cancelled };
            },
            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = (response?.data?.content?.data ?? []).map(item => ({
                    ...item,
                    orderDate: parseUtcDate(item.orderDate),
                    createdAt: parseUtcDate(item.createdAt)
                }));
                state.pagination.page = Math.min(state.pagination.page, Math.max(totalPages.value, 1));
                methods.updateSummaryCards();
            },
            connectOrderNotifications: async () => {
                if (!window.signalR || orderNotificationsConnection) {
                    return;
                }

                orderNotificationsConnection = new signalR.HubConnectionBuilder()
                    .withUrl('/hubs/orders', {
                        accessTokenFactory: () => StorageManager.getAccessToken() ?? ''
                    })
                    .withAutomaticReconnect()
                    .build();

                orderNotificationsConnection.on('OrderChanged', () => {
                    clearTimeout(orderNotificationsRefreshTimeout);
                    orderNotificationsRefreshTimeout = setTimeout(async () => {
                        try {
                            await methods.populateMainData();
                            mainGrid.refresh();
                        } catch (error) {
                            console.error('Nao foi possivel atualizar os pedidos em tempo real.', error);
                        }
                    }, 250);
                });

                try {
                    await orderNotificationsConnection.start();
                } catch (error) {
                    console.error('Nao foi possivel conectar as notificacoes de pedidos.', error);
                    orderNotificationsConnection = null;
                }
            },
            formatOrderDate: (order) => {
                const date = parseUtcDate(order?.orderDate);
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
            formatRowShippingBoxData: (order) => formatRowShippingBoxData(order),
            getSortIcon: (field) => {
                if (state.sort.field !== field) return 'fa-sort';

                return state.sort.direction === 'asc'
                    ? 'fa-sort-up'
                    : 'fa-sort-down';
            },
            getOrderTimelineCurrent: (order) => getOrderTimelineCurrent(order),
            getOrderTimelineSteps: (order) => getOrderTimelineSteps(order),
            getOrderTimelineTitle: (order) => {
                const current = getOrderTimelineCurrent(order);
                const steps = getOrderTimelineSteps(order)
                    .map(step => `${step.index}. ${step.label}: ${step.description}`)
                    .join('\n');

                return `${current.label}\n${current.description}\n\n${steps}`;
            },
            getSortValue: (order, field) => {
                if (field === 'orderDate') {
                    const date = parseUtcDate(order?.orderDate);
                    return date ? date.getTime() : 0;
                }

                if (field === 'customerName') return String(order?.customerName ?? '').toLowerCase();
                if (field === 'status') return getOrderTimelineCurrentIndex(order);

                return '';
            },
            getOrderShippingCost: (order) => Number(order?.shippingCost ?? 0),
            getOrderFinalTotal: (order) => {
                const itemsTotal = Number(order?.itemsTotal ?? 0);
                const totalWithShipping = Number(order?.totalWithShipping ?? 0);
                const totalAmount = Number(order?.totalAmount ?? 0);
                const shippingCost = Number(order?.shippingCost ?? 0);

                if (totalWithShipping > 0) return totalWithShipping;
                if (itemsTotal > 0 || shippingCost > 0) return itemsTotal + shippingCost;
                if (totalAmount > 0) return totalAmount;

                return shippingCost;
            },
            getOrderItemsTotal: (order) => {
                const explicitItemsTotal = Number(order?.itemsTotal ?? 0);
                if (explicitItemsTotal > 0) return explicitItemsTotal;

                const itemsTotal = methods.getOrderFinalTotal(order) - methods.getOrderShippingCost(order);
                return Math.max(itemsTotal, 0);
            },
            getTotalTitle: (order) => [
                `Roupas: ${formatCurrency(methods.getOrderItemsTotal(order))}`,
                `Frete: ${formatCurrency(methods.getOrderShippingCost(order))}`,
                `Total: ${formatCurrency(methods.getOrderFinalTotal(order))}`
            ].join('\n'),
            getPaymentMethodLabel: (order) => {
                const paymentText = [
                    order?.paymentMethod,
                    order?.payment?.paymentDetail?.paymentMethod,
                    order?.paymentTypeName,
                    order?.payment?.paymentTypeName
                ]
                    .filter(Boolean)
                    .join(' ')
                    .toLowerCase();

                if (!paymentText) return '-';
                if (paymentText.includes('pix')) return 'Pix';
                if (
                    paymentText.includes('cartao') ||
                    paymentText.includes('cartão') ||
                    paymentText.includes('credito') ||
                    paymentText.includes('crédito') ||
                    paymentText.includes('debito') ||
                    paymentText.includes('débito') ||
                    paymentText.includes('card')
                ) {
                    return 'Cartão';
                }

                return '-';
            },
            getShippingLabelTitle: (order) => {
                if (order?.status !== 'Paid') return 'Disponivel apenas para pedidos pagos';
                if (order?.isMelhorEnvioGenerated || order?.melhorEnvioGeneratedAt) return 'Etiqueta gerada no Melhor Envio';
                if (order?.isMelhorEnvioCheckedOut || order?.melhorEnvioCheckoutAt) return 'Gerar etiqueta no Melhor Envio';
                if (order?.isMelhorEnvioCartAdded || order?.melhorEnvioCartId) {
                    return `Finalizar sacolinha${order.melhorEnvioCartId ? ': ' + order.melhorEnvioCartId : ''}`;
                }

                return 'Adicionar frete a sacolinha';
            },
            getShippingLabelIcon: (order) => {
                if (order?.isMelhorEnvioGenerated || order?.melhorEnvioGeneratedAt) return 'fa-check';
                if (order?.isMelhorEnvioCheckedOut || order?.melhorEnvioCheckoutAt) return 'fa-tag';
                if (order?.isMelhorEnvioCartAdded || order?.melhorEnvioCartId) return 'fa-shopping-bag';

                return 'fa-shopping-bag';
            },
            getShippingLabelButtonClass: (order) => {
                if (order?.status !== 'Paid') return '';
                if (order?.isMelhorEnvioGenerated || order?.melhorEnvioGeneratedAt) return 'order-action-button--success';
                if (order?.isMelhorEnvioCheckedOut || order?.melhorEnvioCheckoutAt) return 'order-action-button--success';
                if (order?.isMelhorEnvioCartAdded || order?.melhorEnvioCartId) return 'order-action-button--bag-finalize';

                return 'order-action-button--bag';
            },
            getShippingLabelText: (order) => {
                if (order?.status !== 'Paid') return 'Indisponivel';
                if (order?.isMelhorEnvioGenerated || order?.melhorEnvioGeneratedAt) return 'Gerada';
                if (order?.isMelhorEnvioCheckedOut || order?.melhorEnvioCheckoutAt) return 'Gerar';
                if (order?.isMelhorEnvioCartAdded || order?.melhorEnvioCartId) return 'Finalizar';

                return 'Sacolinha';
            },
            getPagerText: () => {
                const total = sortedOrders.value.length;
                if (!total) return 'Nenhum pedido';

                const start = ((state.pagination.page - 1) * state.pagination.pageSize) + 1;
                const end = Math.min(start + state.pagination.pageSize - 1, total);
                return `${start}-${end} de ${total} pedidos`;
            },
            loadMelhorEnvioBalance: async () => {
                try {
                    state.melhorEnvioBalance.isLoading = true;
                    state.melhorEnvioBalance.error = '';

                    const response = await services.getMelhorEnvioBalance();
                    const balance = response?.data?.content?.data ?? {};

                    state.melhorEnvioBalance.balance = Number(balance.balance || 0);
                    state.melhorEnvioBalance.reserved = Number(balance.reserved || 0);
                    state.melhorEnvioBalance.debts = Number(balance.debts || 0);
                } catch (error) {
                    state.melhorEnvioBalance.error = getApiErrorMessage(error, 'Nao foi possivel carregar o saldo do Melhor Envio.');
                } finally {
                    state.melhorEnvioBalance.isLoading = false;
                }
            },
            populateProducts: async () => {
                const response = await services.getProducts();
                state.products = response?.data?.content?.data ?? [];
            },
            syncOrderItemPricesFromProducts: () => {
                let changed = false;

                state.orderDetails.forEach((item) => {
                    if (!item.productId) {
                        return;
                    }

                    const product = state.products.find(x => x.id === item.productId);
                    const productPrice = product?.unitPrice ?? 0;

                    if (Number(item.unitPrice || 0) !== Number(productPrice || 0)) {
                        item.unitPrice = productPrice;
                        changed = true;
                    }
                });

                if (changed) {
                    recalculateTotal();
                }
            },
            loadLookups: async () => {
                const [customers, shippingBoxes, paymentTypes] = await Promise.all([
                    services.getCustomers(),
                    services.getShippingBoxes(),
                    services.getPaymentTypes()
                ]);

                state.customers = customers?.data?.content?.data ?? [];
                await methods.populateProducts();
                state.shippingBoxes = shippingBoxes?.data?.content?.data ?? [];
                state.paymentTypes = paymentTypes?.data?.content?.data ?? [];
            },
            connectCatalogNotifications: async () => {
                if (!window.signalR || catalogNotificationsConnection) {
                    return;
                }

                catalogNotificationsConnection = new signalR.HubConnectionBuilder()
                    .withUrl('/hubs/catalog', {
                        accessTokenFactory: () => StorageManager.getAccessToken() ?? ''
                    })
                    .withAutomaticReconnect()
                    .build();

                catalogNotificationsConnection.on('ProductChanged', () => {
                    clearTimeout(catalogNotificationsRefreshTimeout);
                    catalogNotificationsRefreshTimeout = setTimeout(async () => {
                        try {
                            await methods.populateProducts();
                            methods.syncOrderItemPricesFromProducts();
                        } catch (error) {
                            console.error('Nao foi possivel atualizar os valores dos produtos em tempo real.', error);
                        }
                    }, 250);
                });

                try {
                    await catalogNotificationsConnection.start();
                } catch (error) {
                    console.error('Nao foi possivel conectar as notificacoes de produtos.', error);
                    catalogNotificationsConnection = null;
                }
            },
            loadOrder: async (id) => {
                const response = await services.getSingleData(id);
                const order = response?.data?.content?.data;
                if (!order) return;

                resetForm();
                fillOrder(order);
            },
            openInsertBalance: async () => {
                const result = await Swal.fire({
                    title: 'Inserir saldo',
                    html: `
                        <div class="text-start">
                            <label class="form-label" for="insert-balance-value">Valor</label>
                            <input id="insert-balance-value" type="number" min="1" step="0.01" class="form-control" placeholder="10.50">
                            <label class="form-label mt-3" for="insert-balance-slug">Forma</label>
                            <select id="insert-balance-slug" class="form-select">
                                <option value="pix">Pix</option>
                                <option value="boleto">Boleto</option>
                            </select>
                        </div>
                    `,
                    showCancelButton: true,
                    confirmButtonText: 'Inserir saldo',
                    cancelButtonText: 'Cancelar',
                    focusConfirm: false,
                    preConfirm: () => {
                        const value = parseMoneyInput(document.getElementById('insert-balance-value')?.value);
                        const slug = document.getElementById('insert-balance-slug')?.value || 'pix';

                        if (!value || value <= 0) {
                            Swal.showValidationMessage('Informe um valor maior que zero.');
                            return false;
                        }

                        return { value, slug };
                    }
                });

                if (!result.isConfirmed) return;

                try {
                    state.melhorEnvioBalance.isInserting = true;
                    state.melhorEnvioBalance.error = '';

                    const response = await services.insertMelhorEnvioBalance({
                        value: result.value.value,
                        slug: result.value.slug
                    });

                    const content = response?.data?.content ?? {};
                    await methods.loadMelhorEnvioBalance();

                    await Swal.fire({
                        icon: 'success',
                        title: 'Saldo solicitado',
                        html: buildInsertBalanceResultHtml(content),
                        confirmButtonText: 'Fechar',
                        didOpen: () => {
                            const copyButton = document.getElementById('insert-balance-copy-button');
                            const copyInput = document.getElementById('insert-balance-pix-copy');

                            if (!copyButton || !copyInput) {
                                return;
                            }

                            copyButton.addEventListener('click', async () => {
                                try {
                                    await navigator.clipboard.writeText(copyInput.value);
                                    copyButton.textContent = 'Pix copiado';
                                } catch {
                                    copyInput.select();
                                    document.execCommand('copy');
                                    copyButton.textContent = 'Pix copiado';
                                }
                            });
                        }
                    });
                } catch (error) {
                    state.melhorEnvioBalance.error = getApiErrorMessage(error, 'Nao foi possivel inserir saldo na carteira.');
                    Swal.fire({
                        icon: 'error',
                        title: 'Inserir saldo',
                        text: state.melhorEnvioBalance.error
                    });
                } finally {
                    state.melhorEnvioBalance.isInserting = false;
                }
            },
            openPixPayment: async (order) => {
                if (!order?.id) return;

                let pixResponse;
                try {
                    pixResponse = await services.createMercadoPagoPixPayment({
                        orderId: order.id,
                        description: `Pedido ${order.id}`
                    });
                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Pix',
                        text: getApiErrorMessage(error, 'Nao foi possivel gerar o Pix no Mercado Pago.')
                    });
                    return;
                }

                const content = pixResponse?.data ?? {};
                const paymentId = content.paymentId || content.payment_id;
                if (!paymentId) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Pix',
                        text: 'O Mercado Pago nao retornou o ID do pagamento.'
                    });
                    return;
                }

                let pollTimer = null;
                let countdownTimer = null;
                let settled = false;

                const stopTimers = () => {
                    if (pollTimer) clearInterval(pollTimer);
                    if (countdownTimer) clearInterval(countdownTimer);
                    pollTimer = null;
                    countdownTimer = null;
                };

                const updateExpiredUi = (message) => {
                    const statusText = document.getElementById('pix-status-text');
                    const spinner = document.getElementById('pix-status-spinner');
                    const newButton = document.getElementById('pix-new-button');

                    if (statusText) statusText.textContent = message;
                    if (spinner) spinner.classList.add('d-none');
                    if (newButton) newButton.classList.remove('d-none');
                };

                await Swal.fire({
                    title: 'Pagamento Pix Mercado Pago',
                    html: buildPixPaymentHtml(content),
                    width: 560,
                    showConfirmButton: false,
                    showCancelButton: true,
                    cancelButtonText: 'Fechar',
                    allowOutsideClick: false,
                    didOpen: () => {
                        const copyButton = document.getElementById('pix-copy-button');
                        const copyInput = document.getElementById('pix-copy-code');
                        const newButton = document.getElementById('pix-new-button');
                        const countdown = document.getElementById('pix-countdown');
                        const expiration = new Date(content.expiracao || content.expiration || '');

                        if (copyButton && copyInput) {
                            copyButton.addEventListener('click', async () => {
                                try {
                                    await navigator.clipboard.writeText(copyInput.value);
                                } catch {
                                    copyInput.select();
                                    document.execCommand('copy');
                                }

                                copyButton.textContent = 'Pix copiado';
                            });
                        }

                        if (newButton) {
                            newButton.addEventListener('click', () => {
                                stopTimers();
                                Swal.close();
                                setTimeout(() => methods.openPixPayment(order), 200);
                            });
                        }

                        const updateCountdown = () => {
                            if (!countdown || Number.isNaN(expiration.getTime())) {
                                return;
                            }

                            const remaining = expiration.getTime() - Date.now();
                            if (remaining <= 0) {
                                countdown.textContent = 'expirado';
                                updateExpiredUi('QR Code expirado.');
                                stopTimers();
                                return;
                            }

                            const minutes = Math.floor(remaining / 60000);
                            const seconds = Math.floor((remaining % 60000) / 1000);
                            countdown.textContent = `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
                        };

                        const pollStatus = async () => {
                            if (settled) return;

                            try {
                                const statusResponse = await services.getMercadoPagoPixPaymentStatus(paymentId);
                                const statusContent = statusResponse?.data ?? {};
                                const status = String(statusContent.status || '').toLowerCase();

                                if (status === 'approved') {
                                    settled = true;
                                    stopTimers();
                                    Swal.close();

                                    await methods.populateMainData();
                                    mainGrid.refresh();

                                    Swal.fire({
                                        icon: 'success',
                                        title: 'Pagamento confirmado',
                                        timer: 1800,
                                        showConfirmButton: false
                                    });
                                } else if (['expired', 'cancelled', 'rejected'].includes(status)) {
                                    settled = true;
                                    stopTimers();
                                    updateExpiredUi('Pagamento expirado ou cancelado.');
                                }
                            } catch (error) {
                                const statusText = document.getElementById('pix-status-text');
                                if (statusText) {
                                    statusText.textContent = getApiErrorMessage(error, 'Nao foi possivel consultar o pagamento.');
                                }
                            }
                        };

                        updateCountdown();
                        countdownTimer = setInterval(updateCountdown, 1000);
                        pollTimer = setInterval(pollStatus, 5000);
                    },
                    willClose: stopTimers
                });
            },
            handleCustomerChange: async () => {
                const customer = state.customers.find(x => x.id === state.customerId);
                if (!customer) {
                    state.shipping = emptyShipping();
                    recalculateTotal();
                    return;
                }

                const name = splitCustomerName(customer.name);

                state.shipping.firstName = name.firstName;
                state.shipping.lastName = name.lastName;
                state.shipping.email = customer.emailAddress ?? '';
                state.shipping.phoneNumber = customer.phoneNumber ?? '';
                state.shipping.street = customer.street ?? '';
                state.shipping.number = customer.number ?? '';
                state.shipping.neighborhood = customer.neighborhood ?? '';
                state.shipping.complement = customer.complement ?? '';
                state.shipping.city = customer.city ?? '';
                state.shipping.state = customer.state ?? '';
                state.shipping.postCode = customer.postalCode ?? '';

                await methods.refreshShippingCost();
            },
            openShippingLabel: (order) => {
                resetLabel();

                state.label.orderId = order.id ?? '';
                state.label.recipientName = order.customerName || order.shippingRecipientName || '';
                state.label.postCode = order.shippingPostCode || '';
                state.label.city = order.shippingCity || '';
                state.label.state = order.shippingState || '';
                state.label.dimensions = formatRowShippingBoxData(order);
                state.label.weight = Number(order.shippingBoxWeight || 0);
                state.label.shippingCost = Number(order.shippingCost || 0);
                state.label.totalAmount = Number(order.totalAmount || 0);
                state.label.labelId = order.melhorEnvioCartId || '';
                state.label.cartAddedAt = order.melhorEnvioCartAddedAt || '';
                state.label.checkoutAt = order.melhorEnvioCheckoutAt || '';
                state.label.generatedAt = order.melhorEnvioGeneratedAt || '';

                labelModal.obj.show();
            },
            buyShippingCart: async () => {
                if (!state.label.orderId || !state.label.labelId || state.label.checkoutAt) return;

                const confirm = await Swal.fire({
                    icon: 'question',
                    title: 'Finalizar sacolinha?',
                    text: `Finalizar a sacolinha do Melhor Envio para o pedido ${state.label.orderId}?`,
                    showCancelButton: true,
                    confirmButtonText: 'Finalizar',
                    cancelButtonText: 'Cancelar'
                });

                if (!confirm.isConfirmed) return;

                try {
                    state.label.isBuying = true;
                    state.label.error = '';

                    const response = await services.buyShippingCart({
                        orderId: state.label.orderId
                    });

                    state.label.rawResponse = response?.data?.content?.rawResponse ?? '';
                    state.label.checkoutAt = new Date().toISOString();

                    const current = state.mainData.find(item => item.id === state.label.orderId);
                    if (current) {
                        current.melhorEnvioCheckoutAt = state.label.checkoutAt;
                        current.isMelhorEnvioCheckedOut = true;
                        current.isMelhorEnvioGenerated = false;
                        mainGrid.refresh();
                    }

                    await methods.loadMelhorEnvioBalance();
                } catch (error) {
                    state.label.error = getApiErrorMessage(error, 'Nao foi possivel finalizar a sacolinha.');
                } finally {
                    state.label.isBuying = false;
                }
            },
            generatePurchasedShippingLabel: async () => {
                if (!state.label.orderId || !state.label.labelId || !state.label.checkoutAt || state.label.generatedAt) return;

                const confirm = await Swal.fire({
                    icon: 'question',
                    title: 'Gerar etiqueta?',
                    text: `Gerar a etiqueta no Melhor Envio para o pedido ${state.label.orderId}?`,
                    showCancelButton: true,
                    confirmButtonText: 'Gerar',
                    cancelButtonText: 'Cancelar'
                });

                if (!confirm.isConfirmed) return;

                try {
                    state.label.isGeneratingPurchased = true;
                    state.label.error = '';

                    const response = await services.generatePurchasedShippingLabel({
                        orderId: state.label.orderId
                    });

                    state.label.rawResponse = response?.data?.content?.rawResponse ?? '';
                    state.label.generatedAt = new Date().toISOString();

                    const current = state.mainData.find(item => item.id === state.label.orderId);
                    if (current) {
                        current.melhorEnvioGeneratedAt = state.label.generatedAt;
                        current.isMelhorEnvioGenerated = true;
                        mainGrid.refresh();
                    }
                } catch (error) {
                    state.label.error = getApiErrorMessage(error, 'Nao foi possivel gerar a etiqueta.');
                } finally {
                    state.label.isGeneratingPurchased = false;
                }
            },
            downloadShippingLabel: async () => {
                if (!state.label.labelId) return;

                try {
                    state.label.isDownloading = true;
                    state.label.error = '';

                    const response = await services.downloadShippingLabel(state.label.labelId);
                    const blob = new Blob([response.data], { type: 'application/pdf' });
                    const url = window.URL.createObjectURL(blob);
                    const link = document.createElement('a');
                    link.href = url;
                    link.download = `etiqueta-${state.label.orderId || state.label.labelId}.pdf`;
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);
                    window.URL.revokeObjectURL(url);
                } catch (error) {
                    state.label.error = getApiErrorMessage(error, 'Nao foi possivel baixar a etiqueta.');
                } finally {
                    state.label.isDownloading = false;
                }
            },
            markShippingCart: async () => {
                if (!state.label.orderId || state.label.labelId) return;

                const result = await Swal.fire({
                    title: 'Codigo da sacolinha',
                    input: 'text',
                    inputLabel: 'Informe o codigo/id retornado pelo Melhor Envio',
                    inputPlaceholder: 'Ex: etiqueta ou order id da sacolinha',
                    showCancelButton: true,
                    confirmButtonText: 'Marcar',
                    cancelButtonText: 'Cancelar',
                    inputValidator: (value) => {
                        if (!value || !value.trim()) {
                            return 'Informe o codigo da sacolinha.';
                        }
                        return null;
                    }
                });

                if (!result.isConfirmed) return;

                try {
                    state.label.isGenerating = true;
                    state.label.error = '';

                    const response = await services.markShippingCart({
                        orderId: state.label.orderId,
                        cartId: result.value.trim()
                    });

                    const cartId = response?.data?.content?.cartId ?? result.value.trim();
                    state.label.labelId = cartId;
                    state.label.cartAddedAt = new Date().toISOString();

                    const current = state.mainData.find(item => item.id === state.label.orderId);
                    if (current) {
                        current.melhorEnvioCartId = cartId;
                        current.melhorEnvioCartAddedAt = state.label.cartAddedAt;
                        current.isMelhorEnvioCartAdded = true;
                        current.isMelhorEnvioCheckedOut = false;
                        current.isMelhorEnvioGenerated = false;
                        mainGrid.refresh();
                    }

                    await methods.loadMelhorEnvioBalance();
                } catch (error) {
                    state.label.error = getApiErrorMessage(error, 'Nao foi possivel marcar o pedido como adicionado a sacolinha.');
                } finally {
                    state.label.isGenerating = false;
                }
            },
            addOrderItem: () => {
                state.orderDetails.push({
                    id: '',
                    productId: '',
                    quantity: 1,
                    unitPrice: 0
                });
            },
            removeOrderItem: (index) => {
                state.orderDetails.splice(index, 1);
                recalculateTotal();
            },
            handleProductChange: (item) => {
                const product = state.products.find(x => x.id === item.productId);
                item.unitPrice = product?.unitPrice ?? 0;
                recalculateTotal();
            },
            refreshShippingCost: async () => {
                recalculateTotal(calculateShippingCost());

                if (!state.shippingBoxId || !state.shipping.postCode) {
                    return;
                }

                try {
                    const response = await services.calculateShippingCost(state.shippingBoxId, state.shipping.postCode);
                    const shippingCost = Number(response?.data?.content?.data ?? state.shippingCost);
                    recalculateTotal(shippingCost);
                } catch {
                    recalculateTotal();
                }
            },
            handleShippingBoxChange: async () => {
                await methods.refreshShippingCost();
            },
            showShippingBoxInfo: () => {
                const shippingBox = getSelectedShippingBox();
                if (!shippingBox) {
                    Swal.fire({
                        icon: 'info',
                        title: 'Caixa de Envio',
                        text: 'Selecione uma caixa de envio primeiro.'
                    });
                    return;
                }

                Swal.fire({
                    title: 'Caixa de Envio',
                    html: `
                        <div class="table-responsive">
                            <table class="table table-sm table-bordered text-start">
                                <tbody>
                                    <tr><th>Largura</th><td>${formatNumber(shippingBox.width)} cm</td></tr>
                                    <tr><th>Comprimento</th><td>${formatNumber(shippingBox.length)} cm</td></tr>
                                    <tr><th>Altura</th><td>${formatNumber(shippingBox.height)} cm</td></tr>
                                    <tr><th>Peso</th><td>${formatNumber(shippingBox.weight)} kg</td></tr>
                                    <tr><th>Peso Cubico</th><td>${formatNumber(calculateCubicWeight(shippingBox))} kg</td></tr>
                                    <tr><th>Peso Cobrado</th><td>${formatNumber(calculateChargedWeight(shippingBox))} kg</td></tr>
                                    <tr><th>Valor do Seguro</th><td>${formatCurrency(shippingBox.insuranceValue)}</td></tr>
                                    <tr><th>Frete</th><td>${formatCurrency(state.shippingCost)}</td></tr>
                                </tbody>
                            </table>
                        </div>
                    `,
                    width: 560,
                    confirmButtonText: 'Fechar'
                });
            },
            showOrderDetails: async (id) => {
                window.AdminSidebarNotifications?.markViewed('orders', id);
                try {
                    const response = await services.getSingleData(id);
                    const order = response?.data?.content?.data;
                    const items = order?.orderDetails ?? [];

                    if (!items.length) {
                        return Swal.fire({ icon: 'info', title: 'Detalhes do Pedido', text: 'Nenhum item encontrado para este pedido.' });
                    }

                    const itemCount = getOrderItemCount(order);
                    const itemCards = items.map((item) => `
                        <div class="order-detail-card">
                            <img class="order-detail-card__image"
                                 src="${getProductImageUrl(item.productImageUrl)}"
                                 alt="${escapeHtml(item.productName || 'Roupa')}"
                                 onerror="this.onerror=null;this.src='/noimage.png';">
                            <div class="order-detail-card__body">
                                <div class="order-detail-card__name">${escapeHtml(item.productName || 'Desconhecido')}</div>
                                <div class="order-detail-card__meta">
                                    <span>Qtd: ${Number(item.quantity || 0)}</span>
                                    <span>${formatCurrency(item.unitPrice)}</span>
                                    <span>Total: ${formatCurrency(item.totalPrice)}</span>
                                </div>
                            </div>
                        </div>
                    `).join('');

                    await Swal.fire({
                        title: `${itemCount} ${itemCount === 1 ? 'roupa' : 'roupas'} no pedido`,
                        html: `
                            <style>
                                .order-detail-grid {
                                    display: grid;
                                    gap: 12px;
                                    grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
                                    text-align: left;
                                }

                                .order-detail-card {
                                    background: #fff;
                                    border: 1px solid #e5e7eb;
                                    border-radius: 8px;
                                    overflow: hidden;
                                }

                                .order-detail-card__image {
                                    aspect-ratio: 4 / 5;
                                    background: #f3f4f6;
                                    display: block;
                                    object-fit: cover;
                                    width: 100%;
                                }

                                .order-detail-card__body {
                                    padding: 10px;
                                }

                                .order-detail-card__name {
                                    color: #111827;
                                    font-size: 14px;
                                    font-weight: 800;
                                    line-height: 1.25;
                                    min-height: 36px;
                                }

                                .order-detail-card__meta {
                                    color: #6b7280;
                                    display: grid;
                                    font-size: 12px;
                                    font-weight: 700;
                                    gap: 4px;
                                    margin-top: 8px;
                                }
                            </style>
                            <div class="order-detail-grid">
                                ${itemCards}
                            </div>
                        `,
                        width: 780,
                        confirmButtonText: 'Fechar'
                    });
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'Erro', text: error.response?.data?.message ?? 'Nao foi possivel carregar os detalhes do pedido' });
                }
            }
        };

        const sortedOrders = Vue.computed(() => {
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
            Math.max(Math.ceil(sortedOrders.value.length / state.pagination.pageSize), 1)
        );

        const pagedOrders = Vue.computed(() => {
            if (state.pagination.page > totalPages.value) {
                state.pagination.page = totalPages.value;
            }

            const start = (state.pagination.page - 1) * state.pagination.pageSize;
            return sortedOrders.value.slice(start, start + state.pagination.pageSize);
        });

        const handler = {
            handleSort: (field) => {
                if (state.sort.field === field) {
                    state.sort.direction = state.sort.direction === 'asc' ? 'desc' : 'asc';
                } else {
                    state.sort.field = field;
                    state.sort.direction = field === 'orderDate' ? 'desc' : 'asc';
                }

                state.pagination.page = 1;
            },
            handlePreviousPage: () => {
                state.pagination.page = Math.max(state.pagination.page - 1, 1);
            },
            handleNextPage: () => {
                state.pagination.page = Math.min(state.pagination.page + 1, totalPages.value);
            },
            handleNew: () => {
                resetForm();
                state.deleteMode = false;
                state.mainTitle = 'Adicionar Pedido';
                methods.addOrderItem();
                mainModal.obj.show();
            },
            handleDelete: async (order) => {
                if (!order?.id) return;

                await methods.loadOrder(order.id);
                state.deleteMode = true;
                state.mainTitle = 'Excluir Pedido';
                mainModal.obj.show();
            },
            handleGenerateShippingLabel: async () => {
                if (!state.label.orderId) return;

                try {
                    state.label.isGenerating = true;
                    state.label.error = '';
                    state.label.labelId = '';
                    state.label.rawResponse = '';

                    const response = await services.generateShippingLabel({
                        orderId: state.label.orderId
                    });

                    const result = response?.data?.content ?? {};
                    state.label.labelId = result.cartId ?? result.labelId ?? '';
                    state.label.rawResponse = result.rawResponse ?? '';
                    state.label.cartAddedAt = new Date().toISOString();

                    const current = state.mainData.find(item => item.id === state.label.orderId);
                    if (current) {
                        current.melhorEnvioCartId = state.label.labelId;
                        current.melhorEnvioCartAddedAt = state.label.cartAddedAt;
                        current.isMelhorEnvioCartAdded = true;
                        current.isMelhorEnvioCheckedOut = false;
                        current.isMelhorEnvioGenerated = false;
                        mainGrid.refresh();
                    }

                    await methods.loadMelhorEnvioBalance();

                    if (!state.label.labelId) {
                        state.label.error = 'Frete adicionado a sacolinha, mas o Melhor Envio nao retornou o codigo da etiqueta.';
                    }
                } catch (error) {
                    state.label.error = getApiErrorMessage(error, 'Nao foi possivel adicionar o frete a sacolinha.');
                } finally {
                    state.label.isGenerating = false;
                }
            },
            handleSubmit: async () => {
                try {
                    state.isSubmitting = true;
                    state.errors.customerId = '';
                    state.errors.orderDetails = '';

                    if (state.deleteMode) {
                        const deleteResponse = await services.deleteMainData(state.id);

                        if (deleteResponse.data.code === 200) {
                            await methods.populateMainData();
                            mainGrid.refresh();
                            Swal.fire({ icon: 'success', title: 'Excluido com Sucesso', timer: 1000, showConfirmButton: false });
                            setTimeout(() => mainModal.obj.hide(), 1000);
                        }

                        return;
                    }

                    if (!state.customerId) {
                        state.errors.customerId = 'Cliente e obrigatorio.';
                        return;
                    }

                    if (!state.orderDetails.length || state.orderDetails.some(x => !x.productId)) {
                        state.errors.orderDetails = 'Adicione pelo menos um produto.';
                        return;
                    }

                    await methods.refreshShippingCost();
                    const request = buildPayload();
                    const response = state.id
                        ? await services.updateMainData(request)
                        : await services.createMainData(request);

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();

                        Swal.fire({ icon: 'success', title: 'Salvo com Sucesso', timer: 1000, showConfirmButton: false });
                        setTimeout(() => mainModal.obj.hide(), 1000);
                    } else {
                        Swal.fire({ icon: 'error', title: 'Falha ao Salvar', text: response.data.message ?? 'Erro' });
                    }
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'Erro', text: error.response?.data?.message ?? 'Erro inesperado' });
                } finally {
                    state.isSubmitting = false;
                }
            },
            recalculateTotal
        };

        const formatDateTimeValue = (value) => {
            if (!value) return '';
            const date = parseUtcDate(value);
            if (!date) return '';

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

            return `${parts.year}-${parts.month}-${parts.day}T${parts.hour}:${parts.minute}`;
        };

        const formatDate = (value) => {
            if (!value) return '';
            const date = parseUtcDate(value);
            if (!date) return '';
            return date.toLocaleString('pt-BR', {
                timeZone: BRASILIA_TIME_ZONE,
                day: '2-digit',
                month: '2-digit',
                year: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
            });
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['Orders']);
                await SecurityManager.validateToken();

                mainModal.create();
                labelModal.create();

                mainModalRef.value.addEventListener('hidden.bs.modal', () => {
                    resetForm();
                });

                labelModalRef.value.addEventListener('hidden.bs.modal', () => {
                    resetLabel();
                });

                try {
                    await methods.populateMainData();
                    mainGrid.refresh();
                    await methods.connectOrderNotifications();
                    await methods.connectCatalogNotifications();
                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Pedidos',
                        text: error.response?.data?.message ?? 'Nao foi possivel carregar os pedidos.'
                    });
                }

                await methods.loadMelhorEnvioBalance();

                try {
                    await methods.loadLookups();
                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Dados do Pedido',
                        text: error.response?.data?.message ?? 'Nao foi possivel carregar clientes, produtos, caixas de envio ou formas de pagamento.'
                    });
                }
            } catch (e) {
                console.error(e);
                Swal.fire({
                    icon: 'error',
                    title: 'Pedidos',
                    text: e.response?.data?.message ?? e.message ?? 'Nao foi possivel abrir a pagina de pedidos.'
                });
            }
        });

        return {
            state,
            mainModalRef,
            labelModalRef,
            sortedOrders,
            pagedOrders,
            totalPages,
            handler,
            methods,
            translateStatus,
            formatDate,
            calculateItemsTotal,
            calculateCubicWeight,
            calculateChargedWeight,
            getSelectedShippingBox,
            getItemShippingCost,
            getItemTotal,
            formatCurrency,
            formatNumber,
            formatShippingBoxData
        };
    }
};

Vue.createApp(App).mount('#app');
