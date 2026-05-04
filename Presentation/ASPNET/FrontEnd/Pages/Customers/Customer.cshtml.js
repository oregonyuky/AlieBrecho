const App = {
    setup() {
        const getInitialState = () => ({
            mainData: [],
            deleteMode: false,
            mainTitle: 'Edit Customer',
            id: '',
            name: '',
            description: '',
            cpf: '',
            phoneNumber: '',
            emailAddress: '',
            street: '',
            number: '',
            neighborhood: '',
            complement: '',
            city: '',
            stateRegion: '',
            postalCode: '',
            country: '',
            website: '',
            instagram: '',
            twitterX: '',
            tikTok: '',
            customerStatus: 'Active',
            errors: {
                name: ''
            },
            isSubmitting: false
        });

        const state = Vue.reactive(getInitialState());

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);

        const services = {
            getMainData: async () => AxiosManager.get('/Customer/GetCustomerList', {}),
            createMainData: async (payload) => AxiosManager.post('/Customer/CreateCustomer', payload),
            updateMainData: async (payload) => AxiosManager.post('/Customer/UpdateCustomer', payload),
            deleteMainData: async (id) => AxiosManager.post('/Customer/DeleteCustomer', { id })
        };

        const buildPayload = () => ({
            id: state.id,
            name: state.name,
            description: state.description,
            cpf: state.cpf,
            phoneNumber: state.phoneNumber,
            emailAddress: state.emailAddress,
            street: state.street,
            number: state.number,
            neighborhood: state.neighborhood,
            complement: state.complement,
            city: state.city,
            state: state.stateRegion,
            postalCode: state.postalCode,
            country: state.country,
            website: state.website,
            instagram: state.instagram,
            twitterX: state.twitterX,
            tikTok: state.tikTok,
            customerStatus: state.customerStatus
        });

        const fillStateFromRow = (row) => {
            state.id = row.id ?? '';
            state.name = row.name ?? '';
            state.description = row.description ?? '';
            state.cpf = row.cpf ?? '';
            state.phoneNumber = row.phoneNumber ?? '';
            state.emailAddress = row.emailAddress ?? '';
            state.street = row.street ?? '';
            state.number = row.number ?? '';
            state.neighborhood = row.neighborhood ?? '';
            state.complement = row.complement ?? '';
            state.city = row.city ?? '';
            state.stateRegion = row.state ?? '';
            state.postalCode = row.postalCode ?? '';
            state.country = row.country ?? '';
            state.website = row.website ?? '';
            state.instagram = row.instagram ?? '';
            state.twitterX = row.twitterX ?? '';
            state.tikTok = row.tikTok ?? '';
            state.customerStatus = row.customerStatus ?? 'Active';
        };

        const resetForm = () => {
            const initial = getInitialState();
            Object.keys(initial).forEach((key) => {
                state[key] = initial[key];
            });
        };

        const mainGrid = {
            obj: null,
            create: async (dataSource) => {
                mainGrid.obj = new ej.grids.Grid({
                    height: '360px',
                    dataSource: dataSource,
                    allowFiltering: true,
                    showColumnMenu: true,
                    gridLines: 'None',
                    allowSorting: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    allowSelection: true,
                    allowResizing: true,
                    filterSettings: { type: 'Menu' },
                    pageSettings: { pageSize: 50 },
                    selectionSettings: { type: 'Single' },
                    columns: [
                        { type: 'checkbox', width: 60 },
                        { field: 'id', isPrimaryKey: true, visible: false },
                        { field: 'name', headerText: 'Name', width: 180 },
                        { field: 'cpf', headerText: 'CPF', width: 140 },
                        { field: 'phoneNumber', headerText: 'Phone', width: 150 },
                        { field: 'emailAddress', headerText: 'Email', width: 220 },
                        { field: 'postalCode', headerText: 'CEP', width: 160 },
                        { field: 'customerStatus', headerText: 'Status', width: 120 },
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
                            mainGrid.obj.excelExport({ fileName: 'Customers.xlsx' });
                        }

                        if (args.item.id === 'AddCustom') {
                            resetForm();
                            state.deleteMode = false;
                            state.mainTitle = 'Add Customer';
                            mainModal.obj.show();
                        }

                        if (args.item.id === 'EditCustom') {
                            const selected = mainGrid.obj.getSelectedRecords()[0];
                            if (!selected) return;

                            state.deleteMode = false;
                            state.mainTitle = 'Edit Customer';
                            fillStateFromRow(selected);
                            mainModal.obj.show();
                        }

                        if (args.item.id === 'DeleteCustom') {
                            const selected = mainGrid.obj.getSelectedRecords()[0];
                            if (!selected) return;

                            state.deleteMode = true;
                            state.mainTitle = 'Delete Customer';
                            fillStateFromRow(selected);
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

                state.mainData = (response?.data?.content?.data ?? []).map((item) => ({
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
                    state.errors.name = '';

                    if (!state.deleteMode && !state.name) {
                        state.errors.name = 'Name is required.';
                        return;
                    }

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
                        }

                        return;
                    }

                    const payload = buildPayload();
                    const response = state.id
                        ? await services.updateMainData(payload)
                        : await services.createMainData(payload);

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
                await SecurityManager.authorizePage(['Customers']);
                await SecurityManager.validateToken();

                await methods.populateMainData();
                await mainGrid.create(state.mainData);
                mainModal.create();

                mainModalRef.value.addEventListener('hidden.bs.modal', () => {
                    resetForm();
                });
            } catch (e) {
                console.error(e);
            }
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
