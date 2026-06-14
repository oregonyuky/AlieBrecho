const App = {
    setup() {
        const getInitialState = () => ({
            mainData: [],
            summary: {
                total: 0,
                active: 0,
                inactive: 0
            },
            deleteMode: false,
            mainTitle: 'Editar Cliente',
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

        const translateCustomerStatus = (status) => ({
            Active: 'Ativo',
            Inactive: 'Inativo'
        }[status] ?? status);

        const renderCustomerStatusBadge = (status) => {
            if (status === 'Active') {
                return '<span class="badge d-inline-flex align-items-center gap-1" style="background:#dcfce7;color:#166534;border:1px solid #86efac;font-weight:600;"><i class="fa fa-circle-check"></i> Ativo</span>';
            }

            return '<span class="badge d-inline-flex align-items-center gap-1" style="background:#f1f5f9;color:#475569;border:1px solid #cbd5e1;font-weight:600;"><i class="fa fa-circle-minus"></i> Inativo</span>';
        };

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
                        { field: 'name', headerText: 'Nome', width: 180 },
                        { field: 'cpf', headerText: 'CPF', width: 140 },
                        { field: 'phoneNumber', headerText: 'Telefone', width: 150 },
                        { field: 'emailAddress', headerText: 'Email', width: 220 },
                        { field: 'postalCode', headerText: 'CEP', width: 160 },
                        { field: 'customerStatusDisplay', headerText: 'Status', width: 120 },
                        { field: 'createdAt', headerText: 'Criado Em', width: 180, format: 'dd/MM/yyyy HH:mm' }
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
                    rowDataBound: (args) => {
                        const statusValue = args.data?.customerStatus;
                        const statusText = translateCustomerStatus(statusValue);
                        const statusCell = Array.from(args.row.cells)
                            .find(cell => cell.textContent.trim() === statusText);

                        if (statusCell) {
                            statusCell.innerHTML = renderCustomerStatusBadge(statusValue);
                        }
                    },
                    toolbarClick: (args) => {
                        if (args.item.id?.toLowerCase().includes('excelexport')) {
                            mainGrid.obj.excelExport({ fileName: 'Customers.xlsx' });
                        }

                        if (args.item.id === 'AddCustom') {
                            resetForm();
                            state.deleteMode = false;
                            state.mainTitle = 'Adicionar Cliente';
                            mainModal.obj.show();
                        }

                        if (args.item.id === 'EditCustom') {
                            const selected = mainGrid.obj.getSelectedRecords()[0];
                            if (!selected) return;

                            state.deleteMode = false;
                            state.mainTitle = 'Editar Cliente';
                            fillStateFromRow(selected);
                            mainModal.obj.show();
                        }

                        if (args.item.id === 'DeleteCustom') {
                            const selected = mainGrid.obj.getSelectedRecords()[0];
                            if (!selected) return;

                            state.deleteMode = true;
                            state.mainTitle = 'Excluir Cliente';
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
            updateSummaryCards: () => {
                const total = state.mainData.length;
                const active = state.mainData.filter(x => x?.customerStatus === 'Active').length;
                const inactive = total - active;

                state.summary = { total, active, inactive };
            },
            populateMainData: async () => {
                const response = await services.getMainData();

                state.mainData = (response?.data?.content?.data ?? []).map((item) => ({
                    ...item,
                    customerStatusDisplay: translateCustomerStatus(item.customerStatus),
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
                    state.errors.name = '';

                    if (!state.deleteMode && !state.name) {
                        state.errors.name = 'Nome e obrigatorio.';
                        return;
                    }

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
