const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            userId: '',
            firstName: '',
            lastName: '',
            companyName: '',
            oldPassword: '',
            newPassword: '',
            confirmNewPassword: '',
            mainTitle: 'Editar Meu Perfil',
            changePasswordTitle: 'Alterar Senha',
            changeAvatarTitle: 'Alterar Avatar',
            errors: {
                firstName: '',
                lastName: '',
                oldPassword: '',
                newPassword: '',
                confirmNewPassword: ''
            },
            isSubmitting: false
        });

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);
        const changePasswordModalRef = Vue.ref(null);
        const changeAvatarModalRef = Vue.ref(null);
        const firstNameRef = Vue.ref(null);
        const lastNameRef = Vue.ref(null);
        const companyNameRef = Vue.ref(null);
        const oldPasswordRef = Vue.ref(null);
        const newPasswordRef = Vue.ref(null);
        const confirmNewPasswordRef = Vue.ref(null);
        const imageUploadRef = Vue.ref(null);

        const services = {
            getMainData: async () => {
                try {
                    const response = await AxiosManager.get('/Security/GetMyProfileList?userId=' + StorageManager.getUserId(), {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateMainData: async (userId, firstName, lastName, companyName) => {
                try {
                    const response = await AxiosManager.post('/Security/UpdateMyProfile', {
                        userId, firstName, lastName, companyName
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updatePasswordData: async (userId, oldPassword, newPassword, confirmNewPassword) => {
                try {
                    const response = await AxiosManager.post('/Security/UpdateMyProfilePassword', {
                        userId, oldPassword, newPassword, confirmNewPassword
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            uploadImage: async (file) => {
                const formData = new FormData();
                formData.append('file', file);
                try {
                    const response = await AxiosManager.post('/FileImage/UploadImage', formData, {
                        headers: {
                            'Content-Type': 'multipart/form-data'
                        }
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateAvatarData: async (userId, avatar) => {
                try {
                    const response = await AxiosManager.post('/Security/UpdateMyProfileAvatar', {
                        userId, avatar
                    });
                    return response;
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
                    allowSelection: true,
                    allowGrouping: true,
                    allowTextWrap: true,
                    allowResizing: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    showColumnMenu: true,
                    gridLines: 'None',
                    filterSettings: { type: 'Menu' },
                    sortSettings: { columns: [{ field: 'firstName', direction: 'Descending' }] },
                    pageSettings: { currentPage: 1, pageSize: 50, pageSizes: ["10", "20", "50", "100", "200", "All"] },
                    selectionSettings: { persistSelection: true, type: 'Single' },
                    autoFit: true,
                    columns: [
                        { type: 'checkbox', width: 60 },
                        {
                            field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false
                        },
                        { field: 'firstName', headerText: 'Primeiro Nome', width: 200, minWidth: 200 },
                        { field: 'lastName', headerText: 'Sobrenome', width: 200, minWidth: 200 },
                        { field: 'companyName', headerText: 'Empresa', width: 400, minWidth: 400 },
                    ],
                    toolbar: [
                        'ExcelExport', 'Search',
                        { type: 'Separator' },
                        { text: 'Editar', tooltipText: 'Editar', prefixIcon: 'e-edit', id: 'EditCustom' },
                        { type: 'Separator' },
                        { text: 'Alterar Senha', tooltipText: 'Alterar Senha', id: 'ChangePasswordCustom' },
                        { text: 'Alterar Avatar', tooltipText: 'Alterar Avatar', id: 'ChangeAvatarCustom' },
                    ],
                    beforeDataBound: () => { },
                    dataBound: function () {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'ChangePasswordCustom', 'ChangeAvatarCustom'], false);
                        mainGrid.obj.autoFitColumns(['firstName', 'lastName', 'companyName']);
                    },
                    excelExportComplete: () => { },
                    rowSelected: () => {
                        if (mainGrid.obj.getSelectedRecords().length === 1) {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'ChangePasswordCustom', 'ChangeAvatarCustom'], true);
                        } else {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'ChangePasswordCustom', 'ChangeAvatarCustom'], false);
                        }
                    },
                    rowDeselected: () => {
                        if (mainGrid.obj.getSelectedRecords().length === 1) {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'ChangePasswordCustom', 'ChangeAvatarCustom'], true);
                        } else {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'ChangePasswordCustom', 'ChangeAvatarCustom'], false);
                        }
                    },
                    rowSelecting: () => {
                        if (mainGrid.obj.getSelectedRecords().length) {
                            mainGrid.obj.clearSelection();
                        }
                    },
                    toolbarClick: (args) => {
                        if (args.item.id === 'MainGrid_excelexport') {
                            mainGrid.obj.excelExport();
                        }

                        if (args.item.id === 'EditCustom') {
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.userId = selectedRecord.id ?? '';
                                state.firstName = selectedRecord.firstName ?? '';
                                state.lastName = selectedRecord.lastName ?? '';
                                state.companyName = selectedRecord.companyName ?? '';
                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'ChangePasswordCustom') {
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.userId = selectedRecord.id ?? '';
                                changePasswordModal.obj.show();
                            }
                        }

                        if (args.item.id === 'ChangeAvatarCustom') {
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.userId = selectedRecord.id ?? '';
                                changeAvatarModal.obj.show();
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

        const handler = {
            handleSubmit: async () => {
                state.isSubmitting = true;
                await new Promise(resolve => setTimeout(resolve, 200));

                state.errors.firstName = '';
                state.errors.lastName = '';
                let isValid = true;

                // Validasi firstName
                if (!state.firstName) {
                    state.errors.firstName = 'Primeiro nome e obrigatorio.';
                    isValid = false;
                }

                // Validasi lastName
                if (!state.lastName) {
                    state.errors.lastName = 'Sobrenome e obrigatorio.';
                    isValid = false;
                }

                if (!isValid) {
                    state.isSubmitting = false;
                    return;
                }

                try {
                    const response = await services.updateMainData(state.userId, state.firstName, state.lastName, state.companyName);
                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();
                        Swal.fire({
                            icon: 'success',
                            title: 'Salvo com Sucesso',
                            text: 'O formulario sera fechado...',
                            timer: 2000,
                            showConfirmButton: false
                        });
                        setTimeout(() => {
                            mainModal.obj.hide();
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
                    state.isSubmitting = false;
                }
            },
            handleChangePassword: async () => {
                state.isSubmitting = true;
                await new Promise(resolve => setTimeout(resolve, 200));

                state.errors.oldPassword = '';
                state.errors.newPassword = '';
                state.errors.confirmNewPassword = '';
                let isValid = true;

                // old password validation
                if (!state.oldPassword) {
                    state.errors.oldPassword = 'Senha antiga e obrigatoria.';
                    isValid = false;
                } else if (state.oldPassword.length < 6) {
                    state.errors.oldPassword = 'A senha antiga deve ter pelo menos 6 caracteres.';
                    isValid = false;
                }

                // new password validation
                if (!state.newPassword) {
                    state.errors.newPassword = 'Nova senha e obrigatoria.';
                    isValid = false;
                } else if (state.newPassword.length < 6) {
                    state.errors.newPassword = 'A nova senha deve ter pelo menos 6 caracteres.';
                    isValid = false;
                }

                // confirm new password validation
                if (!state.confirmNewPassword) {
                    state.errors.confirmNewPassword = 'Confirme a nova senha.';
                    isValid = false;
                } else if (state.confirmNewPassword.length < 6) {
                    state.errors.confirmNewPassword = 'A confirmacao deve ter pelo menos 6 caracteres.';
                    isValid = false;
                }

                if (!isValid) {
                    state.isSubmitting = false;
                    return;
                }

                try {
                    const response = await services.updatePasswordData(state.userId, state.oldPassword, state.newPassword, state.confirmNewPassword);
                    if (response.data.code === 200) {
                        Swal.fire({
                            icon: 'success',
                            title: 'Salvo com Sucesso',
                            text: 'O formulario sera fechado...',
                            timer: 2000,
                            showConfirmButton: false
                        });
                        setTimeout(() => {
                            changePasswordModal.obj.hide();
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
                    state.isSubmitting = false;
                }
            },
            handleFileUpload: async (file) => {
                try {
                    const response = await services.uploadImage(file);
                    if (response.status === 200) {
                        const imageName = response?.data?.content?.imageName;
                        await services.updateAvatarData(state.userId, imageName);
                        StorageManager.saveAvatar(imageName);

                        Swal.fire({
                            icon: "success",
                            title: "Upload realizado com Sucesso",
                            text: 'A pagina sera atualizada...',
                            timer: 1000,
                            showConfirmButton: false
                        });

                        setTimeout(() => {
                            changeAvatarModal.obj.hide();
                            location.reload();
                        }, 1000);
                    } else {
                        Swal.fire({
                            icon: "error",
                            title: "Falha no Upload",
                            text: response.message ?? "Ocorreu um erro durante o upload."
                        });
                    }
                } catch (error) {
                    Swal.fire({
                        icon: "error",
                        title: "Falha no Upload",
                        text: "Ocorreu um erro inesperado."
                    });
                }
            },
        };

        const methods = {
            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = response?.data?.content?.data;
            },
        };

        Vue.onMounted(async () => {
            Dropzone.autoDiscover = false;
            try {
                await SecurityManager.authorizePage(['Profiles']);
                await SecurityManager.validateToken();
                await methods.populateMainData();
                await mainGrid.create(state.mainData);

                mainModal.create();
                changePasswordModal.create();
                changeAvatarModal.create();

                initDropzone();

            } catch (e) {
                console.error('page init error:', e);
            } finally {
                
            }
        });

        let dropzoneInitialized = false;
        const initDropzone = () => {
            if (!dropzoneInitialized && imageUploadRef.value) {
                dropzoneInitialized = true;
                const dropzoneInstance = new Dropzone(imageUploadRef.value, {
                    url: "api/FileImage/UploadImage",
                    paramName: "file",
                    maxFilesize: 5,
                    acceptedFiles: "image/*",
                    addRemoveLinks: true,
                    dictDefaultMessage: "Arraste e solte uma imagem aqui para enviar",
                    autoProcessQueue: false,
                    init: function () {
                        this.on("addedfile", async function (file) {
                            await handler.handleFileUpload(file);
                        });
                    }
                });
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

        const changeAvatarModal = {
            obj: null,
            create: () => {
                changeAvatarModal.obj = new bootstrap.Modal(changeAvatarModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };


        return {
            state,
            mainGridRef,
            mainModalRef,
            changePasswordModalRef,
            changeAvatarModalRef,
            firstNameRef,
            lastNameRef,
            companyNameRef,
            oldPasswordRef,
            newPasswordRef,
            confirmNewPasswordRef,
            imageUploadRef,
            handler
        };
    }
};

Vue.createApp(App).mount('#app');
