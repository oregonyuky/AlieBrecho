const App = {
    setup() {
        const BRASILIA_TIME_ZONE = 'America/Sao_Paulo';

        const emptyState = () => ({
            id: '',
            name: '',
            description: '',
            isActive: true
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
            deleteMode: false,
            mainTitle: 'Editar Categoria',
            errors: {
                name: '',
                description: ''
            },
            isSubmitting: false,
            ...emptyState()
        });

        const mainModalRef = Vue.ref(null);
        const nameRef = Vue.ref(null);

        const services = {
            getMainData: async () => {
                try {
                    return await AxiosManager.get('/Category/GetCategoryList', {});
                } catch (error) {
                    throw error;
                }
            },
            createMainData: async (name, description, isActive) => {
                try {
                    return await AxiosManager.post('/Category/CreateCategory', {
                        name,
                        description,
                        isActive
                    });
                } catch (error) {
                    throw error;
                }
            },
            updateMainData: async (id, name, description, isActive) => {
                try {
                    return await AxiosManager.post('/Category/UpdateCategory', {
                        id,
                        name,
                        description,
                        isActive
                    });
                } catch (error) {
                    throw error;
                }
            },
            deleteMainData: async (id) => {
                try {
                    return await AxiosManager.post('/Category/DeleteCategory', {
                        id
                    });
                } catch (error) {
                    throw error;
                }
            }
        };

        const methods = {
            parseUtcDate: (rawDate) => {
                if (!rawDate) return null;

                if (typeof rawDate === 'string') {
                    const hasTimeZone = /Z$/i.test(rawDate) || /[+-]\d{2}:\d{2}$/.test(rawDate);
                    const utcDateText = hasTimeZone ? rawDate : `${rawDate}Z`;
                    const parsedDate = new Date(utcDateText);

                    return Number.isNaN(parsedDate.getTime()) ? null : parsedDate;
                }

                const parsedDate = new Date(rawDate);

                return Number.isNaN(parsedDate.getTime()) ? null : parsedDate;
            },
            resetForm: () => {
                Object.assign(state, emptyState());
                state.errors = {
                    name: '',
                    description: ''
                };
            },
            setFormData: (category) => {
                Object.assign(state, {
                    id: category?.id ?? '',
                    name: category?.name ?? '',
                    description: category?.description ?? '',
                    isActive: category?.isActive ?? true
                });
            },
            getStatusLabel: (category) => category?.isActive === true ? 'Ativa' : 'Inativa',
            getStatusClass: (category) => category?.isActive === true
                ? 'category-status--active'
                : 'category-status--inactive',
            getSortIcon: (field) => {
                if (state.sort.field !== field) return 'fa-sort';

                return state.sort.direction === 'asc'
                    ? 'fa-sort-up'
                    : 'fa-sort-down';
            },
            getSortValue: (category, field) => {
                if (field === 'name') return String(category?.name ?? '').toLowerCase();
                if (field === 'description') return String(category?.description ?? '').toLowerCase();
                if (field === 'status') return methods.getStatusLabel(category).toLowerCase();

                if (field === 'createdAt') {
                    const rawDate = category?.createdAt
                        ?? category?.createdAtUtc
                        ?? category?.insertedAt
                        ?? category?.createdDate
                        ?? category?.dateCreated
                        ?? category?.creationDate;
                    const date = methods.parseUtcDate(rawDate);

                    return date && !Number.isNaN(date.getTime()) ? date.getTime() : 0;
                }

                return '';
            },
            formatInsertedDate: (category) => {
                const rawDate = category?.createdAt
                    ?? category?.createdAtUtc
                    ?? category?.insertedAt
                    ?? category?.createdDate
                    ?? category?.dateCreated
                    ?? category?.creationDate;

                if (!rawDate) {
                    return {
                        date: '-',
                        time: ''
                    };
                }

                const date = methods.parseUtcDate(rawDate);
                if (!date) {
                    return {
                        date: '-',
                        time: ''
                    };
                }

                return {
                    date: date.toLocaleDateString('pt-BR', {
                        timeZone: BRASILIA_TIME_ZONE
                    }),
                    time: date.toLocaleTimeString('pt-BR', {
                        timeZone: BRASILIA_TIME_ZONE,
                        hour: '2-digit',
                        minute: '2-digit'
                    })
                };
            },
            updateSummaryCards: () => {
                const total = state.mainData.length;
                const active = state.mainData.filter(x => x?.isActive === true).length;
                const inactive = total - active;

                state.summary = { total, active, inactive };
            },
            populateMainData: async () => {
                const response = await services.getMainData();

                state.mainData = response?.data?.content?.data ?? [];

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

        const nameText = {
            obj: null,
            create: () => {
                nameText.obj = new ej.inputs.TextBox({
                    placeholder: 'Digite o Nome'
                });
                nameText.obj.appendTo(nameRef.value);
            },
            refresh: () => {
                if (nameText.obj) nameText.obj.value = state.name;
            }
        };

        Vue.watch(() => state.name, () => {
            state.errors.name = '';
            nameText.refresh();
        });

        const filteredCategories = Vue.computed(() => {
            const search = state.filters.search.trim().toLowerCase();
            const status = state.filters.status;

            const categories = state.mainData.filter(category => {
                const name = String(category?.name ?? '').toLowerCase();
                const description = String(category?.description ?? '').toLowerCase();
                const matchesSearch = !search
                    || name.includes(search)
                    || description.includes(search);
                const matchesStatus = !status
                    || (status === 'active' && category?.isActive === true)
                    || (status === 'inactive' && category?.isActive !== true);

                return matchesSearch && matchesStatus;
            });

            const direction = state.sort.direction === 'desc' ? -1 : 1;

            return [...categories].sort((first, second) => {
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
                state.deleteMode = false;
                state.mainTitle = 'Adicionar Categoria';
                methods.resetForm();

                mainModal.obj.show();
            },
            handleEdit: (category) => {
                state.deleteMode = false;
                state.mainTitle = 'Editar Categoria';
                methods.setFormData(category);

                mainModal.obj.show();
            },
            handleDelete: (category) => {
                state.deleteMode = true;
                state.mainTitle = 'Excluir Categoria';
                methods.setFormData(category);

                mainModal.obj.show();
            },
            handleSubmit: async () => {
                try {
                    state.isSubmitting = true;
                    await new Promise(r => setTimeout(r, 200));

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

                    let isValid = true;
                    state.errors.name = '';

                    if (!state.name) {
                        state.errors.name = 'Nome e obrigatorio.';
                        isValid = false;
                    }

                    if (!isValid) return;

                    const response = state.id
                        ? await services.updateMainData(
                            state.id,
                            state.name,
                            state.description,
                            state.isActive
                        )
                        : await services.createMainData(
                            state.name,
                            state.description,
                            state.isActive
                        );

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
                await SecurityManager.authorizePage(['Categories']);
                await SecurityManager.validateToken();

                await methods.populateMainData();

                nameText.create();
                mainModal.create();

                mainModalRef.value.addEventListener('hidden.bs.modal', () => {
                    methods.resetForm();
                    state.deleteMode = false;
                    state.mainTitle = 'Editar Categoria';
                });

            } catch (e) {
                console.error(e);
            }
        });

        return {
            state,
            mainModalRef,
            nameRef,
            filteredCategories,
            methods,
            handler
        };
    }
};

Vue.createApp(App).mount('#app');
