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
            updateMainData: async (payload) => AxiosManager.post('/Bag/UpdateBag', payload)
        };

        const mainGrid = {
            refresh: () => {}
        };

        const mainModal = {
            obj: null,
            create: () => {
                mainModal.obj = new bootstrap.Modal(mainModalRef.value, {
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
            showBagDetails: async (id) => {
                try {
                    const response = await services.getSingleData(id);
                    const bag = response?.data?.content?.data;
                    const items = bag?.items ?? [];

                    if (!items.length) {
                        return Swal.fire({ icon: 'info', title: 'Detalhes da Sacola', text: 'Nenhum item encontrado para esta sacola.' });
                    }

                    const itemCards = items.map((item) => `
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
                                    <span>${item.isPaid ? 'Pago' : 'Pendente'}</span>
                                </div>
                            </div>
                        </div>
                    `).join('');

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
            mainModal.create();
        });

        return {
            state,
            mainModalRef,
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
