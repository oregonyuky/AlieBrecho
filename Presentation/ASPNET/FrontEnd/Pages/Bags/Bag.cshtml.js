const App = {
    setup() {
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

        const formatDateTimeValue = (value) => {
            if (!value) return '';
            const date = new Date(value);
            const pad = (n) => String(n).padStart(2, '0');
            return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
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
                    createdAt: item.createdAt ? new Date(item.createdAt) : null,
                    lastInteractionAt: item.lastInteractionAt ? new Date(item.lastInteractionAt) : null
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

                const date = new Date(value);
                if (Number.isNaN(date.getTime())) {
                    return {
                        date: '-',
                        time: ''
                    };
                }

                return {
                    date: date.toLocaleDateString('pt-BR'),
                    time: date.toLocaleTimeString('pt-BR', {
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
                    const date = new Date(bag?.createdAt);
                    return Number.isNaN(date.getTime()) ? 0 : date.getTime();
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

                    const rows = items.map((item) => `
                        <tr>
                            <td>${item.productName || 'Desconhecido'}</td>
                            <td class="text-end">${item.quantity}</td>
                            <td class="text-end">${Number(item.price || 0).toFixed(2)}</td>
                            <td class="text-end">${Number(item.weight || 0).toFixed(3)}</td>
                            <td class="text-center">${item.isPaid ? 'Sim' : 'Nao'}</td>
                        </tr>
                    `).join('');

                    await Swal.fire({
                        title: 'Itens da Sacola',
                        html: `
                            <div class="table-responsive">
                                <table class="table table-sm table-bordered">
                                    <thead>
                                        <tr>
                                            <th>Produto</th>
                                            <th class="text-end">Qtd</th>
                                            <th class="text-end">Preco</th>
                                            <th class="text-end">Peso</th>
                                            <th class="text-center">Pago</th>
                                        </tr>
                                    </thead>
                                    <tbody>${rows}</tbody>
                                </table>
                            </div>
                        `,
                        width: 760,
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
