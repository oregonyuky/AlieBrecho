const App = {
    setup() {
        const emptyPayment = () => ({
            id: '',
            name: '',
            description: '',
            status: 'Pending',
            paymentDateTime: '',
            amount: 0,
            paymentTypeId: '',
            paymentMethod: '',
            transactionId: '',
            authorizationCode: '',
            referenceNumber: '',
            cardHolderName: '',
            cardLast4: ''
        });

        const emptyShipping = () => ({
            firstName: '',
            lastName: '',
            email: '',
            phoneNumber: '',
            street: '',
            number: '',
            neighborhood: '',
            complement: '',
            city: '',
            state: '',
            postCode: ''
        });

        const emptyState = () => ({
            mainData: [],
            deleteMode: false,
            mainTitle: 'Editar Pedido',
            id: '',
            customerId: '',
            status: 'Pending',
            orderDate: new Date().toISOString(),
            discount: 0,
            taxes: 0,
            shippingCost: 0,
            totalAmount: 0,
            notes: '',
            shippingBoxId: '',
            payment: emptyPayment(),
            shipping: emptyShipping(),
            orderDetails: [],
            customers: [],
            products: [],
            shippingBoxes: [],
            paymentTypes: [],
            paymentStatuses: ['Pending', 'Paid', 'Cancelled'],
            orderStatuses: ['Pending', 'Paid', 'Dispatched', 'Shipped', 'Delivered', 'Cancelled'],
            errors: {
                customerId: '',
                orderDetails: ''
            },
            isSubmitting: false
        });

        const state = Vue.reactive(emptyState());

        const statusLabels = {
            Pending: 'Pendente',
            Paid: 'Pago',
            Dispatched: 'Despachado',
            Shipped: 'Enviado',
            Delivered: 'Entregue',
            Cancelled: 'Cancelado'
        };

        const translateStatus = (status) => statusLabels[status] ?? status;

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);

        const services = {
            getMainData: async () => AxiosManager.get('/Order/GetOrderList', {}),
            getSingleData: async (id) => AxiosManager.get('/Order/GetOrderSingle', { params: { id } }),
            createMainData: async (request) => AxiosManager.post('/Order/CreateOrder', request),
            updateMainData: async (request) => AxiosManager.post('/Order/UpdateOrder', request),
            deleteMainData: async (id) => AxiosManager.post('/Order/DeleteOrder', { id }),
            getCustomers: async () => AxiosManager.get('/Customer/GetCustomerList', {}),
            getProducts: async () => AxiosManager.get('/Product/GetProductList', {}),
            getShippingBoxes: async () => AxiosManager.get('/ShippingBox/GetShippingBoxList', {}),
            getPaymentTypes: async () => AxiosManager.get('/PaymentType/GetPaymentTypeList', {}),
            calculateShippingCost: async (shippingBoxId, destinationPostCode) => AxiosManager.get('/Order/CalculateShippingCost', {
                params: { shippingBoxId, destinationPostCode }
            })
        };

        const calculateItemsTotal = () => state.orderDetails.reduce((total, item) => {
            const quantity = Number(item.quantity || 0);
            const unitPrice = Number(item.unitPrice || 0);
            return total + (quantity * unitPrice);
        }, 0);

        const getSelectedShippingBox = () =>
            state.shippingBoxes.find(x => x.id === state.shippingBoxId) ?? null;

        const calculateCubicWeight = (shippingBox) => {
            if (!shippingBox) return 0;

            return (
                Number(shippingBox.width || 0) *
                Number(shippingBox.length || 0) *
                Number(shippingBox.height || 0)
            ) / 6000;
        };

        const calculateChargedWeight = (shippingBox) => {
            if (!shippingBox) return 0;

            return Math.max(
                Number(shippingBox.weight || 0),
                calculateCubicWeight(shippingBox)
            );
        };

        const calculateShippingCost = () => {
            const shippingBox = getSelectedShippingBox();
            if (!shippingBox) return 0;

            const chargedWeight = calculateChargedWeight(shippingBox);
            const insurance = Number(shippingBox.insuranceValue || 0) * 0.01;

            return Number(((chargedWeight * 10) + insurance).toFixed(2));
        };

        const getItemTotal = (item) =>
            Number(item.quantity || 0) * Number(item.unitPrice || 0);

        const getItemShippingCost = (item) => {
            const itemsTotal = calculateItemsTotal();
            if (!itemsTotal || !state.shippingCost) return 0;

            return Number(((getItemTotal(item) / itemsTotal) * state.shippingCost).toFixed(2));
        };

        const formatCurrency = (value) =>
            Number(value || 0).toLocaleString(undefined, {
                style: 'currency',
                currency: 'BRL'
            });

        const formatNumber = (value, decimals = 2) =>
            Number(value || 0).toFixed(decimals);

        const formatCompactNumber = (value) =>
            parseFloat(Number(value || 0).toFixed(2)).toString();

        const formatShippingBoxData = (shippingBox) => {
            if (!shippingBox) return '';

            return `${formatCompactNumber(shippingBox.width)}cm x ${formatCompactNumber(shippingBox.length)}cm x ${formatCompactNumber(shippingBox.height)}cm ${formatCompactNumber(shippingBox.weight)}kg`;
        };

        const recalculateTotal = (shippingCost = state.shippingCost) => {
            if (!shippingCost && state.shippingBoxId) {
                shippingCost = calculateShippingCost();
            }

            state.shippingCost = shippingCost;
            state.totalAmount = calculateItemsTotal()
                - Number(state.discount || 0)
                + Number(state.taxes || 0)
                + Number(state.shippingCost || 0);
            state.payment.amount = state.totalAmount;
        };

        const resetForm = () => {
            const initial = emptyState();
            Object.keys(initial).forEach((key) => {
                if (['mainData', 'customers', 'products', 'shippingBoxes', 'paymentTypes'].includes(key)) {
                    return;
                }
                state[key] = initial[key];
            });
        };

        const fillOrder = (order) => {
            state.id = order.id ?? '';
            state.customerId = order.customerId ?? '';
            state.status = order.status ?? 'Pending';
            state.orderDate = order.orderDate ?? new Date().toISOString();
            state.discount = order.discount ?? 0;
            state.taxes = order.taxes ?? 0;
            state.shippingCost = order.shippingCost ?? 0;
            state.totalAmount = order.totalAmount ?? 0;
            state.notes = order.notes ?? '';
            state.shippingBoxId = order.shippingBoxId ?? '';

            state.payment.id = order.payment?.id ?? '';
            state.payment.name = order.payment?.name ?? '';
            state.payment.description = order.payment?.description ?? '';
            state.payment.status = order.payment?.status ?? 'Pending';
            state.payment.paymentDateTime = order.payment?.paymentDateTime ? formatDateTimeValue(order.payment.paymentDateTime) : '';
                state.payment.amount = order.payment?.amount ?? state.totalAmount;
            state.payment.paymentTypeId = order.payment?.paymentTypeId ?? '';
            state.payment.paymentMethod = order.payment?.paymentDetail?.paymentMethod ?? '';
            state.payment.transactionId = order.payment?.paymentDetail?.transactionId ?? '';
            state.payment.authorizationCode = order.payment?.paymentDetail?.authorizationCode ?? '';
            state.payment.referenceNumber = order.payment?.paymentDetail?.referenceNumber ?? '';
            state.payment.cardHolderName = order.payment?.paymentDetail?.cardHolderName ?? '';
            state.payment.cardLast4 = order.payment?.paymentDetail?.cardLast4 ?? '';

            state.shipping.firstName = order.shippingDetail?.firstName ?? '';
            state.shipping.lastName = order.shippingDetail?.lastName ?? '';
            state.shipping.email = order.shippingDetail?.email ?? '';
            state.shipping.phoneNumber = order.shippingDetail?.phoneNumber ?? '';
            state.shipping.street = order.shippingDetail?.street ?? '';
            state.shipping.number = order.shippingDetail?.number ?? '';
            state.shipping.neighborhood = order.shippingDetail?.neighborhood ?? '';
            state.shipping.complement = order.shippingDetail?.complement ?? '';
            state.shipping.city = order.shippingDetail?.city ?? '';
            state.shipping.state = order.shippingDetail?.state ?? '';
            state.shipping.postCode = order.shippingDetail?.postCode ?? '';

            state.orderDetails = (order.orderDetails ?? []).map((item) => ({
                id: item.id ?? '',
                productId: item.productId ?? '',
                quantity: item.quantity ?? 1,
                unitPrice: item.unitPrice ?? 0
            }));

            recalculateTotal(state.shippingCost);
        };

        const buildPayload = () => ({
            id: state.id,
            customerId: state.customerId,
            status: state.status,
            discount: state.discount,
            taxes: state.taxes,
            shippingCost: state.shippingCost,
            totalAmount: state.totalAmount,
            notes: state.notes,
            shippingBoxId: state.shippingBoxId || null,
            payment: {
                id: state.payment.id,
                name: state.payment.name,
                description: state.payment.description,
                status: state.payment.status,
                paymentDateTime: state.payment.paymentDateTime ? new Date(state.payment.paymentDateTime) : null,
                amount: state.payment.amount,
                paymentTypeId: state.payment.paymentTypeId || null,
                paymentMethod: state.payment.paymentMethod,
                transactionId: state.payment.transactionId,
                authorizationCode: state.payment.authorizationCode,
                referenceNumber: state.payment.referenceNumber,
                cardHolderName: state.payment.cardHolderName,
                cardLast4: state.payment.cardLast4
            },
            shippingDetail: {
                firstName: state.shipping.firstName,
                lastName: state.shipping.lastName,
                email: state.shipping.email,
                phoneNumber: state.shipping.phoneNumber,
                street: state.shipping.street,
                number: state.shipping.number,
                neighborhood: state.shipping.neighborhood,
                complement: state.shipping.complement,
                city: state.shipping.city,
                state: state.shipping.state,
                postCode: state.shipping.postCode
            },
            orderDetails: state.orderDetails.map((item) => ({
                id: item.id,
                productId: item.productId,
                quantity: Number(item.quantity || 1),
                unitPrice: Number(item.unitPrice || 0)
            }))
        });

        const mainGrid = {
            obj: null,
            create: async (dataSource) => {
                mainGrid.obj = new ej.grids.Grid({
                    height: '360px',
                    dataSource: dataSource,
                    allowFiltering: true,
                    showColumnMenu: true,
                    gridLines: 'None',
                    filterSettings: { type: 'Menu' },
                    allowSorting: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    allowSelection: true,
                    allowResizing: true,
                    pageSettings: { pageSize: 30 },
                    selectionSettings: { type: 'Single', mode: 'Row' },
                    columns: [
                        { type: 'checkbox', width: 60 },
                        { field: 'id', isPrimaryKey: true, visible: false },
                        { field: 'customerName', headerText: 'Nome do cliente', width: 180 },
                        { field: 'status', headerText: 'Status', width: 120 },
                        { field: 'totalAmount', headerText: 'Total', width: 120, format: 'C2' },
                        { field: 'shippingCost', headerText: 'Frete', width: 120, format: 'C2' },
                        { field: 'shippingBoxData', headerText: 'Caixa de Papelão', width: 360 },
                        { field: 'totalWithShipping', headerText: 'Total + Frete', width: 150, format: 'C2' },
                        { field: 'paymentTypeName', headerText: 'Formas de Pagamento', width: 150 },
                        { field: 'shippingPostCode', headerText: 'CEP', width: 140 },
                        {
                            headerText: 'Itens',
                            width: 120,
                            textAlign: 'Center',
                            template: '<button type="button" class="btn btn-sm btn-outline-primary order-detail-btn" title="Ver itens do pedido"><i class="fa fa-list"></i></button>'
                        },
                        { field: 'orderDate', headerText: 'Data do Pedido', width: 180, format: 'yyyy-MM-dd HH:mm' }
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
                    rowDataBound: (args) => {
                        const button = args.row.querySelector('.order-detail-btn');
                        if (button) {
                            button.addEventListener('click', async (event) => {
                                event.stopPropagation();
                                await methods.showOrderDetails(args.data.id);
                            });
                        }

                        const statusValue = args.data?.status;
                        const statusCell = statusValue
                            ? Array.from(args.row.cells).find(cell => cell.textContent.trim() === statusValue)
                            : null;

                        if (statusCell && statusValue) {
                            const statusClass = {
                                Pending: 'bg-warning text-dark',
                                Paid: 'bg-success',
                                Dispatched: 'bg-info text-dark',
                                Shipped: 'bg-primary',
                                Delivered: 'bg-success',
                                Cancelled: 'bg-danger'
                            }[statusValue] || 'bg-secondary';

                            statusCell.innerHTML = `<span class="badge ${statusClass}">${translateStatus(statusValue)}</span>`;
                        }
                    },
                    toolbarClick: async (args) => {
                        if (args.item.id?.toLowerCase().includes('excelexport')) {
                            mainGrid.obj.excelExport({ fileName: 'Orders.xlsx' });
                        }

                        if (args.item.id === 'AddCustom') {
                            resetForm();
                            state.deleteMode = false;
                            state.mainTitle = 'Adicionar Pedido';
                            methods.addOrderItem();
                            mainModal.obj.show();
                        }

                        if (args.item.id === 'EditCustom' || args.item.id === 'DeleteCustom') {
                            const selected = mainGrid.obj.getSelectedRecords()[0];
                            if (!selected) return;

                            await methods.loadOrder(selected.id);
                            state.deleteMode = args.item.id === 'DeleteCustom';
                            state.mainTitle = state.deleteMode ? 'Excluir Pedido' : 'Editar Pedido';
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

        const methods = {
            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = (response?.data?.content?.data ?? []).map(item => ({
                    ...item,
                    orderDate: new Date(item.orderDate),
                    createdAt: new Date(item.createdAt)
                }));
            },
            loadLookups: async () => {
                const [customers, products, shippingBoxes, paymentTypes] = await Promise.all([
                    services.getCustomers(),
                    services.getProducts(),
                    services.getShippingBoxes(),
                    services.getPaymentTypes()
                ]);

                state.customers = customers?.data?.content?.data ?? [];
                state.products = products?.data?.content?.data ?? [];
                state.shippingBoxes = shippingBoxes?.data?.content?.data ?? [];
                state.paymentTypes = paymentTypes?.data?.content?.data ?? [];
            },
            loadOrder: async (id) => {
                const response = await services.getSingleData(id);
                const order = response?.data?.content?.data;
                if (!order) return;

                resetForm();
                fillOrder(order);
            },
            addOrderItem: () => {
                state.orderDetails.push({
                    id: '',
                    productId: '',
                    quantity: 1,
                    unitPrice: 0
                });
            },
            removeOrderItem: (index) => {
                state.orderDetails.splice(index, 1);
                recalculateTotal();
            },
            handleProductChange: (item) => {
                const product = state.products.find(x => x.id === item.productId);
                item.unitPrice = product?.unitPrice ?? 0;
                recalculateTotal();
            },
            refreshShippingCost: async () => {
                recalculateTotal(calculateShippingCost());

                if (!state.shippingBoxId || !state.shipping.postCode) {
                    return;
                }

                try {
                    const response = await services.calculateShippingCost(state.shippingBoxId, state.shipping.postCode);
                    const shippingCost = Number(response?.data?.content?.data ?? state.shippingCost);
                    recalculateTotal(shippingCost);
                } catch {
                    recalculateTotal();
                }
            },
            handleShippingBoxChange: async () => {
                await methods.refreshShippingCost();
            },
            showShippingBoxInfo: () => {
                const shippingBox = getSelectedShippingBox();
                if (!shippingBox) {
                    Swal.fire({
                        icon: 'info',
                        title: 'Caixa de Envio',
                        text: 'Selecione uma caixa de envio primeiro.'
                    });
                    return;
                }

                Swal.fire({
                    title: 'Caixa de Envio',
                    html: `
                        <div class="table-responsive">
                            <table class="table table-sm table-bordered text-start">
                                <tbody>
                                    <tr><th>Largura</th><td>${formatNumber(shippingBox.width)} cm</td></tr>
                                    <tr><th>Comprimento</th><td>${formatNumber(shippingBox.length)} cm</td></tr>
                                    <tr><th>Altura</th><td>${formatNumber(shippingBox.height)} cm</td></tr>
                                    <tr><th>Peso</th><td>${formatNumber(shippingBox.weight)} kg</td></tr>
                                    <tr><th>Peso Cubico</th><td>${formatNumber(calculateCubicWeight(shippingBox))} kg</td></tr>
                                    <tr><th>Peso Cobrado</th><td>${formatNumber(calculateChargedWeight(shippingBox))} kg</td></tr>
                                    <tr><th>Valor do Seguro</th><td>${formatCurrency(shippingBox.insuranceValue)}</td></tr>
                                    <tr><th>Frete</th><td>${formatCurrency(state.shippingCost)}</td></tr>
                                </tbody>
                            </table>
                        </div>
                    `,
                    width: 560,
                    confirmButtonText: 'Fechar'
                });
            },
            showOrderDetails: async (id) => {
                try {
                    const response = await services.getSingleData(id);
                    const order = response?.data?.content?.data;
                    const items = order?.orderDetails ?? [];

                    if (!items.length) {
                        return Swal.fire({ icon: 'info', title: 'Detalhes do Pedido', text: 'Nenhum item encontrado para este pedido.' });
                    }

                    const rows = items.map(item => {
                        const imageUrl = item.productImageUrl
                            ? '/api/FileImage/GetImage?imageName=' + encodeURIComponent(item.productImageUrl)
                            : null;

                        return `
                            <tr>
                                <td>
                                    <div class="d-flex align-items-center gap-3">
                                        ${imageUrl ? `<img src="${imageUrl}" alt="${item.productName ?? 'Produto'}" style="width:56px;height:56px;object-fit:cover;border-radius:6px;margin-right:1rem;" />` : `<span class="badge bg-secondary">Sem Imagem</span>`}
                                        <span>${item.productName || 'Desconhecido'}</span>
                                    </div>
                                </td>
                                <td class="text-end">${item.quantity}</td>
                                <td class="text-end">${item.unitPrice != null ? Number(item.unitPrice).toFixed(2) : '0.00'}</td>
                                <td class="text-end">${item.totalPrice != null ? Number(item.totalPrice).toFixed(2) : '0.00'}</td>
                            </tr>
                        `;
                    }).join('');

                    await Swal.fire({
                        title: 'Itens do Pedido',
                        html: `
                            <div class="table-responsive">
                                <table class="table table-sm table-bordered">
                                    <thead>
                                        <tr>
                                            <th>Produto</th>
                                            <th class="text-end">Qtd</th>
                                            <th class="text-end">Unitario</th>
                                            <th class="text-end">Total</th>
                                        </tr>
                                    </thead>
                                    <tbody>${rows}</tbody>
                                </table>
                            </div>
                        `,
                        width: 780,
                        confirmButtonText: 'Fechar'
                    });
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'Erro', text: error.response?.data?.message ?? 'Nao foi possivel carregar os detalhes do pedido' });
                }
            }
        };

        const handler = {
            handleSubmit: async () => {
                try {
                    state.isSubmitting = true;
                    state.errors.customerId = '';
                    state.errors.orderDetails = '';

                    if (state.deleteMode) {
                        const deleteResponse = await services.deleteMainData(state.id);

                        if (deleteResponse.data.code === 200) {
                            await methods.populateMainData();
                            mainGrid.refresh();
                            Swal.fire({ icon: 'success', title: 'Excluido com Sucesso', timer: 1000, showConfirmButton: false });
                            setTimeout(() => mainModal.obj.hide(), 1000);
                        }

                        return;
                    }

                    if (!state.customerId) {
                        state.errors.customerId = 'Cliente e obrigatorio.';
                        return;
                    }

                    if (!state.orderDetails.length || state.orderDetails.some(x => !x.productId)) {
                        state.errors.orderDetails = 'Adicione pelo menos um produto.';
                        return;
                    }

                    await methods.refreshShippingCost();
                    const request = buildPayload();
                    const response = state.id
                        ? await services.updateMainData(request)
                        : await services.createMainData(request);

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();

                        Swal.fire({ icon: 'success', title: 'Salvo com Sucesso', timer: 1000, showConfirmButton: false });
                        setTimeout(() => mainModal.obj.hide(), 1000);
                    } else {
                        Swal.fire({ icon: 'error', title: 'Falha ao Salvar', text: response.data.message ?? 'Erro' });
                    }
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'Erro', text: error.response?.data?.message ?? 'Erro inesperado' });
                } finally {
                    state.isSubmitting = false;
                }
            },
            recalculateTotal
        };

        const formatDateTimeValue = (value) => {
            if (!value) return '';
            const date = new Date(value);
            if (Number.isNaN(date.getTime())) return '';
            return date.toISOString().slice(0, 16);
        };

        const formatDate = (value) => {
            if (!value) return '';
            const date = new Date(value);
            if (Number.isNaN(date.getTime())) return '';
            return date.toLocaleString();
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['Orders']);
                await SecurityManager.validateToken();

                await mainGrid.create(state.mainData);
                mainModal.create();

                mainModalRef.value.addEventListener('hidden.bs.modal', () => {
                    resetForm();
                });

                try {
                    await methods.populateMainData();
                    mainGrid.refresh();
                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Pedidos',
                        text: error.response?.data?.message ?? 'Nao foi possivel carregar os pedidos.'
                    });
                }

                try {
                    await methods.loadLookups();
                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Dados do Pedido',
                        text: error.response?.data?.message ?? 'Nao foi possivel carregar clientes, produtos, caixas de envio ou formas de pagamento.'
                    });
                }
            } catch (e) {
                console.error(e);
                Swal.fire({
                    icon: 'error',
                    title: 'Pedidos',
                    text: e.response?.data?.message ?? e.message ?? 'Nao foi possivel abrir a pagina de pedidos.'
                });
            }
        });

        return {
            state,
            mainGridRef,
            mainModalRef,
            handler,
            methods,
            translateStatus,
            formatDate,
            calculateItemsTotal,
            calculateCubicWeight,
            calculateChargedWeight,
            getSelectedShippingBox,
            getItemShippingCost,
            getItemTotal,
            formatCurrency,
            formatNumber,
            formatShippingBoxData
        };
    }
};

Vue.createApp(App).mount('#app');
