const App = {
    setup() {
        const BRASILIA_TIME_ZONE = 'America/Sao_Paulo';

        const emptyState = () => ({
            id: '',
            name: '',
            width: null,
            length: null,
            height: null,
            weight: null,
            insuranceValue: null,
            packageCategoryId: '',
            stockQuantity: 1,
            maxWeight: null,
            isActive: true
        });

        const state = Vue.reactive({
            mainData: [],
            packageCategories: [],
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
                field: 'createdAt',
                direction: 'desc'
            },
            deleteMode: false,
            mainTitle: 'Editar Caixa de Envio',
            errors: {
                width: '',
                length: '',
                height: '',
                weight: '',
                insuranceValue: ''
            },
            isSubmitting: false,
            ...emptyState()
        });

        const mainModalRef = Vue.ref(null);

        const services = {
            getMainData: async () => {
                return await AxiosManager.get('/ShippingBox/GetShippingBoxList', {});
            },
            getPackageCategories: async () => {
                return await AxiosManager.get('/PackageCategory/GetPackageCategoryList?activeOnly=true', {});
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

        const mainModal = {
            obj: null,
            create: () => {
                mainModal.obj = new bootstrap.Modal(mainModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        const methods = {
            formatNumber: (value) => {
                const number = Number(value ?? 0);

                return number.toLocaleString('pt-BR', {
                    minimumFractionDigits: 0,
                    maximumFractionDigits: 2
                });
            },
            formatCurrencyBRL: (value) =>
                Number(value || 0).toLocaleString('pt-BR', {
                    style: 'currency',
                    currency: 'BRL'
                }),
            formatDimensions: (box) => {
                const height = methods.formatNumber(box?.height);
                const width = methods.formatNumber(box?.width);
                const length = methods.formatNumber(box?.length);

                return `${height} x ${width} x ${length} cm`;
            },
            parseUtcDate: (rawDate) => {
                if (!rawDate) {
                    return null;
                }

                if (typeof rawDate === 'string') {
                    const hasTimeZone = /Z$/i.test(rawDate) || /[+-]\d{2}:\d{2}$/.test(rawDate);
                    const utcDateText = hasTimeZone ? rawDate : `${rawDate}Z`;
                    const parsedDate = new Date(utcDateText);

                    return Number.isNaN(parsedDate.getTime()) ? null : parsedDate;
                }

                const parsedDate = new Date(rawDate);

                return Number.isNaN(parsedDate.getTime()) ? null : parsedDate;
            },
            formatBrasiliaDateTime: (rawDate) => {
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
            getStatusLabel: (box) => box?.isInUse === true
                ? 'Em uso'
                : (box?.isActive === true ? 'Ativa' : 'Inativa'),
            getStatusClass: (box) => box?.isInUse === true
                ? 'shipping-status--in-use'
                : (box?.isActive === true
                    ? 'shipping-status--active'
                    : 'shipping-status--inactive'),
            getSortIcon: (field) => {
                if (state.sort.field !== field) return 'fa-sort';

                return state.sort.direction === 'asc'
                    ? 'fa-sort-up'
                    : 'fa-sort-down';
            },
            getSortValue: (box, field) => {
                if (field === 'dimensions') {
                    return Number(box?.width ?? 0) * Number(box?.length ?? 0) * Number(box?.height ?? 0);
                }

                if (field === 'weight') return Number(box?.weight ?? 0);
                if (field === 'insuranceValue') return Number(box?.insuranceValue ?? 0);
                if (field === 'packageCategoryName') return box?.packageCategoryName ?? '';
                if (field === 'isActive') {
                    if (box?.isInUse === true) return 2;
                    return box?.isActive === true ? 1 : 0;
                }

                if (field === 'createdAt') {
                    const date = methods.parseUtcDate(box?.createdAt);
                    return date && !Number.isNaN(date.getTime()) ? date.getTime() : 0;
                }

                return '';
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
            },
            resetForm: () => {
                Object.assign(state, emptyState());
                state.errors = {
                    width: '',
                    length: '',
                    height: '',
                    weight: '',
                    insuranceValue: ''
                };
            },
            setFormData: (box) => {
                Object.assign(state, {
                    id: box?.id ?? '',
                    name: box?.name ?? '',
                    width: box?.width ?? null,
                    length: box?.length ?? null,
                    height: box?.height ?? null,
                    weight: box?.weight ?? null,
                    insuranceValue: box?.insuranceValue ?? null,
                    packageCategoryId: box?.packageCategoryId ?? '',
                    stockQuantity: box?.stockQuantity ?? 0,
                    maxWeight: box?.maxWeight ?? null,
                    isActive: box?.isActive ?? true
                });
            },
            buildPayload: () => ({
                id: state.id,
                name: state.name,
                width: state.width,
                length: state.length,
                height: state.height,
                weight: state.weight,
                insuranceValue: state.insuranceValue,
                packageCategoryId: state.packageCategoryId,
                stockQuantity: state.stockQuantity,
                maxWeight: state.maxWeight,
                isActive: state.isActive
            }),
            validateForm: () => {
                let isValid = true;
                state.errors = {
                    width: '',
                    length: '',
                    height: '',
                    weight: '',
                    insuranceValue: ''
                };

                if (!state.name?.trim()) {
                    isValid = false;
                }
                if (!state.packageCategoryId) {
                    isValid = false;
                }
                if (!Number.isInteger(Number(state.stockQuantity)) || Number(state.stockQuantity) < 0) {
                    isValid = false;
                }

                ['width', 'length', 'height', 'weight', 'insuranceValue'].forEach((field) => {
                    const value = Number(state[field]);

                    if (!Number.isFinite(value) || value < 0) {
                        state.errors[field] = 'Informe um valor valido.';
                        isValid = false;
                    }
                });

                return isValid;
            }
        };

        const filteredShippingBoxes = Vue.computed(() => {
            const search = state.filters.search.trim().toLowerCase();
            const status = state.filters.status;

            const boxes = state.mainData.filter(box => {
                const haystack = [
                    box?.packageCategoryName,
                    methods.formatDimensions(box),
                    methods.formatNumber(box?.weight),
                    methods.formatCurrencyBRL(box?.insuranceValue),
                    methods.getStatusLabel(box)
                ].join(' ').toLowerCase();

                const matchesSearch = !search || haystack.includes(search);
                const matchesStatus = !status
                    || (status === 'in-use' && box?.isInUse === true)
                    || (status === 'active' && box?.isActive === true && box?.isInUse !== true)
                    || (status === 'inactive' && box?.isActive !== true && box?.isInUse !== true);

                return matchesSearch && matchesStatus;
            });

            const direction = state.sort.direction === 'desc' ? -1 : 1;

            return [...boxes].sort((first, second) => {
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
                state.mainTitle = 'Adicionar Caixa de Envio';
                methods.resetForm();

                mainModal.obj.show();
            },
            handleEdit: (box) => {
                state.deleteMode = false;
                state.mainTitle = 'Editar Caixa de Envio';
                methods.setFormData(box);

                mainModal.obj.show();
            },
            handleDelete: (box) => {
                state.deleteMode = true;
                state.mainTitle = 'Excluir Caixa de Envio';
                methods.setFormData(box);

                mainModal.obj.show();
            },
            handleSubmit: async () => {
                try {
                    state.isSubmitting = true;

                    if (state.deleteMode) {
                        await services.deleteMainData(state.id);
                    } else {
                        if (!methods.validateForm()) return;

                        const payload = methods.buildPayload();

                        if (state.id) {
                            await services.updateMainData(payload);
                        } else {
                            await services.createMainData(payload);
                        }
                    }

                    await methods.populateMainData();
                    mainModal.obj.hide();

                    Swal.fire({
                        icon: 'success',
                        title: state.deleteMode ? 'Excluido com Sucesso' : 'Salvo com Sucesso',
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
            const categoryResponse = await services.getPackageCategories();
            state.packageCategories = categoryResponse?.data?.content?.data ?? [];
            mainModal.create();

            mainModalRef.value.addEventListener('hidden.bs.modal', () => {
                methods.resetForm();
                state.deleteMode = false;
                state.mainTitle = 'Editar Caixa de Envio';
            });
        });

        return {
            state,
            mainModalRef,
            filteredShippingBoxes,
            methods,
            handler
        };
    }
};

Vue.createApp(App).mount('#app');
