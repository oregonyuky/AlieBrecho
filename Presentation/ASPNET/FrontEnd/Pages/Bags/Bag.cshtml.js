const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
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
            isSubmitting: false
        });

        const mainGridRef = Vue.ref(null);
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
            obj: null,
            create: async (dataSource) => {
                mainGrid.obj = new ej.grids.Grid({
                    height: '280px',
                    dataSource,
                    allowFiltering: true,
                    showColumnMenu: true,
                    gridLines: 'None',
                    filterSettings: { type: 'Menu' },
                    allowSorting: true,
                    allowPaging: true,
                    allowSelection: true,
                    allowResizing: true,
                    pageSettings: { pageSize: 30 },
                    selectionSettings: { type: 'Single', mode: 'Row' },
                    columns: [
                        { type: 'checkbox', width: 60 },
                        { field: 'id', isPrimaryKey: true, visible: false },
                        { field: 'customerName', headerText: 'Cliente', width: 180 },
                        { field: 'statusDisplay', headerText: 'Status', width: 120 },
                        { field: 'totalItemsValue', headerText: 'Total dos Itens', width: 130, format: 'C2' },
                        { field: 'shippingCost', headerText: 'Frete', width: 120, format: 'C2' },
                        { field: 'totalWeight', headerText: 'Peso', width: 110 },
                        { field: 'itemCount', headerText: 'Itens', width: 90 },
                        {
                            headerText: 'Detalhes', width: 120, textAlign: 'Center', template: '<button type="button" class="btn btn-sm btn-outline-primary bag-detail-btn" title="Ver itens da sacola"><i class="fa fa-list"></i></button>'
                        },
                        { field: 'lastInteractionAt', headerText: 'Ultima Interacao', width: 180, format: 'yyyy-MM-dd HH:mm' }
                    ],
                    toolbar: [
                        'Search',
                        { type: 'Separator' },
                        { text: 'Editar', tooltipText: 'Editar', prefixIcon: 'e-edit', id: 'EditCustom' }
                    ],
                    dataBound: function () {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom'], false);
                    },
                    rowSelected: () => {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom'], true);
                    },
                    rowDeselected: () => {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom'], false);
                    },
                    rowDataBound: (args) => {
                        const button = args.row.querySelector('.bag-detail-btn');
                        if (button) {
                            button.addEventListener('click', async (event) => {
                                event.stopPropagation();
                                await methods.showBagDetails(args.data.id);
                            });
                        }
                    },
                    toolbarClick: async (args) => {
                        if (args.item.id === 'EditCustom') {
                            const selected = mainGrid.obj.getSelectedRecords()[0];
                            if (!selected) return;

                            await methods.loadBag(selected.id);
                            mainModal.obj.show();
                        }
                    }
                });

                mainGrid.obj.appendTo(mainGridRef.value);
            },
            refresh: () => {
                mainGrid.obj.setProperties({ dataSource: state.mainData });
            }
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

        const methods = {
            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = (response?.data?.content?.data ?? []).map((item) => ({
                    ...item,
                    statusDisplay: translateBagStatus(item.status),
                    lastInteractionAt: item.lastInteractionAt ? new Date(item.lastInteractionAt) : null
                }));
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

        const handler = {
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
            await mainGrid.create(state.mainData);
            mainModal.create();
        });

        return {
            state,
            mainGridRef,
            mainModalRef,
            translateBagStatus,
            handler
        };
    }
};

Vue.createApp(App).mount('#app');
