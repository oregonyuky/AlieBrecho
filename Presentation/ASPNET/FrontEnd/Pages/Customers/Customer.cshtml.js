const App = {
    setup() {
        const emptyCustomer = () => ({
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
            customerStatus: 'Active'
        });

        const emptyAddress = () => ({
            street: '',
            number: '',
            neighborhood: '',
            complement: '',
            city: '',
            stateRegion: '',
            postalCode: '',
            country: ''
        });

        const state = Vue.reactive({
            mainData: [],
            summary: {
                total: 0,
                active: 0,
                inactive: 0
            },
            filters: {
                search: '',
                status: ''
            },
            sort: {
                field: 'name',
                direction: 'asc'
            },
            address: emptyAddress(),
            deleteMode: false,
            mainTitle: 'Editar Cliente',
            errors: {
                name: ''
            },
            isSubmitting: false,
            ...emptyCustomer()
        });

        const mainModalRef = Vue.ref(null);
        const addressModalRef = Vue.ref(null);
        let customerNotificationsConnection = null;
        let customerRefreshTimeout = null;

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

        const methods = {
            resetForm: () => {
                Object.assign(state, emptyCustomer());
                state.errors = { name: '' };
            },
            setFormData: (customer) => {
                Object.assign(state, {
                    id: customer?.id ?? '',
                    name: customer?.name ?? '',
                    description: customer?.description ?? '',
                    cpf: customer?.cpf ?? '',
                    phoneNumber: customer?.phoneNumber ?? '',
                    emailAddress: customer?.emailAddress ?? '',
                    street: customer?.street ?? '',
                    number: customer?.number ?? '',
                    neighborhood: customer?.neighborhood ?? '',
                    complement: customer?.complement ?? '',
                    city: customer?.city ?? '',
                    stateRegion: customer?.state ?? customer?.stateRegion ?? '',
                    postalCode: customer?.postalCode ?? '',
                    country: customer?.country ?? '',
                    website: customer?.website ?? '',
                    instagram: customer?.instagram ?? '',
                    twitterX: customer?.twitterX ?? '',
                    tikTok: customer?.tikTok ?? '',
                    customerStatus: customer?.customerStatus ?? 'Active'
                });
            },
            setAddressData: (customer) => {
                state.address = {
                    street: customer?.street ?? '',
                    number: customer?.number ?? '',
                    neighborhood: customer?.neighborhood ?? '',
                    complement: customer?.complement ?? '',
                    city: customer?.city ?? '',
                    stateRegion: customer?.state ?? customer?.stateRegion ?? '',
                    postalCode: customer?.postalCode ?? '',
                    country: customer?.country ?? ''
                };
            },
            valueOrDash: (value) => value || '-',
            translateCustomerStatus: (status) => ({
                Active: 'Ativo',
                Inactive: 'Inativo'
            }[status] ?? status ?? '-'),
            getStatusClass: (customer) => customer?.customerStatus === 'Active'
                ? 'customer-status--active'
                : 'customer-status--inactive',
            getSortIcon: (field) => {
                if (state.sort.field !== field) return 'fa-sort';

                return state.sort.direction === 'asc'
                    ? 'fa-sort-up'
                    : 'fa-sort-down';
            },
            getSortValue: (customer, field) => {
                if (field === 'name') return String(customer?.name ?? '').toLowerCase();
                if (field === 'cpf') return String(customer?.cpf ?? '').toLowerCase();
                if (field === 'phoneNumber') return String(customer?.phoneNumber ?? '').toLowerCase();
                if (field === 'emailAddress') return String(customer?.emailAddress ?? '').toLowerCase();
                if (field === 'instagram') return String(customer?.instagram ?? '').toLowerCase();
                if (field === 'status') return methods.translateCustomerStatus(customer?.customerStatus).toLowerCase();

                return '';
            },
            updateSummaryCards: () => {
                const total = state.mainData.length;
                const active = state.mainData.filter(x => x?.customerStatus === 'Active').length;
                const inactive = total - active;

                state.summary = { total, active, inactive };
            },
            populateMainData: async () => {
                const response = await services.getMainData();

                state.mainData = response?.data?.content?.data ?? [];

                methods.updateSummaryCards();
            },
            connectCustomerNotifications: async () => {
                if (!window.signalR || customerNotificationsConnection) return;

                customerNotificationsConnection = new signalR.HubConnectionBuilder()
                    .withUrl('/hubs/customers', {
                        accessTokenFactory: () => StorageManager.getAccessToken() ?? ''
                    })
                    .withAutomaticReconnect()
                    .build();

                customerNotificationsConnection.on('CustomerChanged', () => {
                    window.clearTimeout(customerRefreshTimeout);
                    customerRefreshTimeout = window.setTimeout(() => {
                        methods.populateMainData().catch(error => {
                            console.error('Nao foi possivel atualizar os clientes em tempo real.', error);
                        });
                    }, 200);
                });

                try {
                    await customerNotificationsConnection.start();
                } catch (error) {
                    console.error('Nao foi possivel conectar as notificacoes de clientes.', error);
                    customerNotificationsConnection = null;
                }
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

        const addressModal = {
            obj: null,
            create: () => {
                addressModal.obj = new bootstrap.Modal(addressModalRef.value);
            }
        };

        Vue.watch(() => state.name, () => {
            state.errors.name = '';
        });

        const filteredCustomers = Vue.computed(() => {
            const search = state.filters.search.trim().toLowerCase();
            const status = state.filters.status;

            const customers = state.mainData.filter(customer => {
                const searchableText = [
                    customer?.name,
                    customer?.cpf,
                    customer?.phoneNumber,
                    customer?.emailAddress,
                    customer?.instagram,
                    customer?.postalCode
                ].map(value => String(value ?? '').toLowerCase()).join(' ');
                const matchesSearch = !search || searchableText.includes(search);
                const matchesStatus = !status || customer?.customerStatus === status;

                return matchesSearch && matchesStatus;
            });

            const direction = state.sort.direction === 'desc' ? -1 : 1;

            return [...customers].sort((first, second) => {
                const firstValue = methods.getSortValue(first, state.sort.field);
                const secondValue = methods.getSortValue(second, state.sort.field);

                return String(firstValue).localeCompare(String(secondValue), 'pt-BR', {
                    numeric: true,
                    sensitivity: 'base'
                }) * direction;
            });
        });

        const handler = {
            handleSort: (field) => {
                if (state.sort.field === field) {
                    state.sort.direction = state.sort.direction === 'asc' ? 'desc' : 'asc';
                    return;
                }

                state.sort.field = field;
                state.sort.direction = 'asc';
            },
            handleNew: () => {
                methods.resetForm();
                state.deleteMode = false;
                state.mainTitle = 'Adicionar Cliente';

                mainModal.obj.show();
            },
            handleEdit: (customer) => {
                state.deleteMode = false;
                state.mainTitle = 'Editar Cliente';
                methods.setFormData(customer);

                mainModal.obj.show();
            },
            handleDelete: (customer) => {
                state.deleteMode = true;
                state.mainTitle = 'Excluir Cliente';
                methods.setFormData(customer);

                mainModal.obj.show();
            },
            handleAddress: (customer) => {
                methods.setAddressData(customer);
                addressModal.obj.show();
            },
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

                    const payload = buildPayload();
                    const response = state.id
                        ? await services.updateMainData(payload)
                        : await services.createMainData(payload);

                    if (response.data.code === 200) {
                        await methods.populateMainData();

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
                await methods.connectCustomerNotifications();
                mainModal.create();
                addressModal.create();

                mainModalRef.value.addEventListener('hidden.bs.modal', () => {
                    methods.resetForm();
                    state.deleteMode = false;
                    state.mainTitle = 'Editar Cliente';
                });

                addressModalRef.value.addEventListener('hidden.bs.modal', () => {
                    state.address = emptyAddress();
                });
            } catch (e) {
                console.error(e);
            }
        });

        Vue.onBeforeUnmount(async () => {
            window.clearTimeout(customerRefreshTimeout);
            if (customerNotificationsConnection) {
                await customerNotificationsConnection.stop();
            }
        });

        return {
            state,
            mainModalRef,
            addressModalRef,
            filteredCustomers,
            methods,
            handler
        };
    }
};

Vue.createApp(App).mount('#app');
