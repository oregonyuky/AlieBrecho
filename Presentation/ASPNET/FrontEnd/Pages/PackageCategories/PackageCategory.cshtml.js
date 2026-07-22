const App = {
    setup() {
        const modalRef = Vue.ref(null);
        const items = Vue.ref([]);
        const submitting = Vue.ref(false);
        const newForm = () => ({ id: '', name: '', capacityPoints: 1, description: '', isActive: true });
        const form = Vue.reactive(newForm());
        let modal;

        const load = async () => {
            const response = await AxiosManager.get('/PackageCategory/GetPackageCategoryList', {});
            items.value = response?.data?.content?.data ?? [];
        };
        const openNew = () => { Object.assign(form, newForm()); modal.show(); };
        const openEdit = item => { Object.assign(form, item); modal.show(); };
        const save = async () => {
            if (!form.name || !Number.isInteger(Number(form.capacityPoints)) || Number(form.capacityPoints) <= 0) {
                Swal.fire({ icon: 'warning', title: 'Dados inválidos', text: 'Informe o nome e uma capacidade inteira maior que zero.' });
                return;
            }
            submitting.value = true;
            try {
                const endpoint = form.id ? '/PackageCategory/UpdatePackageCategory' : '/PackageCategory/CreatePackageCategory';
                await AxiosManager.post(endpoint, { ...form });
                await load(); modal.hide();
                Swal.fire({ icon: 'success', title: 'Salvo com sucesso', timer: 1000, showConfirmButton: false });
            } catch (error) {
                Swal.fire({ icon: 'error', title: 'Erro', text: error.response?.data?.message ?? 'Não foi possível salvar.' });
            } finally { submitting.value = false; }
        };
        const deactivate = async item => {
            const answer = await Swal.fire({ icon: 'question', title: 'Desativar categoria?', showCancelButton: true, confirmButtonText: 'Desativar', cancelButtonText: 'Cancelar' });
            if (!answer.isConfirmed) return;
            await AxiosManager.post('/PackageCategory/DeactivatePackageCategory', { id: item.id });
            await load();
        };

        Vue.onMounted(async () => {
            await SecurityManager.authorizePage(['PackageCategories']);
            await SecurityManager.validateToken();
            modal = new bootstrap.Modal(modalRef.value, { backdrop: 'static' });
            await load();
        });
        return { modalRef, items, form, submitting, openNew, openEdit, save, deactivate };
    }
};
Vue.createApp(App).mount('#app');
