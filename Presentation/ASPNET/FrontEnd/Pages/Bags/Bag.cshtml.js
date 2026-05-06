const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            mainTitle: 'Edit Bag',
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
                        { field: 'customerName', headerText: 'Customer', width: 180 },
                        { field: 'status', headerText: 'Status', width: 120 },
                        { field: 'totalItemsValue', headerText: 'Items Total', width: 130, format: 'C2' },
                        { field: 'shippingCost', headerText: 'Shipping', width: 120, format: 'C2' },
                        { field: 'totalWeight', headerText: 'Weight', width: 110 },
                        { field: 'itemCount', headerText: 'Items', width: 90 },
                        {
                            headerText: 'Details', width: 120, textAlign: 'Center', template: '<button type="button" class="btn btn-sm btn-outline-primary bag-detail-btn" title="Show bag items"><i class="fa fa-list"></i></button>'
                        },
                        { field: 'lastInteractionAt', headerText: 'Last Interaction', width: 180, format: 'yyyy-MM-dd HH:mm' }
                    ],
                    toolbar: [
                        'Search',
                        { type: 'Separator' },
                        { text: 'Edit', tooltipText: 'Edit', prefixIcon: 'e-edit', id: 'EditCustom' }
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
                        return Swal.fire({ icon: 'info', title: 'Bag Details', text: 'No items found for this bag.' });
                    }

                    const rows = items.map((item) => `
                        <tr>
                            <td>${item.productName || 'Unknown'}</td>
                            <td class="text-end">${item.quantity}</td>
                            <td class="text-end">${Number(item.price || 0).toFixed(2)}</td>
                            <td class="text-end">${Number(item.weight || 0).toFixed(3)}</td>
                            <td class="text-center">${item.isPaid ? 'Yes' : 'No'}</td>
                        </tr>
                    `).join('');

                    await Swal.fire({
                        title: 'Bag Items',
                        html: `
                            <div class="table-responsive">
                                <table class="table table-sm table-bordered">
                                    <thead>
                                        <tr>
                                            <th>Product</th>
                                            <th class="text-end">Qty</th>
                                            <th class="text-end">Price</th>
                                            <th class="text-end">Weight</th>
                                            <th class="text-center">Paid</th>
                                        </tr>
                                    </thead>
                                    <tbody>${rows}</tbody>
                                </table>
                            </div>
                        `,
                        width: 760,
                        confirmButtonText: 'Close'
                    });
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'Error', text: error.response?.data?.message ?? 'Unable to load bag details' });
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
                            title: 'Success',
                            text: 'Bag updated successfully',
                            timer: 1200,
                            showConfirmButton: false
                        });
                        mainModal.obj.hide();
                    }
                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Error',
                        text: error.response?.data?.message ?? 'Unable to update bag'
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
            handler
        };
    }
};

Vue.createApp(App).mount('#app');
