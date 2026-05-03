const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            deleteMode: false,
            mainTitle: 'Edit Payment Type',
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
                    allowSorting: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    allowSelection: true,
                    filterSettings: { type: 'CheckBox' },
                    pageSettings: { pageSize: 50 },
                    selectionSettings: { type: 'Single' },
                    columns: [
                        { type: 'checkbox', width: 60 },
                        { field: 'id', isPrimaryKey: true, visible: false },
                        { field: 'typeName', headerText: 'Type Name', width: 200 },
                        { field: 'description', headerText: 'Description', width: 250 },
                        { field: 'isActive', headerText: 'Active', width: 120 },
                        { field: 'createdAt', headerText: 'Created At', width: 180, format: 'yyyy-MM-dd HH:mm' }
                    ],
                    toolbar: [
                        'ExcelExport', 'Search',
                        { type: 'Separator' },
                        { text: 'Add', tooltipText: 'Add', prefixIcon: 'e-add', id: 'AddCustom' },
                        { text: 'Edit', tooltipText: 'Edit', prefixIcon: 'e-edit', id: 'EditCustom' },
                        { text: 'Delete', tooltipText: 'Delete', prefixIcon: 'e-delete', id: 'DeleteCustom' }
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
                            state.mainTitle = 'Add Payment Type';
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
                            state.mainTitle = 'Edit Payment Type';
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
                            state.mainTitle = 'Delete Payment Type';
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
                    placeholder: 'Enter Type Name'
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
                                title: 'Delete Successful',
                                timer: 1000,
                                showConfirmButton: false
                            });

                            setTimeout(() => {
                                mainModal.obj.hide();
                            }, 1000);
                        } else {
                            Swal.fire({
                                icon: 'error',
                                title: 'Delete Failed',
                                text: deleteResponse.data.message ?? 'Error'
                            });
                        }

                        return;
                    }

                    let isValid = true;
                    state.errors.typeName = '';

                    if (!state.typeName) {
                        state.errors.typeName = 'Type Name is required.';
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
                            title: 'Save Successful',
                            timer: 1000,
                            showConfirmButton: false
                        });

                        setTimeout(() => {
                            mainModal.obj.hide();
                        }, 1000);
                    } else {
                        Swal.fire({
                            icon: 'error',
                            title: 'Save Failed',
                            text: response.data.message ?? 'Error'
                        });
                    }

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
                    state.mainTitle = 'Edit Payment Type';
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
