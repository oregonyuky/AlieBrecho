const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            mainTitle: 'Edit Order',
            id: '',
            status: 'Pending',
            orderDate: new Date().toISOString(),
            discount: 0,
            taxes: 0,
            totalAmount: 0,
            shippingCost: 0,
            notes: '',
            payment: {
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
            },
            shipping: {
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
            },
            paymentTypes: [],
            paymentStatuses: ['Pending', 'Paid', 'Cancelled'],
            orderStatuses: ['Pending', 'Paid', 'Dispatched', 'Shipped', 'Delivered', 'Cancelled'],
            package: {
                height: 0,
                width: 0,
                length: 0,
                weight: 0,
                insuranceCost: 0
            },
            isSubmitting: false
        });

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);

        const services = {
            getMainData: async () => {
                try {
                    return await AxiosManager.get('/Order/GetOrderList', { params: { status: 'Paid' } });
                } catch (error) {
                    throw error;
                }
            },
            getSingleData: async (id) => {
                try {
                    return await AxiosManager.get('/Order/GetOrderSingle', { params: { id } });
                } catch (error) {
                    throw error;
                }
            },
            updateMainData: async (request) => {
                try {
                    return await AxiosManager.post('/Order/UpdateOrder', request);
                } catch (error) {
                    throw error;
                }
            },
            getPaymentTypes: async () => {
                try {
                    return await AxiosManager.get('/PaymentType/GetPaymentTypeList', {});
                } catch (error) {
                    throw error;
                }
            },
            generateLabel: async (request) => {
                try {
                    return await AxiosManager.post('/Order/GenerateShippingLabel', request);
                } catch (error) {
                    throw error;
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
                    filterSettings: { type: 'Menu' },
                    allowSorting: true,
                    allowPaging: true,
                    allowSelection: true,
                    allowResizing: true,
                    pageSettings: { pageSize: 30 },
                    selectionSettings: { type: 'Single', mode: 'Row' },
                    columns: [
                        { type: 'checkbox', width: 60 },
                        { field: 'id', isPrimaryKey: true, visible: false },
                        { field: 'customerName', headerText: 'Customer', width: 180 },
                        { field: 'status', headerText: 'Status', width: 120 },
                        { field: 'totalAmount', headerText: 'Total', width: 120, format: 'C2' },
                        { field: 'paymentTypeName', headerText: 'Payment Method', width: 140 },
                        { field: 'shippingPostCode', headerText: 'Post Code', width: 140 },
                        {
                            headerText: 'Items', width: 120, textAlign: 'Center', template: '<button type="button" class="btn btn-sm btn-outline-primary order-detail-btn" title="Show order items"><i class="fa fa-list"></i></button>'
                        },
                        { field: 'orderDate', headerText: 'Order Date', width: 180, format: 'yyyy-MM-dd HH:mm' }
                    ],
                    toolbar: [
                        'Search',
                        { type: 'Separator' },
                        { text: 'Edit', tooltipText: 'Edit', prefixIcon: 'e-edit', id: 'EditCustom' }
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

                            statusCell.innerHTML = `<span class="badge ${statusClass}">${statusValue}</span>`;
                        }
                    },
                    toolbarClick: async (args) => {
                        if (args.item.id === 'EditCustom') {
                            const selected = mainGrid.obj.getSelectedRecords()[0];
                            if (!selected) return;

                            await methods.loadOrder(selected.id);
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

        const packageModal = {
            obj: null,
            create: () => {
                packageModal.obj = new bootstrap.Modal(packageModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        const methods = {
            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = response?.data?.content?.data.map(item => ({
                    ...item,
                    orderDate: new Date(item.orderDate),
                    createdAt: new Date(item.createdAt)
                }));
            },
            loadPaymentTypes: async () => {
                const response = await services.getPaymentTypes();
                state.paymentTypes = response?.data?.content?.data ?? [];
            },
            loadOrder: async (id) => {
                const response = await services.getSingleData(id);
                const order = response?.data?.content?.data;
                if (!order) return;

                state.id = order.id ?? '';
                state.status = order.status ?? 'Pending';
                state.orderDate = order.orderDate ?? new Date().toISOString();
                state.discount = order.discount ?? 0;
                state.taxes = order.taxes ?? 0;
                state.totalAmount = order.totalAmount ?? 0;
                state.shippingCost = order.shippingCost ?? 0;
                state.notes = order.notes ?? '';
                state.package.height = order.height ?? 0;
                state.package.width = order.width ?? 0;
                state.package.length = order.length ?? 0;
                state.package.weight = order.weight ?? 0;
                state.package.insuranceCost = order.insuranceCost ?? 0;

                state.payment.id = order.payment?.id ?? '';
                state.payment.name = order.payment?.name ?? '';
                state.payment.description = order.payment?.description ?? '';
                state.payment.status = order.payment?.status ?? 'Pending';
                state.payment.paymentDateTime = order.payment?.paymentDateTime ? formatDateTimeValue(order.payment.paymentDateTime) : '';
                state.payment.amount = order.payment?.amount ?? 0;
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
            },
            showOrderDetails: async (id) => {
                try {
                    const response = await services.getSingleData(id);
                    const order = response?.data?.content?.data;
                    const items = order?.orderDetails ?? [];

                    if (!items.length) {
                        return Swal.fire({ icon: 'info', title: 'Order Details', text: 'No items found for this order.' });
                    }

                    const rows = items.map(item => {
                        const imageUrl = item.productImageUrl
                            ? '/api/FileImage/GetImage?imageName=' + encodeURIComponent(item.productImageUrl)
                            : null;

                        return `
                        <tr>
                            <td>
                                <div class="d-flex align-items-center gap-3">
                                    ${imageUrl ? `<img src="${imageUrl}" alt="${item.productName ?? 'Product'}" style="width:56px;height:56px;object-fit:cover;border-radius:6px;margin-right:1rem;" />` : `<span class="badge bg-secondary">No Image</span>`}
                                    <span>${item.productName || 'Unknown'}</span>
                                </div>
                            </td>
                            <td class="text-end">${item.quantity}</td>
                            <td class="text-end">${item.unitPrice != null ? Number(item.unitPrice).toFixed(2) : '0.00'}</td>
                            <td class="text-end">${item.totalPrice != null ? Number(item.totalPrice).toFixed(2) : '0.00'}</td>
                        </tr>
                    `;
                    }).join('');

                    await Swal.fire({
                        title: 'Order Items',
                        html: `
                            <div class="table-responsive">
                                <table class="table table-sm table-bordered">
                                    <thead>
                                        <tr>
                                            <th>Product</th>
                                            <th class="text-end">Qty</th>
                                            <th class="text-end">Unit</th>
                                            <th class="text-end">Total</th>
                                        </tr>
                                    </thead>
                                    <tbody>${rows}</tbody>
                                </table>
                            </div>
                        `,
                        width: 780,
                        confirmButtonText: 'Close'
                    });
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'Error', text: error.response?.data?.message ?? 'Unable to load order details' });
                }
            }
        };

        const handler = {
            handleSubmit: async () => {
                try {
                    state.isSubmitting = true;
                    await new Promise(r => setTimeout(r, 150));

                    const request = {
                        id: state.id,
                        status: state.status,
                        discount: state.discount,
                        taxes: state.taxes,
                        totalAmount: state.totalAmount,
                        shippingCost: state.shippingCost,
                        height: state.package.height,
                        width: state.package.width,
                        length: state.package.length,
                        weight: state.package.weight,
                        insuranceCost: state.package.insuranceCost,
                        notes: state.notes,
                        payment: {
                            id: state.payment.id,
                            status: state.payment.status,
                            paymentDateTime: state.payment.paymentDateTime ? new Date(state.payment.paymentDateTime) : null,
                            amount: state.payment.amount,
                            paymentTypeId: state.payment.paymentTypeId,
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
                        }
                    };

                    const response = await services.updateMainData(request);

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();

                        Swal.fire({ icon: 'success', title: 'Save Successful', timer: 1000, showConfirmButton: false });
                        setTimeout(() => mainModal.obj.hide(), 1000);
                    } else {
                        Swal.fire({ icon: 'error', title: 'Save Failed', text: response.data.message ?? 'Error' });
                    }
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'Error', text: error.response?.data?.message ?? 'Unexpected error' });
                } finally {
                    state.isSubmitting = false;
                }
            },
            handleGenerateLabel: () => {
                if (!state.id) {
                    Swal.fire({ icon: 'error', title: 'Error', text: 'Order not loaded' });
                    return;
                }

                state.package.height = 0;
                state.package.width = 0;
                state.package.length = 0;
                state.package.weight = 0;
                state.package.insuranceCost = 0;

                packageModal.obj.show();
            },
            handleSubmitPackage: async () => {
                try {
                    state.isSubmitting = true;
                    await new Promise(r => setTimeout(r, 150));

                    if (!state.package.height || !state.package.width || !state.package.length || !state.package.weight) {
                        Swal.fire({ icon: 'warning', title: 'Validation', text: 'Please fill all package dimensions' });
                        return;
                    }

                    const request = {
                        id: state.id,
                        height: state.package.height,
                        width: state.package.width,
                        length: state.package.length,
                        weight: state.package.weight,
                        insuranceCost: state.package.insuranceCost || 0
                    };

                    const response = await services.generateLabel(request);

                    if (response.data.code === 200) {
                        state.shippingCost = response.data.content?.shippingCost ?? 0;

                        await Swal.fire({
                            icon: 'success',
                            title: 'Label Generated',
                            html: `
                                <div class="text-start">
                                    <p><strong>Shipping Cost:</strong> R$ ${state.shippingCost?.toFixed(2)}</p>
                                    <p><small class="text-muted">The shipping cost has been updated in the order.</small></p>
                                </div>
                            `,
                            timer: 3000,
                            showConfirmButton: false
                        });

                        packageModal.obj.hide();
                    } else {
                        Swal.fire({ icon: 'error', title: 'Generation Failed', text: response.data.message ?? 'Error' });
                    }
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'Error', text: error.response?.data?.message ?? 'Unexpected error' });
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

                await methods.populateMainData();
                await methods.loadPaymentTypes();
                await mainGrid.create(state.mainData);
                mainModal.create();
                packageModal.create();

                mainModalRef.value.addEventListener('hidden.bs.modal', () => {
                    state.id = '';
                    state.status = 'Pending';
                    state.orderDate = new Date().toISOString();
                    state.discount = 0;
                    state.taxes = 0;
                    state.totalAmount = 0;
                    state.shippingCost = 0;
                    state.notes = '';
                    state.package = {
                        height: 0,
                        width: 0,
                        length: 0,
                        weight: 0,
                        insuranceCost: 0
                    };
                    state.payment = {
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
                    };
                    state.shipping = {
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
                    };
                });
            } catch (e) {
                console.error(e);
            }
        });

        return {
            state,
            mainGridRef,
            mainModalRef,
            packageModalRef,
            handler,
            methods,
            formatDate
        };
    }
};

Vue.createApp(App).mount('#app');
