const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            deleteMode: false,
            mainTitle: 'Edit ShippingBox',
            id: '',
            width: null,
            length: null,
            height: null,
            weight: null,
            insuranceValue: null,
            isActive: true,
            errors: {
                width: '',
                length: '',
                height: '',
                weight: '',
                insuranceValue: ''
            },
            isSubmitting: false
        });

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);

        const services = {
            getMainData: async () => {
                return await AxiosManager.get('/ShippingBox/GetShippingBoxList', {});
            },
            createMainData: async (data) => {
                return await AxiosManager.post('/ShippingBox/CreateShippingBox', data);
            },
            updateMainData: async (data) => {
                return await AxiosManager.post('/ShippingBox/UpdateShippingBox', data);
            },
            deleteMainData: async (id) => {
                return await AxiosManager.post('/ShippingBox/DeleteShippingBox', { id });
            }
        };

        const mainGrid = {
            obj: null,
            create: async (dataSource) => {
                mainGrid.obj = new ej.grids.Grid({
                    height: '240px',
                    dataSource: dataSource,
                    allowFiltering: true,
                    showColumnMenu: true,
                    gridLines: 'None',
                    allowSorting: true,
                    allowPaging: true,
                    allowResizing: true,
                    allowExcelExport: true,
                    allowSelection: true,
                    filterSettings: { type: 'Menu' },
                    pageSettings: { pageSize: 50 },
                    selectionSettings: { type: 'Single' },
                    columns: [
                        { type: 'checkbox', width: 60 },
                        { field: 'id', isPrimaryKey: true, visible: false },
                        { field: 'width', headerText: 'Width', width: 120 },
                        { field: 'length', headerText: 'Length', width: 120 },
                        { field: 'height', headerText: 'Height', width: 120 },
                        { field: 'weight', headerText: 'Weight', width: 120 },
                        { field: 'insuranceValue', headerText: 'Insurance', width: 140 },
                        { field: 'isActive', headerText: 'Active', width: 100 },
                        { field: 'createdAt', headerText: 'Created At', width: 180, format: 'yyyy-MM-dd HH:mm' }
                    ],
                    toolbar: [
                        'ExcelExport', 'Search',
                        { type: 'Separator' },
                        { text: 'Add', prefixIcon: 'e-add', id: 'AddCustom' },
                        { text: 'Edit', prefixIcon: 'e-edit', id: 'EditCustom' },
                        { text: 'Delete', prefixIcon: 'e-delete', id: 'DeleteCustom' }
                    ],
                    dataBound: () => {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom'], false);
                        mainGrid.obj.toolbarModule.enableItems(['DeleteCustom'], false);
                    },
                    rowSelected: () => {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom'], true);
                        mainGrid.obj.toolbarModule.enableItems(['DeleteCustom'], true);
                    },
                    rowDeselected: () => {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom'], false);
                        mainGrid.obj.toolbarModule.enableItems(['DeleteCustom'], false);
                    },
                    toolbarClick: (args) => {
                        if (args.item.id?.toLowerCase().includes('excelexport')) {
                            mainGrid.obj.excelExport({ fileName: 'ShippingBoxes.xlsx' });
                        }

                        const selected = mainGrid.obj.getSelectedRecords()[0];

                        if (args.item.id === 'AddCustom') {
                            Object.assign(state, {
                                deleteMode: false,
                                mainTitle: 'Add ShippingBox',
                                id: '',
                                width: null,
                                length: null,
                                height: null,
                                weight: null,
                                insuranceValue: null,
                                isActive: true
                            });
                            mainModal.obj.show();
                        }

                        if (args.item.id === 'EditCustom' && selected) {
                            Object.assign(state, {
                                deleteMode: false,
                                mainTitle: 'Edit ShippingBox',
                                id: selected.id,
                                width: selected.width,
                                length: selected.length,
                                height: selected.height,
                                weight: selected.weight,
                                insuranceValue: selected.insuranceValue,
                                isActive: selected.isActive
                            });
                            mainModal.obj.show();
                        }

                        if (args.item.id === 'DeleteCustom' && selected) {
                            Object.assign(state, {
                                deleteMode: true,
                                mainTitle: 'Delete ShippingBox',
                                id: selected.id,
                                width: selected.width,
                                length: selected.length,
                                height: selected.height,
                                weight: selected.weight,
                                insuranceValue: selected.insuranceValue,
                                isActive: selected.isActive
                            });
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

        const methods = {
            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = response?.data?.content?.data.map(item => ({
                    ...item,
                    createdAt: new Date(item.createdAt)
                }));
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

        const handler = {
            handleSubmit: async () => {
                try {
                    state.isSubmitting = true;

                    if (state.deleteMode) {
                        await services.deleteMainData(state.id);
                    } else {
                        const payload = {
                            id: state.id,
                            width: state.width,
                            length: state.length,
                            height: state.height,
                            weight: state.weight,
                            insuranceValue: state.insuranceValue,
                            isActive: state.isActive
                        };

                        if (state.id) {
                            await services.updateMainData(payload);
                        } else {
                            await services.createMainData(payload);
                        }
                    }

                    await methods.populateMainData();
                    mainGrid.refresh();
                    mainModal.obj.hide();

                    Swal.fire({
                        icon: 'success',
                        title: 'Success',
                        timer: 1000,
                        showConfirmButton: false
                    });

                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Error',
                        text: error.response?.data?.message ?? 'Unexpected error'
                    });
                } finally {
                    state.isSubmitting = false;
                }
            }
        };

        Vue.onMounted(async () => {
            await SecurityManager.authorizePage(['ShippingBoxes']);
            await SecurityManager.validateToken();

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