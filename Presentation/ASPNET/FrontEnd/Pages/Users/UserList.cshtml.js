const App = {
    setup() {
        const BRASILIA_TIME_ZONE = 'America/Sao_Paulo';

        const state = Vue.reactive({
            mainData: [],
            secondaryData: [],
            deleteMode: false,
            mainTitle: null,
            changePasswordTitle: null,
            changeRoleTitle: null,
            id: '',
            firstName: '',
            lastName: '',
            email: '',
            emailConfirmed: false,
            isBlocked: false,
            isDeleted: false,
            password: '',
            confirmPassword: '',
            newPassword: '',
            userId: '',
            errors: {
                firstName: '',
                lastName: '',
                email: '',
                password: '',
                confirmPassword: '',
                newPassword: '',
            },
            isSubmitting: false,
            isChangePasswordSubmitting: false,
        });

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);
        const changePasswordModalRef = Vue.ref(null);
        const changeRoleModalRef = Vue.ref(null);
        const secondaryGridRef = Vue.ref(null);
        const firstNameRef = Vue.ref(null);
        const lastNameRef = Vue.ref(null);
        const emailRef = Vue.ref(null);

        const firstNameText = {
            obj: null,
            create: () => {
                firstNameText.obj = new ej.inputs.TextBox({
                    placeholder: 'Digite o Primeiro Nome',
                });
                firstNameText.obj.appendTo(firstNameRef.value);
            },
            refresh: () => {
                if (firstNameText.obj) {
                    firstNameText.obj.value = state.firstName;
                }
            }
        };

        const lastNameText = {
            obj: null,
            create: () => {
                lastNameText.obj = new ej.inputs.TextBox({
                    placeholder: 'Digite o Sobrenome',
                });
                lastNameText.obj.appendTo(lastNameRef.value);
            },
            refresh: () => {
                if (lastNameText.obj) {
                    lastNameText.obj.value = state.lastName;
                }
            }
        };

        const emailText = {
            obj: null,
            create: () => {
                emailText.obj = new ej.inputs.TextBox({
                    placeholder: 'Digite o Email',
                });
                emailText.obj.appendTo(emailRef.value);
            },
            refresh: () => {
                if (emailText.obj) {
                    emailText.obj.value = state.email;
                }
            }
        };

        Vue.watch(
            () => state.firstName,
            (newVal, oldVal) => {
                state.errors.firstName = '';
                firstNameText.refresh();
            }
        );

        Vue.watch(
            () => state.lastName,
            (newVal, oldVal) => {
                state.errors.lastName = '';
                lastNameText.refresh();
            }
        );

        Vue.watch(
            () => state.email,
            (newVal, oldVal) => {
                state.errors.email = '';
                emailText.refresh();
            }
        );

        const validateForm = function () {
            state.errors.firstName = '';
            state.errors.lastName = '';
            state.errors.email = '';
            state.errors.password = '';
            state.errors.confirmPassword = '';

            let isValid = true;

            if (!state.firstName) {
                state.errors.firstName = 'Primeiro nome e obrigatorio.';
                isValid = false;
            }
            if (!state.lastName) {
                state.errors.lastName = 'Sobrenome e obrigatorio.';
                isValid = false;
            }
            if (!state.email) {
                state.errors.email = 'Email e obrigatorio.';
                isValid = false;
            } else if (!/\S+@\S+\.\S+/.test(state.email)) {
                state.errors.email = 'Digite um email valido.';
                isValid = false;
            }
            if (state.id === '') {
                if (!state.password) {
                    state.errors.password = 'Senha e obrigatoria.';
                    isValid = false;
                }
                if (!state.confirmPassword) {
                    state.errors.confirmPassword = 'Confirmacao de senha e obrigatoria.';
                    isValid = false;
                }
                if (state.password && state.confirmPassword && state.password !== state.confirmPassword) {
                    state.errors.confirmPassword = 'Senha e confirmacao devem ser iguais.';
                    isValid = false;
                }
            }

            return isValid;
        };

        const validateChangePasswordForm = function () {
            state.errors.newPassword = '';

            let isValid = true;

            if (!state.newPassword) {
                state.errors.newPassword = 'Nova senha e obrigatoria.';
                isValid = false;
            } else if (state.newPassword.length < 6) {
                state.errors.newPassword = 'A nova senha deve ter pelo menos 6 caracteres.';
                isValid = false;
            }

            return isValid;
        };

        const resetFormState = () => {
            state.id = '';
            state.firstName = '';
            state.lastName = '';
            state.email = '';
            state.emailConfirmed = false;
            state.isBlocked = false;
            state.isDeleted = false;
            state.password = '';
            state.confirmPassword = '';
            state.errors = {
                firstName: '',
                lastName: '',
                email: '',
                password: '',
                confirmPassword: '',
            };
        };

        const resetChangePasswordFormState = () => {
            state.newPassword = '';
            state.errors = {
                newPassword: '',
            };
        };

        const services = {
            getMainData: async () => {
                try {
                    const response = await AxiosManager.get('/Security/GetUserList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createMainData: async (firstName, lastName, email, emailConfirmed, isBlocked, isDeleted, password, confirmPassword, createdById) => {
                try {
                    const response = await AxiosManager.post('/Security/CreateUser', {
                        firstName, lastName, email, emailConfirmed, isBlocked, isDeleted, password, confirmPassword, createdById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateMainData: async (userId, firstName, lastName, emailConfirmed, isBlocked, isDeleted, updatedById) => {
                try {
                    const response = await AxiosManager.post('/Security/UpdateUser', {
                        userId, firstName, lastName, emailConfirmed, isBlocked, isDeleted, updatedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            deleteMainData: async (userId, deletedById) => {
                try {
                    const response = await AxiosManager.post('/Security/DeleteUser', {
                        userId, deletedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updatePasswordData: async (userId, newPassword) => {
                try {
                    const response = await AxiosManager.post('/Security/UpdatePasswordUser', {
                        userId, newPassword
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getRolesData: async () => {
                try {
                    const response = await AxiosManager.get('/Security/GetRoleList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getUserRolesData: async (userId) => {
                try {
                    if (!userId || userId.trim() === "") {
                        return null;
                    }
                    const response = await AxiosManager.post('/Security/GetUserRoles', { userId });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateUserRoleData: async (userId, roleName, accessGranted) => {
                try {
                    const response = await AxiosManager.post('/Security/UpdateUserRole', { userId, roleName, accessGranted });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
        };

        const parseUtcDate = (rawDate) => {
            if (!rawDate) return null;

            if (typeof rawDate === 'string') {
                const hasTimeZone = /Z$/i.test(rawDate) || /[+-]\d{2}:\d{2}$/.test(rawDate);
                const utcDateText = hasTimeZone ? rawDate : `${rawDate}Z`;
                const parsedDate = new Date(utcDateText);

                return Number.isNaN(parsedDate.getTime()) ? null : parsedDate;
            }

            const parsedDate = new Date(rawDate);

            return Number.isNaN(parsedDate.getTime()) ? null : parsedDate;
        };

        const formatBrasiliaDateTime = (rawDate) => {
            const date = parseUtcDate(rawDate);
            if (!date) return '-';

            return date.toLocaleString('pt-BR', {
                timeZone: BRASILIA_TIME_ZONE,
                day: '2-digit',
                month: '2-digit',
                year: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
            });
        };

        const methods = {
            populateMainData: async () => {
                try {
                    const response = await services.getMainData();
                    state.mainData = response?.data?.content?.data.map(item => ({
                        ...item,
                        createdAt: parseUtcDate(item.createdAt)
                    }));
                } catch (error) {
                    console.error("Error populating main data:", error);
                    state.mainData = [];
                }
            },
            populateSecondaryData: async (userId) => {
                try {
                    const rolesResponse = await services.getRolesData();
                    const roles = rolesResponse?.data?.content?.data ?? [];
                    const userRolesResponse = await services.getUserRolesData(userId);
                    const userRoles = userRolesResponse?.data?.content?.data ?? [];
                    const result = roles.length === 0
                        ? []
                        : roles.map(role => ({
                            roleName: role.name,
                            accessGranted: userRoles.includes(role.name)
                        }));

                    state.secondaryData = result;
                } catch (error) {
                    console.error("Error populating secondary data:", error);
                    state.secondaryData = [];
                }
            },
        };

        const handler = {
            handleSubmit: async function () {
                try {
                    state.isSubmitting = true;
                    await new Promise(resolve => setTimeout(resolve, 300));

                    if (!validateForm()) {
                        return;
                    }

                    const response = state.id === ''
                        ? await services.createMainData(state.firstName, state.lastName, state.email, state.emailConfirmed, state.isBlocked, state.isDeleted, state.password, state.confirmPassword, StorageManager.getUserId())
                        : state.deleteMode
                            ? await services.deleteMainData(state.id, StorageManager.getUserId())
                            : await services.updateMainData(state.id, state.firstName, state.lastName, state.emailConfirmed, state.isBlocked, state.isDeleted, StorageManager.getUserId());

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();

                        if (!state.deleteMode) {
                            state.mainTitle = state.id === '' ? 'Adicionar Usuario' : 'Editar Usuario';
                            state.id = response?.data?.content?.data.userId ?? '';
                            state.firstName = response?.data?.content?.data.firstName ?? '';
                            state.lastName = response?.data?.content?.data.lastName ?? '';
                            state.email = response?.data?.content?.data.email ?? '';
                            state.emailConfirmed = response?.data?.content?.data.emailConfirmed ?? false;
                            state.isBlocked = response?.data?.content?.data.isBlocked ?? false;
                            state.isDeleted = response?.data?.content?.data.isDeleted ?? false;

                            Swal.fire({
                                icon: 'success',
                                title: state.deleteMode ? 'Excluido com Sucesso' : 'Salvo com Sucesso',
                                text: 'O formulario sera fechado...',
                                timer: 2000,
                                showConfirmButton: false
                            });
                            setTimeout(() => {
                                mainModal.obj.hide();
                            }, 2000);

                        } else {
                            Swal.fire({
                                icon: 'success',
                                title: 'Excluido com Sucesso',
                                text: 'O formulario sera fechado...',
                                timer: 2000,
                                showConfirmButton: false
                            });
                            setTimeout(() => {
                                mainModal.obj.hide();
                                resetFormState();
                            }, 2000);
                        }

                    } else {
                        Swal.fire({
                            icon: 'error',
                            title: state.deleteMode ? 'Falha ao Excluir' : 'Falha ao Salvar',
                            text: response.data.message ?? 'Verifique seus dados.',
                            confirmButtonText: 'Tentar Novamente'
                        });
                    }

                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Ocorreu um Erro',
                        text: error.response?.data?.message ?? 'Tente novamente.',
                        confirmButtonText: 'OK'
                    });
                } finally {
                    state.isSubmitting = false;
                }
            },
            handleChangePassword: async function () {
                try {
                    state.isChangePasswordSubmitting = true;
                    await new Promise(resolve => setTimeout(resolve, 300));

                    if (!validateChangePasswordForm()) {
                        return;
                    }

                    const response = await services.updatePasswordData(state.userId, state.newPassword);

                    if (response.data.code === 200) {
                        Swal.fire({
                            icon: 'success',
                            title: 'Salvo com Sucesso',
                            text: 'Senha atualizada.',
                            timer: 2000,
                            showConfirmButton: false
                        });
                        setTimeout(() => {
                            changePasswordModal.obj.hide();
                            resetChangePasswordFormState();
                        }, 2000);
                    } else {
                        Swal.fire({
                            icon: 'error',
                            title: 'Falha ao Salvar',
                            text: response.data.message ?? 'Verifique seus dados.',
                            confirmButtonText: 'Tentar Novamente'
                        });
                    }

                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Ocorreu um Erro',
                        text: error.response?.data?.message ?? 'Tente novamente.',
                        confirmButtonText: 'OK'
                    });
                } finally {
                    state.isChangePasswordSubmitting = false;
                }
            }
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['Users']);
                await SecurityManager.validateToken();

                await methods.populateMainData();
                await mainGrid.create(state.mainData);
                await secondaryGrid.create(state.secondaryData);

                firstNameText.create();
                lastNameText.create();
                emailText.create();

                mainModal.create();
                changePasswordModal.create();
                changeRoleModal.create();

                mainModalRef.value?.addEventListener('hidden.bs.modal', () => {
                    resetFormState();
                });
                changePasswordModalRef.value?.addEventListener('hidden.bs.modal', () => {
                    resetChangePasswordFormState();
                });

            } catch (e) {
                console.error('page init error:', e);
            } finally {
                
            }
        });

        Vue.onUnmounted(() => {
            mainModalRef.value?.removeEventListener('hidden.bs.modal', resetFormState);
            changePasswordModalRef.value?.removeEventListener('hidden.bs.modal', resetChangePasswordFormState);
        });

        const mainGrid = {
            obj: null,
            create: async (dataSource) => {
                mainGrid.obj = new ej.grids.Grid({
                    height: '240px',
                    dataSource: dataSource,
                    allowFiltering: true,
                    allowSorting: true,
                    allowSelection: true,
                    allowGrouping: true,
                    allowTextWrap: true,
                    allowResizing: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    showColumnMenu: true,
                    gridLines: 'None',
                    filterSettings: { type: 'Menu' },
                    sortSettings: { columns: [{ field: 'createdAt', direction: 'Descending' }] },
                    pageSettings: { currentPage: 1, pageSize: 50, pageSizes: ["10", "20", "50", "100", "200", "All"] },
                    selectionSettings: { persistSelection: true, type: 'Single' },
                    autoFit: true,
                    columns: [
                        { type: 'checkbox', width: 60 },
                        {
                            field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false
                        },
                        { field: 'firstName', headerText: 'Primeiro Nome', width: 150, minWidth: 150 },
                        { field: 'lastName', headerText: 'Sobrenome', width: 150, minWidth: 150 },
                        { field: 'email', headerText: 'Email', width: 150, minWidth: 150 },
                        { field: 'emailConfirmed', headerText: 'Email Confirmado', textAlign: 'Center', width: 150, minWidth: 150, type: 'boolean', displayAsCheckBox: true },
                        { field: 'isBlocked', headerText: 'Bloqueado', textAlign: 'Center', width: 150, minWidth: 150, type: 'boolean', displayAsCheckBox: true },
                        { field: 'isDeleted', headerText: 'Excluido', textAlign: 'Center', width: 150, minWidth: 150, type: 'boolean', displayAsCheckBox: true },
                        { field: 'createdAt', headerText: 'Criado Em', width: 150, valueAccessor: (_, data) => formatBrasiliaDateTime(data?.createdAt) }
                    ],
                    toolbar: [
                        'ExcelExport', 'Search',
                        { type: 'Separator' },
                        { text: 'Adicionar', tooltipText: 'Adicionar', prefixIcon: 'e-add', id: 'AddCustom' },
                        { text: 'Editar', tooltipText: 'Editar', prefixIcon: 'e-edit', id: 'EditCustom' },
                        { text: 'Excluir', tooltipText: 'Excluir', prefixIcon: 'e-delete', id: 'DeleteCustom' },
                        { text: 'Alterar Senha', tooltipText: 'Alterar Senha', id: 'ChangePasswordCustom' },
                        { text: 'Alterar Perfil', tooltipText: 'Alterar Perfil', id: 'ChangeRoleCustom' },
                    ],
                    beforeDataBound: () => { },
                    dataBound: function () {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom', 'ChangePasswordCustom', 'ChangeRoleCustom'], false);
                        mainGrid.obj.autoFitColumns(['firstName', 'lastName', 'email', 'emailConfirmed', 'isBlocked', 'isDeleted', 'createdAt']);
                    },
                    excelExportComplete: () => { },
                    rowSelected: () => {
                        if (mainGrid.obj.getSelectedRecords().length == 1) {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom', 'ChangePasswordCustom', 'ChangeRoleCustom'], true);
                        } else {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom', 'ChangePasswordCustom', 'ChangeRoleCustom'], false);
                        }
                    },
                    rowDeselected: () => {
                        if (mainGrid.obj.getSelectedRecords().length == 1) {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom', 'ChangePasswordCustom', 'ChangeRoleCustom'], true);
                        } else {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom', 'ChangePasswordCustom', 'ChangeRoleCustom'], false);
                        }
                    },
                    rowSelecting: () => {
                        if (mainGrid.obj.getSelectedRecords().length) {
                            mainGrid.obj.clearSelection();
                        }
                    },
                    toolbarClick: async (args) => {
                        if (args.item.id === 'MainGrid_excelexport') {
                            mainGrid.obj.excelExport();
                        }

                        if (args.item.id === 'AddCustom') {
                            state.deleteMode = false;
                            state.mainTitle = 'Adicionar Usuario';
                            resetFormState();
                            mainModal.obj.show();
                        }

                        if (args.item.id === 'EditCustom') {
                            state.deleteMode = false;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Editar Usuario';
                                state.id = selectedRecord.id ?? '';
                                state.firstName = selectedRecord.firstName ?? '';
                                state.lastName = selectedRecord.lastName ?? '';
                                state.email = selectedRecord.email ?? '';
                                state.emailConfirmed = selectedRecord.emailConfirmed ?? false;
                                state.isBlocked = selectedRecord.isBlocked ?? false;
                                state.isDeleted = selectedRecord.isDeleted ?? false;
                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'DeleteCustom') {
                            state.deleteMode = true;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Excluir Usuario?';
                                state.id = selectedRecord.id ?? '';
                                state.firstName = selectedRecord.firstName ?? '';
                                state.lastName = selectedRecord.lastName ?? '';
                                state.email = selectedRecord.email ?? '';
                                state.emailConfirmed = selectedRecord.emailConfirmed ?? false;
                                state.isBlocked = selectedRecord.isBlocked ?? false;
                                state.isDeleted = selectedRecord.isDeleted ?? false;
                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'ChangePasswordCustom') {
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.changePasswordTitle = 'Alterar Senha';
                                state.userId = selectedRecord.id ?? '';
                                changePasswordModal.obj.show();
                            }
                        }

                        if (args.item.id === 'ChangeRoleCustom') {
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.changeRoleTitle = 'Alterar Perfis';
                                state.userId = selectedRecord.id ?? '';
                                await methods.populateSecondaryData(state.userId);
                                secondaryGrid.refresh();
                                changeRoleModal.obj.show();
                            }
                        }
                    }
                });

                mainGrid.obj.appendTo(mainGridRef.value);
            },
            refresh: () => {
                mainGrid.obj.setProperties({ dataSource: state.mainData });
            }
        };

        const secondaryGrid = {
            obj: null,
            create: async (dataSource) => {
                secondaryGrid.obj = new ej.grids.Grid({
                    height: 400,
                    dataSource: dataSource,
                    editSettings: { allowEditing: true, allowAdding: false, allowDeleting: false, showDeleteConfirmDialog: true, mode: 'Normal', allowEditOnDblClick: true },
                    allowFiltering: true,
                    allowSorting: true,
                    allowSelection: true,
                    allowGrouping: false,
                    allowTextWrap: true,
                    allowResizing: true,
                    allowPaging: false,
                    allowExcelExport: true,
                    showColumnMenu: true,
                    gridLines: 'None',
                    filterSettings: { type: 'Menu' },
                    sortSettings: { columns: [{ field: 'roleName', direction: 'Descending' }] },
                    pageSettings: { currentPage: 1, pageSize: 50, pageSizes: ["10", "20", "50", "100", "200", "All"] },
                    selectionSettings: { persistSelection: true, type: 'Single' },
                    autoFit: true,
                    columns: [
                        { type: 'checkbox', width: 60 },
                        {
                            field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false
                        },
                        { field: 'roleName', headerText: 'Perfil', allowEditing: false, width: 200, minWidth: 200 },
                        { field: 'accessGranted', headerText: 'Acesso Concedido', textAlign: 'Center', width: 150, minWidth: 150, editType: 'booleanedit', displayAsCheckBox: true, type: 'boolean', allowEditing: true },
                    ],
                    toolbar: [
                        'ExcelExport',
                        { type: 'Separator' },
                        'Edit', 'Update', 'Cancel',
                    ],
                    beforeDataBound: () => { },
                    dataBound: function () {
                        secondaryGrid.obj.autoFitColumns(['roleName', 'accessGranted']);
                    },
                    excelExportComplete: () => { },
                    rowSelected: () => {
                        if (secondaryGrid.obj.getSelectedRecords().length == 1) {
                            secondaryGrid.obj.toolbarModule.enableItems(['Edit'], true);
                        } else {
                            secondaryGrid.obj.toolbarModule.enableItems(['Edit'], false);
                        }
                    },
                    rowDeselected: () => {
                        if (secondaryGrid.obj.getSelectedRecords().length == 1) {
                            secondaryGrid.obj.toolbarModule.enableItems(['Edit'], true);
                        } else {
                            secondaryGrid.obj.toolbarModule.enableItems(['Edit'], false);
                        }
                    },
                    rowSelecting: () => {
                        if (secondaryGrid.obj.getSelectedRecords().length) {
                            secondaryGrid.obj.clearSelection();
                        }
                    },
                    toolbarClick: (args) => {
                        if (args.item.id === 'SecondaryGrid_excelexport') {
                            secondaryGrid.obj.excelExport();
                        }
                    },
                    actionComplete: async (args) => {
                        if (args.requestType === 'save' && args.action === 'edit') {
                            try {
                                const roleName = args?.data?.roleName;
                                const accessGranted = args?.data?.accessGranted;
                                const response = await services.updateUserRoleData(state.userId, roleName, accessGranted);

                                if (response.data.code === 200) {
                                    await methods.populateSecondaryData(state.userId);
                                    secondaryGrid.refresh();
                                    secondaryGrid.obj.clearSelection();
                                    Swal.fire({
                                        icon: 'success',
                                        title: 'Salvo com Sucesso',
                                        timer: 1000,
                                        showConfirmButton: false
                                    });
                                } else {
                                    Swal.fire({
                                        icon: 'error',
                                        title: 'Falha ao Salvar',
                                        text: response.data.message ?? 'Verifique seus dados.',
                                        confirmButtonText: 'Tentar Novamente'
                                    });
                                }
                            } catch (error) {
                                Swal.fire({
                                    icon: 'error',
                                    title: 'Ocorreu um Erro',
                                    text: error.response?.data?.message ?? 'Tente novamente.',
                                    confirmButtonText: 'OK'
                                });
                            }
                        }
                    }
                });
                secondaryGrid.obj.appendTo(secondaryGridRef.value);
            },
            refresh: () => {
                secondaryGrid.obj.setProperties({ dataSource: state.secondaryData });
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

        const changePasswordModal = {
            obj: null,
            create: () => {
                changePasswordModal.obj = new bootstrap.Modal(changePasswordModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        const changeRoleModal = {
            obj: null,
            create: () => {
                changeRoleModal.obj = new bootstrap.Modal(changeRoleModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        return {
            mainGridRef,
            mainModalRef,
            changePasswordModalRef,
            changeRoleModalRef,
            secondaryGridRef,
            firstNameRef,
            lastNameRef,
            emailRef,
            state,
            handler,
        };
    }
};

Vue.createApp(App).mount('#app');
