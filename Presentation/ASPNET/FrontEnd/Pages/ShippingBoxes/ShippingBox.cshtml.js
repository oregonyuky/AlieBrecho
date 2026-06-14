const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            summary: {
                total: 0,
                active: 0,
                inactive: 0
            },
            deleteMode: false,
            mainTitle: 'Editar Caixa de Envio',
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
                        { field: 'width', headerText: 'Largura', width: 120 },
                        { field: 'length', headerText: 'Comprimento', width: 120 },
                        { field: 'height', headerText: 'Altura', width: 120 },
                        { field: 'weight', headerText: 'Peso', width: 120 },
                        { field: 'insuranceValue', headerText: 'Seguro', width: 140 },
                        {
                            field: 'isActive',
                            headerText: 'Ativo',
                            width: 100,
                            textAlign: 'Center',
                            disableHtmlEncode: false,
                            valueAccessor: (_, data) =>
                                data?.isActive === true
                                    ? '<span title="Sim" style="color:#198754;font-size:16px;">&#10004;</span>'
                                    : '<span title="Nao" style="color:#dc3545;font-size:16px;">&#10006;</span>'
                        },
                        { field: 'createdAt', headerText: 'Criado Em', width: 180, format: 'dd/MM/yyyy HH:mm' }
                    ],
                    toolbar: [
                        'ExcelExport', 'Search',
                        { type: 'Separator' },
                        { text: 'Adicionar', prefixIcon: 'e-add', id: 'AddCustom' },
                        { text: 'Editar', prefixIcon: 'e-edit', id: 'EditCustom' },
                        { text: 'Excluir', prefixIcon: 'e-delete', id: 'DeleteCustom' }
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
                                mainTitle: 'Adicionar Caixa de Envio',
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
                                mainTitle: 'Editar Caixa de Envio',
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
                                mainTitle: 'Excluir Caixa de Envio',
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
            updateSummaryCards: () => {
                const total = state.mainData.length;
                const active = state.mainData.filter(x => x?.isActive === true).length;
                const inactive = total - active;

                state.summary = { total, active, inactive };
            },
            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = response?.data?.content?.data.map(item => ({
                    ...item,
                    createdAt: new Date(item.createdAt)
                }));

                methods.updateSummaryCards();
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
                        title: 'Sucesso',
                        timer: 1000,
                        showConfirmButton: false
                    });

                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Erro',
                        text: error.response?.data?.message ?? 'Erro inesperado'
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
