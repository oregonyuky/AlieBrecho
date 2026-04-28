const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            deleteMode: false,
            mainTitle: 'Edit Category',
            id: '',
            name: '',
            description: '',
            isActive: true,
            errors: {
                name: ''
            },
            isSubmitting: false
        });

        const mainGridRef = Vue.ref(null);
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
                    allowSelection: true,
                    filterSettings: { type: 'CheckBox' },
                    pageSettings: { pageSize: 50 },
                    selectionSettings: { type: 'Single' },
                    columns: [
                        { type: 'checkbox', width: 60 },
                        { field: 'id', isPrimaryKey: true, visible: false },
                        { field: 'name', headerText: 'Name', width: 200 },
                        { field: 'description', headerText: 'Description', width: 250 },
                        { field: 'isActive', headerText: 'Active', width: 120 },
                        { field: 'createdAt', headerText: 'Created At', width: 180, format: 'yyyy-MM-dd HH:mm' }
                    ],
                    toolbar: [
                        'Search',
                        { type: 'Separator' },
                        { text: 'Edit', prefixIcon: 'e-edit', id: 'EditCustom' }
                    ],
                    dataBound: function () {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom'], false);
                    },
                    rowSelected: () => {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom'], true);
                    },
                    rowDeselected: () => {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom'], false);
                    },
                    toolbarClick: (args) => {
                        if (args.item.id === 'EditCustom') {
                            const selected = mainGrid.obj.getSelectedRecords()[0];
                            if (!selected) return;

                            state.id = selected.id ?? '';
                            state.name = selected.name ?? '';
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

        const nameText = {
            obj: null,
            create: () => {
                nameText.obj = new ej.inputs.TextBox({
                    placeholder: 'Enter Name'
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

        const handler = {
            handleSubmit: async () => {
                try {
                    state.isSubmitting = true;
                    await new Promise(r => setTimeout(r, 200));

                    // validation
                    let isValid = true;
                    state.errors.name = '';

                    if (!state.name) {
                        state.errors.name = 'Name is required.';
                        isValid = false;
                    }

                    if (!isValid) return;

                    const response = await services.updateMainData(
                        state.id,
                        state.name,
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
                            location.reload();
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
                await SecurityManager.authorizePage(['Categories']);
                await SecurityManager.validateToken();

                await methods.populateMainData();
                await mainGrid.create(state.mainData);

                nameText.create();
                mainModal.create();

                mainModalRef.value.addEventListener('hidden.bs.modal', () => {
                    state.id = '';
                    state.name = '';
                    state.description = '';
                    state.isActive = true;
                    state.errors = { name: '' };
                });

            } catch (e) {
                console.error(e);
            }
        });

        return {
            state,
            mainGridRef,
            mainModalRef,
            nameRef,
            handler
        };
    }
};

Vue.createApp(App).mount('#app');