const App = {
    setup() {
        const state = Vue.reactive({
            messages: [],
            unreadCount: 0,
            selected: null,
            search: '',
            filter: 'all',
            isLoading: false
        });

        const services = {
            getMessages: async () => AxiosManager.get('/contact-messages', {}),
            markRead: async (id) => AxiosManager.put(`/contact-messages/${encodeURIComponent(id)}/read`, {})
        };

        const getDateParts = (value) => new Intl.DateTimeFormat('pt-BR', {
            timeZone: 'America/Sao_Paulo',
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit',
            hour12: false
        }).formatToParts(new Date(value)).reduce((parts, part) => {
            parts[part.type] = part.value;
            return parts;
        }, {});

        const methods = {
            load: async () => {
                state.isLoading = true;
                try {
                    const response = await services.getMessages();
                    const content = response?.data?.content ?? {};
                    state.messages = content.data ?? [];
                    state.unreadCount = content.unreadCount ?? state.messages.filter(item => !item.isRead).length;
                } finally {
                    state.isLoading = false;
                }
            },
            formatReceivedAt: (value) => {
                if (!value) return '-';
                const parts = getDateParts(value);
                const today = getDateParts(new Date());
                const dateKey = `${parts.year}-${parts.month}-${parts.day}`;
                const todayKey = `${today.year}-${today.month}-${today.day}`;

                if (dateKey === todayKey) {
                    return `Hoje ${parts.hour}:${parts.minute}`;
                }

                return `${parts.day}/${parts.month}/${parts.year} ${parts.hour}:${parts.minute}`;
            },
            formatFullDate: (value) => {
                if (!value) return '-';
                return new Intl.DateTimeFormat('pt-BR', {
                    timeZone: 'America/Sao_Paulo',
                    dateStyle: 'long',
                    timeStyle: 'short'
                }).format(new Date(value));
            }
        };

        const filteredMessages = Vue.computed(() => {
            const term = state.search.trim().toLocaleLowerCase('pt-BR');
            return state.messages.filter(message => {
                if (state.filter === 'unread' && message.isRead) return false;
                if (!term) return true;
                return [message.name, message.email, message.subject]
                    .some(value => String(value ?? '').toLocaleLowerCase('pt-BR').includes(term));
            });
        });

        const handler = {
            refresh: async () => {
                try {
                    await methods.load();
                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Erro ao carregar mensagens',
                        text: error.response?.data?.message ?? 'Tente novamente.'
                    });
                }
            },
            view: async (message) => {
                state.selected = message;
                if (message.isRead) return;

                try {
                    await services.markRead(message.id);
                    message.isRead = true;
                    message.readAtUtc = new Date().toISOString();
                    state.unreadCount = Math.max(0, state.unreadCount - 1);
                } catch (error) {
                    console.error('Não foi possível marcar a mensagem como lida.', error);
                }
            },
            close: () => {
                state.selected = null;
            }
        };

        Vue.onMounted(async () => {
            await SecurityManager.authorizePage(['Messages']);
            const isTokenValid = await SecurityManager.validateToken();
            if (isTokenValid) await handler.refresh();
        });

        return { state, methods, handler, filteredMessages };
    }
};

Vue.createApp(App).mount('#app');
