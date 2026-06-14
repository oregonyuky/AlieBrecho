const App = {
    setup() {
        const LOW_STOCK_THRESHOLD = 3;

        const emptyState = () => ({
            id: '',
            name: '',
            categoryID: '',
            unitPrice: null,
            oldPrice: null,
            unitWeight: null,
            discountPercent: null,
            productAvailable: true,
            mainImageURL: '',
            mainImageFile: null,
            mainImagePreviewURL: '',
            picture1: '',
            picture1File: null,
            picture1PreviewURL: '',
            picture2: '',
            picture2File: null,
            picture2PreviewURL: '',
            picture3: '',
            picture3File: null,
            picture3PreviewURL: '',
            picture4: '',
            picture4File: null,
            picture4PreviewURL: '',
            altText: '',
            addBadge: false,
            shortDescription: '',
            longDescription: '',
            note: '',
            sizes: []
        });

        const state = Vue.reactive({
            mainData: [],
            categories: [],
            deleteMode: false,
            mainTitle: 'Editar Produto',
            errors: {
                name: ''
            },
            summary: {
                totalProducts: 0,
                publishedProducts: 0,
                lowStockProducts: 0,
                outOfStockProducts: 0
            },
            isSubmitting: false,
            ...emptyState()
        });

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);
        const nameRef = Vue.ref(null);
        const mainImageFileRef = Vue.ref(null);
        const picture1FileRef = Vue.ref(null);
        const picture2FileRef = Vue.ref(null);
        const picture3FileRef = Vue.ref(null);
        const picture4FileRef = Vue.ref(null);

        const formatCurrencyBRL = (value) =>
            Number(value || 0).toLocaleString('pt-BR', {
                style: 'currency',
                currency: 'BRL'
            });

        const renderProductAvailabilityBadge = (isAvailable) => {
            if (isAvailable === true) {
                return '<span class="badge d-inline-flex align-items-center gap-1" title="Disponivel" style="background:#dcfce7;color:#166534;border:1px solid #86efac;font-weight:600;"><i class="fa fa-circle-check"></i> Disponivel</span>';
            }

            return '<span class="badge d-inline-flex align-items-center gap-1" title="Indisponivel" style="background:#f1f5f9;color:#475569;border:1px solid #cbd5e1;font-weight:600;"><i class="fa fa-circle-minus"></i> Indisponivel</span>';
        };

        const services = {
            getMainData: async () => {
                try {
                    return await AxiosManager.get('/Product/GetProductList', {});
                } catch (error) {
                    throw error;
                }
            },
            getCategoryData: async () => {
                try {
                    return await AxiosManager.get('/Category/GetCategoryList', {});
                } catch (error) {
                    throw error;
                }
            },
            getSingleData: async (id) => {
                try {
                    return await AxiosManager.get(`/Product/GetProductSingle?id=${encodeURIComponent(id)}`);
                } catch (error) {
                    throw error;
                }
            },
            createMainData: async (payload) => {
                try {
                    return await AxiosManager.post('/Product/CreateProduct', payload);
                } catch (error) {
                    throw error;
                }
            },
            updateMainData: async (payload) => {
                try {
                    return await AxiosManager.post('/Product/UpdateProduct', payload);
                } catch (error) {
                    throw error;
                }
            },
            deleteMainData: async (id) => {
                try {
                    return await AxiosManager.post('/Product/DeleteProduct', {
                        id
                    });
                } catch (error) {
                    throw error;
                }
            },
            uploadImage: async (file) => {
                const formData = new FormData();
                formData.append('file', file);

                try {
                    return await AxiosManager.post('/FileImage/UploadImage', formData, {
                        headers: {
                            'Content-Type': 'multipart/form-data'
                        }
                    });
                } catch (error) {
                    throw error;
                }
            },
        };

        const methods = {
            updateSummaryCards: () => {
                const normalizedStock = (item) => {
                    const possibleStock = Number(
                        item?.stockQuantity
                        ?? item?.quantity
                        ?? item?.stock
                        ?? item?.currentStock
                        ?? NaN
                    );

                    return Number.isFinite(possibleStock) ? possibleStock : null;
                };

                const totalProducts = state.mainData.length;
                const publishedProducts = state.mainData.filter(x => x?.productAvailable === true).length;
                const lowStockProducts = state.mainData.filter(item => {
                    const stock = normalizedStock(item);
                    return stock !== null && stock > 0 && stock <= LOW_STOCK_THRESHOLD;
                }).length;

                let outOfStockProducts = state.mainData.filter(item => {
                    const stock = normalizedStock(item);
                    return stock !== null && stock <= 0;
                }).length;

                if (outOfStockProducts === 0) {
                    outOfStockProducts = state.mainData.filter(x => x?.productAvailable === false).length;
                }

                state.summary = {
                    totalProducts,
                    publishedProducts,
                    lowStockProducts,
                    outOfStockProducts
                };
            },
            resetForm: () => {
                if (state.mainImagePreviewURL) {
                    URL.revokeObjectURL(state.mainImagePreviewURL);
                }
                if (state.picture1PreviewURL) {
                    URL.revokeObjectURL(state.picture1PreviewURL);
                }
                if (state.picture2PreviewURL) {
                    URL.revokeObjectURL(state.picture2PreviewURL);
                }
                if (state.picture3PreviewURL) {
                    URL.revokeObjectURL(state.picture3PreviewURL);
                }
                if (state.picture4PreviewURL) {
                    URL.revokeObjectURL(state.picture4PreviewURL);
                }

                Object.assign(state, emptyState());
                state.errors = { name: '' };

                if (mainImageFileRef.value) {
                    mainImageFileRef.value.value = '';
                }
                if (picture1FileRef.value) {
                    picture1FileRef.value.value = '';
                }
                if (picture2FileRef.value) {
                    picture2FileRef.value.value = '';
                }
                if (picture3FileRef.value) {
                    picture3FileRef.value.value = '';
                }
                if (picture4FileRef.value) {
                    picture4FileRef.value.value = '';
                }
            },
            setFormData: (data) => {
                if (state.mainImagePreviewURL) {
                    URL.revokeObjectURL(state.mainImagePreviewURL);
                }
                if (state.picture1PreviewURL) {
                    URL.revokeObjectURL(state.picture1PreviewURL);
                }
                if (state.picture2PreviewURL) {
                    URL.revokeObjectURL(state.picture2PreviewURL);
                }
                if (state.picture3PreviewURL) {
                    URL.revokeObjectURL(state.picture3PreviewURL);
                }
                if (state.picture4PreviewURL) {
                    URL.revokeObjectURL(state.picture4PreviewURL);
                }

                Object.assign(state, {
                    id: data?.id ?? '',
                    name: data?.name ?? '',
                    categoryID: data?.categoryID ?? '',
                    unitPrice: data?.unitPrice ?? null,
                    oldPrice: data?.oldPrice ?? null,
                    unitWeight: data?.unitWeight ?? null,
                    discountPercent: data?.discountPercent ?? null,
                    productAvailable: data?.productAvailable ?? true,
                    mainImageURL: data?.mainImageURL ?? '',
                    mainImageFile: null,
                    mainImagePreviewURL: '',
                    picture1: data?.picture1 ?? '',
                    picture1File: null,
                    picture1PreviewURL: '',
                    picture2: data?.picture2 ?? '',
                    picture2File: null,
                    picture2PreviewURL: '',
                    picture3: data?.picture3 ?? '',
                    picture3File: null,
                    picture3PreviewURL: '',
                    picture4: data?.picture4 ?? '',
                    picture4File: null,
                    picture4PreviewURL: '',
                    altText: data?.altText ?? '',
                    addBadge: data?.addBadge ?? false,
                    shortDescription: data?.shortDescription ?? '',
                    longDescription: data?.longDescription ?? '',
                    note: data?.note ?? '',
                    sizes: (data?.sizes ?? []).map(size => ({
                        size: size.size ?? '',
                        bust: size.bust ?? null,
                        sleeve: size.sleeve ?? null,
                        length: size.length ?? null
                    }))
                });

                if (mainImageFileRef.value) {
                    mainImageFileRef.value.value = '';
                }
                if (picture1FileRef.value) {
                    picture1FileRef.value.value = '';
                }
                if (picture2FileRef.value) {
                    picture2FileRef.value.value = '';
                }
                if (picture3FileRef.value) {
                    picture3FileRef.value.value = '';
                }
                if (picture4FileRef.value) {
                    picture4FileRef.value.value = '';
                }
            },
            buildPayload: () => ({
                id: state.id || null,
                name: state.name,
                categoryID: state.categoryID || null,
                unitPrice: state.unitPrice,
                oldPrice: state.oldPrice,
                unitWeight: state.unitWeight,
                discountPercent: state.discountPercent,
                productAvailable: state.productAvailable,
                mainImageURL: state.mainImageURL,
                altText: state.altText,
                addBadge: state.addBadge,
                shortDescription: state.shortDescription,
                longDescription: state.longDescription,
                picture1: state.picture1,
                picture2: state.picture2,
                picture3: state.picture3,
                picture4: state.picture4,
                note: state.note,
                sizes: state.sizes
                    .filter(size => size.size)
                    .map(size => ({
                        size: size.size,
                        bust: size.bust,
                        sleeve: size.sleeve,
                        length: size.length
                    }))
            }),
            populateMainData: async () => {
                const response = await services.getMainData();
                const data = response?.data?.content?.data ?? [];

                state.mainData = data.map(item => ({
                    ...item,
                    imageURL: item?.mainImageURL
                        ? '/api/FileImage/GetImage?imageName=' + item.mainImageURL
                        : '/noimage.png'
                }));

                methods.updateSummaryCards();
            },
            populateCategoryData: async () => {
                const response = await services.getCategoryData();

                state.categories = response?.data?.content?.data ?? [];
            },
            uploadImagesIfNeeded: async () => {
                const upload = async (file, targetField) => {
                    const response = await services.uploadImage(file);
                    const imageName = response?.data?.content?.imageName;

                    if (!imageName) {
                        throw new Error('Falha ao enviar imagem.');
                    }

                    state[targetField] = imageName;
                };

                if (state.mainImageFile) {
                    await upload(state.mainImageFile, 'mainImageURL');
                    state.mainImageFile = null;
                }

                if (state.picture1File) {
                    await upload(state.picture1File, 'picture1');
                    state.picture1File = null;
                }

                if (state.picture2File) {
                    await upload(state.picture2File, 'picture2');
                    state.picture2File = null;
                }

                if (state.picture3File) {
                    await upload(state.picture3File, 'picture3');
                    state.picture3File = null;
                }

                if (state.picture4File) {
                    await upload(state.picture4File, 'picture4');
                    state.picture4File = null;
                }
            }
        };

        const mainGrid = {
            obj: null,
            create: async (dataSource) => {
                mainGrid.obj = new ej.grids.Grid({
                    height: '240px',
                    dataSource: dataSource,
                    allowFiltering: true,
                    showColumnMenu: true,
                    gridLines: 'None',
                    allowSorting: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    allowResizing: true,
                    allowSelection: true,
                    filterSettings: { type: 'Menu' },
                    pageSettings: { pageSize: 50 },
                    selectionSettings: { type: 'Single' },
                    columns: [
                        { type: 'checkbox', width: 60 },
                        { field: 'id', isPrimaryKey: true, visible: false },
                        {
                            field: 'name',
                            headerText: 'Nome',
                            width: 260,
                            template: '<div class="product-name-cell"><img src="${imageURL}" alt="Produto" /><span>${name}</span></div>'
                        },
                        { field: 'unitPrice', headerText: 'Preco Unitario', width: 130, valueAccessor: (_, data) => formatCurrencyBRL(data.unitPrice) },
                        { field: 'oldPrice', headerText: 'Preco Antigo', width: 130, valueAccessor: (_, data) => formatCurrencyBRL(data.oldPrice) },
                        { field: 'discountPercent', headerText: 'Desconto %', width: 130, format: 'N2' },
                        {
                            field: 'productAvailable',
                            headerText: 'Disponivel',
                            width: 150,
                            textAlign: 'Center',
                            disableHtmlEncode: false,
                            valueAccessor: (_, data) => renderProductAvailabilityBadge(data?.productAvailable)
                        },
                        { field: 'shortDescription', headerText: 'Descricao Curta', width: 250 }
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
                    toolbarClick: async (args) => {
                        if (args.item.id?.toLowerCase().includes('excelexport')) {
                            mainGrid.obj.excelExport({ fileName: 'Products.xlsx' });
                        }

                        if (args.item.id === 'AddCustom') {
                            state.deleteMode = false;
                            state.mainTitle = 'Adicionar Produto';
                            methods.resetForm();

                            mainModal.obj.show();
                        }

                        if (args.item.id === 'EditCustom') {
                            const selected = mainGrid.obj.getSelectedRecords()[0];
                            if (!selected) return;

                            const response = await services.getSingleData(selected.id);
                            const product = response?.data?.content?.data;

                            state.deleteMode = false;
                            state.mainTitle = 'Editar Produto';
                            methods.setFormData(product);

                            mainModal.obj.show();
                        }

                        if (args.item.id === 'DeleteCustom') {
                            const selected = mainGrid.obj.getSelectedRecords()[0];
                            if (!selected) return;

                            const response = await services.getSingleData(selected.id);
                            const product = response?.data?.content?.data;

                            state.deleteMode = true;
                            state.mainTitle = 'Excluir Produto';
                            methods.setFormData(product);

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

        const handler = {
            handleMainImageChange: (event) => {
                const file = event.target.files?.[0] ?? null;

                if (state.mainImagePreviewURL) {
                    URL.revokeObjectURL(state.mainImagePreviewURL);
                }

                state.mainImageFile = file;
                state.mainImagePreviewURL = file ? URL.createObjectURL(file) : '';
            },
            handlePictureImageChange: (pictureKey, event) => {
                const file = event.target.files?.[0] ?? null;
                const previewKey = `${pictureKey}PreviewURL`;

                if (state[previewKey]) {
                    URL.revokeObjectURL(state[previewKey]);
                }

                state[`${pictureKey}File`] = file;
                state[previewKey] = file ? URL.createObjectURL(file) : '';
            },
            addSize: () => {
                state.sizes.push({
                    size: '',
                    bust: null,
                    sleeve: null,
                    length: null
                });
            },
            removeSize: (index) => {
                state.sizes.splice(index, 1);
            },
            handleSubmit: async () => {
                try {
                    state.isSubmitting = true;
                    await new Promise(r => setTimeout(r, 200));

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

                    await methods.uploadImagesIfNeeded();

                    const payload = methods.buildPayload();
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
                    const responseError = error.response?.data;
                    const errorText = responseError?.error?.message
                        ?? responseError?.message
                        ?? 'Erro inesperado';

                    Swal.fire({
                        icon: 'error',
                        title: 'Erro',
                        text: errorText
                    });
                } finally {
                    state.isSubmitting = false;
                }
            }
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['Products']);
                await SecurityManager.validateToken();

                await methods.populateCategoryData();
                await methods.populateMainData();
                await mainGrid.create(state.mainData);

                nameText.create();
                mainModal.create();

                mainModalRef.value.addEventListener('hidden.bs.modal', () => {
                    methods.resetForm();
                    state.deleteMode = false;
                    state.mainTitle = 'Editar Produto';
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
            mainImageFileRef,
            picture1FileRef,
            picture2FileRef,
            picture3FileRef,
            picture4FileRef,
            handler
        };
    }
};

Vue.createApp(App).mount('#app');
