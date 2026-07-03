const App = {
    setup() {
        const emptyForm = () => ({
            id: '',
            titulo: '',
            subtitulo: '',
            dataLiberacao: '',
            ativo: true
        });

        const state = Vue.reactive({
            mainData: [],
            ...emptyForm(),
            isFormVisible: false,
            errors: {
                titulo: '',
                dataLiberacao: ''
            },
            isSubmitting: false
        });

        const toLocalInputValue = (value) => {
            if (!value) return '';
            return String(value).slice(0, 16);
        };

        const toBrasiliaApiValue = (value) => {
            if (!value) return null;
            return /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/.test(value)
                ? `${value}:00`
                : value;
        };

        const services = {
            getMainData: async () => AxiosManager.get('/drop-config', {}),
            createMainData: async () => AxiosManager.post('/drop-config', {
                titulo: state.titulo,
                subtitulo: state.subtitulo,
                dataLiberacaoBrasilia: toBrasiliaApiValue(state.dataLiberacao),
                ativo: state.ativo
            }),
            updateMainData: async () => AxiosManager.put(`/drop-config/${state.id}`, {
                titulo: state.titulo,
                subtitulo: state.subtitulo,
                dataLiberacaoBrasilia: toBrasiliaApiValue(state.dataLiberacao),
                ativo: state.ativo
            }),
            deleteMainData: async (id) => AxiosManager.delete(`/drop-config/${id}`, {})
        };

        const methods = {
            formatDateTime: (value) => {
                if (!value) return '-';
                const match = String(value).match(/^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})/);
                if (!match) return '-';
                const [, year, month, day, hour, minute] = match;
                return `${day}/${month}/${year} ${hour}:${minute}`;
            },
            clearErrors: () => {
                state.errors.titulo = '';
                state.errors.dataLiberacao = '';
            },
            resetForm: () => {
                Object.assign(state, emptyForm());
                methods.clearErrors();
                state.isFormVisible = false;
            },
            populateMainData: async () => {
                const response = await services.getMainData();
                const data = response?.data?.content?.data ?? [];

                state.mainData = data
                    .map(item => ({
                        ...item,
                        dataLiberacaoSort: item.dataLiberacaoBrasilia ?? ''
                    }))
                    .sort((a, b) => {
                        if (a.ativo !== b.ativo) {
                            return a.ativo ? -1 : 1;
                        }

                        return b.dataLiberacaoSort.localeCompare(a.dataLiberacaoSort);
                    });
            },
            validate: () => {
                let isValid = true;
                methods.clearErrors();

                if (!state.titulo?.trim()) {
                    state.errors.titulo = 'Titulo e obrigatorio.';
                    isValid = false;
                }

                if (!state.dataLiberacao || !toBrasiliaApiValue(state.dataLiberacao)) {
                    state.errors.dataLiberacao = 'Data e hora sao obrigatorias.';
                    isValid = false;
                }

                return isValid;
            }
        };

        Vue.watch(() => state.titulo, () => {
            state.errors.titulo = '';
        });

        Vue.watch(() => state.dataLiberacao, () => {
            state.errors.dataLiberacao = '';
        });

        const handler = {
            handleNew: () => {
                methods.resetForm();
                state.isFormVisible = true;
                Vue.nextTick(() => {
                    document.querySelector('.drop-panel')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
                });
            },
            handleSubmit: async () => {
                if (!methods.validate()) {
                    return;
                }

                try {
                    state.isSubmitting = true;

                    const response = state.id
                        ? await services.updateMainData()
                        : await services.createMainData();

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        methods.resetForm();
                        Swal.fire({
                            icon: 'success',
                            title: 'Drop salvo com sucesso',
                            timer: 1100,
                            showConfirmButton: false
                        });
                        return;
                    }

                    Swal.fire({
                        icon: 'error',
                        title: 'Falha ao salvar',
                        text: response.data.message ?? 'Erro'
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
            },
            handleEdit: (drop) => {
                state.id = drop.id ?? '';
                state.titulo = drop.titulo ?? '';
                state.subtitulo = drop.subtitulo ?? '';
                state.dataLiberacao = toLocalInputValue(drop.dataLiberacaoBrasilia);
                state.ativo = drop.ativo === true;
                state.isFormVisible = true;
                methods.clearErrors();
                Vue.nextTick(() => {
                    document.querySelector('.drop-panel')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
                });
            },
            handleDelete: async (drop) => {
                const result = await Swal.fire({
                    icon: 'warning',
                    title: 'Excluir drop?',
                    text: `Esta acao remove ${drop.titulo ?? 'este drop'}.`,
                    showCancelButton: true,
                    confirmButtonText: 'Excluir',
                    cancelButtonText: 'Cancelar'
                });

                if (!result.isConfirmed) {
                    return;
                }

                try {
                    const response = await services.deleteMainData(drop.id);

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        if (state.id === drop.id) {
                            methods.resetForm();
                        }
                        Swal.fire({
                            icon: 'success',
                            title: 'Drop excluido',
                            timer: 1000,
                            showConfirmButton: false
                        });
                        return;
                    }

                    Swal.fire({
                        icon: 'error',
                        title: 'Falha ao excluir',
                        text: response.data.message ?? 'Erro'
                    });
                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Erro',
                        text: error.response?.data?.message ?? 'Erro inesperado'
                    });
                }
            },
            handleCancel: () => {
                methods.resetForm();
            }
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['DropConfigs']);
                await SecurityManager.validateToken();
                await methods.populateMainData();
            } catch (e) {
                console.error(e);
            }
        });

        return {
            state,
            methods,
            handler
        };
    }
};

Vue.createApp(App).mount('#app');
