const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            deleteMode: false,
            mainTitle: 'Editar Forma de Pagamento',
            id: '',
            typeName: '',
            description: '',
            isActive: true,
            errors: {
                typeName: '',
                description: ''
            },
            isSubmitting: false
        });

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);
        const typeNameRef = Vue.ref(null);

        const services = {
            getMainData: async () => {
                try {
                    return await AxiosManager.get('/PaymentType/GetPaymentTypeList', {});
                } catch (error) {
                    throw error;
                }
            },
            createMainData: async (typeName, description, isActive) => {
                try {
                    return await AxiosManager.post('/PaymentType/CreatePaymentType', {
                        typeName,
                        description,
                        isActive
                    });
                } catch (error) {
                    throw error;
                }
            },
            updateMainData: async (id, typeName, description, isActive) => {
                try {
                    return await AxiosManager.post('/PaymentType/UpdatePaymentType', {
                        id,
                        typeName,
                        description,
                        isActive
                    });
                } catch (error) {
                    throw error;
                }
            },
            deleteMainData: async (id) => {
                try {
                    return await AxiosManager.post('/PaymentType/DeletePaymentType', {
                        id
                    });
                } catch (error) {
                    throw error;
                }
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
                    allowExcelExport: true,
                    allowSelection: true,
                    filterSettings: { type: 'Menu' },
                    pageSettings: { pageSize: 50 },
                    selectionSettings: { type: 'Single' },
                    columns: [
                        { type: 'checkbox', width: 60 },
                        { field: 'id', isPrimaryKey: true, visible: false },
                        { field: 'typeName', headerText: 'Nome do Tipo', width: 200 },
                        { field: 'description', headerText: 'Descricao', width: 250 },
                        { field: 'isActive', headerText: 'Ativo', width: 120 },
                        { field: 'createdAt', headerText: 'Criado Em', width: 180, format: 'yyyy-MM-dd HH:mm' }
                    ],
                    toolbar: [
                        'ExcelExport', 'Search',
                        { type: 'Separator' },
                        { text: 'Adicionar', tooltipText: 'Adicionar', prefixIcon: 'e-add', id: 'AddCustom' },
                        { text: 'Editar', tooltipText: 'Editar', prefixIcon: 'e-edit', id: 'EditCustom' },
                        { text: 'Excluir', tooltipText: 'Excluir', prefixIcon: 'e-delete', id: 'DeleteCustom' }
                    ],
                    dataBound: function () {
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
                            mainGrid.obj.excelExport({ fileName: 'PaymentTypes.xlsx' });
                        }

                        if (args.item.id === 'AddCustom') {
                            state.deleteMode = false;
                            state.mainTitle = 'Adicionar Forma de Pagamento';
                            state.id = '';
                            state.typeName = '';
                            state.description = '';
                            state.isActive = true;

                            mainModal.obj.show();
                        }

                        if (args.item.id === 'EditCustom') {
                            const selected = mainGrid.obj.getSelectedRecords()[0];
                            if (!selected) return;

                            state.deleteMode = false;
                            state.mainTitle = 'Editar Forma de Pagamento';
                            state.id = selected.id ?? '';
                            state.typeName = selected.typeName ?? '';
                            state.description = selected.description ?? '';
                            state.isActive = selected.isActive ?? true;

                            mainModal.obj.show();
                        }

                        if (args.item.id === 'DeleteCustom') {
                            const selected = mainGrid.obj.getSelectedRecords()[0];
                            if (!selected) return;

                            state.deleteMode = true;
                            state.mainTitle = 'Excluir Forma de Pagamento';
                            state.id = selected.id ?? '';
                            state.typeName = selected.typeName ?? '';
                            state.description = selected.description ?? '';
                            state.isActive = selected.isActive ?? true;

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

        const typeNameText = {
            obj: null,
            create: () => {
                typeNameText.obj = new ej.inputs.TextBox({
                    placeholder: 'Digite o Nome do Tipo'
                });
                typeNameText.obj.appendTo(typeNameRef.value);
            },
            refresh: () => {
                if (typeNameText.obj) typeNameText.obj.value = state.typeName;
            }
        };

        Vue.watch(() => state.typeName, () => {
            state.errors.typeName = '';
            typeNameText.refresh();
        });

        const handler = {
            handleSubmit: async () => {
                try {
                    state.isSubmitting = true;
                    await new Promise(r => setTimeout(r, 200));

                    if (state.deleteMode) {
                        const deleteResponse = await services.deleteMainData(state.id);

                        if (deleteResponse.data.code === 200) {
                            await methods.populateMainData();
                            mainGrid.refresh();

                            Swal.fire({
                                icon: 'success',
                                title: 'Excluido com Sucesso',
                                timer: 1000,
                                showConfirmButton: false
                            });

                            setTimeout(() => {
                                mainModal.obj.hide();
                            }, 1000);
                        } else {
                            Swal.fire({
                                icon: 'error',
                                title: 'Falha ao Excluir',
                                text: deleteResponse.data.message ?? 'Erro'
                            });
                        }

                        return;
                    }

                    let isValid = true;
                    state.errors.typeName = '';

                    if (!state.typeName) {
                        state.errors.typeName = 'Nome do tipo e obrigatorio.';
                        isValid = false;
                    }

                    if (!isValid) return;

                    const response = state.id
                        ? await services.updateMainData(
                            state.id,
                            state.typeName,
                            state.description,
                            state.isActive
                        )
                        : await services.createMainData(
                            state.typeName,
                            state.description,
                            state.isActive
                        );

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();

                        Swal.fire({
                            icon: 'success',
                            title: 'Salvo com Sucesso',
                            timer: 1000,
                            showConfirmButton: false
                        });

                        setTimeout(() => {
                            mainModal.obj.hide();
                        }, 1000);
                    } else {
                        Swal.fire({
                            icon: 'error',
                            title: 'Falha ao Salvar',
                            text: response.data.message ?? 'Erro'
                        });
                    }

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
            try {
                await SecurityManager.authorizePage(['PaymentTypes']);
                await SecurityManager.validateToken();

                await methods.populateMainData();
                await mainGrid.create(state.mainData);

                typeNameText.create();
                mainModal.create();

                mainModalRef.value.addEventListener('hidden.bs.modal', () => {
                    state.id = '';
                    state.typeName = '';
                    state.description = '';
                    state.isActive = true;
                    state.deleteMode = false;
                    state.mainTitle = 'Editar Forma de Pagamento';
                    state.errors = { typeName: '', description: '' };
                });

            } catch (e) {
                console.error(e);
            }
        });

        return {
            state,
            mainGridRef,
            mainModalRef,
            typeNameRef,
            handler
        };
    }
};

Vue.createApp(App).mount('#app');
