using Application.Common.Repositories;
using Application.Common.Services;
using Domain.Entities;
using Domain.Enums;

namespace Infrastructure.SeedManager.Demos;

public class OrderSeeder
{
    private readonly ICommandRepository<Order> _orderRepository;
    private readonly ICommandRepository<Customer> _customerRepository;
    private readonly ICommandRepository<Product> _productRepository;
    private readonly ICommandRepository<PaymentType> _paymentTypeRepository;
    private readonly ICommandRepository<ShippingBox> _shippingBoxRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OrderSeeder(
        ICommandRepository<Order> orderRepository,
        ICommandRepository<Customer> customerRepository,
        ICommandRepository<Product> productRepository,
        ICommandRepository<PaymentType> paymentTypeRepository,
        ICommandRepository<ShippingBox> shippingBoxRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _paymentTypeRepository = paymentTypeRepository;
        _shippingBoxRepository = shippingBoxRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var customers = _customerRepository.GetQuery().ToList();
        var paymentTypes = _paymentTypeRepository.GetQuery().ToList();
        var products = _productRepository.GetQuery().ToList();
        var shippingBoxes = _shippingBoxRepository.GetQuery().ToList();

        if (!customers.Any() || !paymentTypes.Any() || !products.Any() || !shippingBoxes.Any())
        {
            return;
        }

        var customer = customers.First();
        var paymentType = paymentTypes.First();
        var shippingBox1 = shippingBoxes.First();
        var shippingBox2 = shippingBoxes.Count > 1 ? shippingBoxes[1] : shippingBox1;

        var order1 = new Order
        {
            CustomerId = customer.Id,
            Customer = customer,
            ShippingBoxId = shippingBox1.Id,
            ShippingBox = shippingBox1,
            Status = OrderStatus.Paid,
            OrderDate = DateTime.UtcNow.AddDays(-3),
            Discount = 10m,
            Taxes = 5m,
            Notes = "Pedido de exemplo para teste.",
            Payment = new Payment
            {
                Name = "Pagamento do Pedido 1",
                Description = "Pagamento via cartão de crédito.",
                Status = PaymentStatus.Paid,
                PaymentDateTime = DateTime.UtcNow.AddDays(-2),
                Amount = 149.80m,
                PaymentTypeId = paymentType.Id,
                PaymentType = paymentType,
                PaymentDetail = new PaymentDetail
                {
                    PaymentMethod = "Credit Card",
                    TransactionId = "TXN-1001",
                    AuthorizationCode = "AUTH1001",
                    ReferenceNumber = "REF1001",
                    CardHolderName = "Cliente Exemplo 1",
                    CardLast4 = "4242"
                }
            },
            ShippingDetail = new ShippingDetail
            {
                FirstName = "Mariana",
                LastName = "Silva",
                Email = "mariana.silva@example.com",
                PhoneNumber = "+55 (11) 99999-9999",
                Street = "Rua das Flores",
                Number = "123",
                Neighborhood = "Jardim Paulista",
                City = "São Paulo",
                State = "SP",
                PostCode = "01405-001",
                OrderId = string.Empty
            },
            OrderDetails = new List<OrderDetail>()
        };

        var firstProduct = products.First();
        order1.OrderDetails.Add(new OrderDetail
        {
            ProductId = firstProduct.Id,
            Product = firstProduct,
            Quantity = 1,
            UnitPrice = firstProduct.UnitPrice,
            TotalPrice = firstProduct.UnitPrice
        });

        if (products.Count > 1)
        {
            var secondProduct = products[1];
            order1.OrderDetails.Add(new OrderDetail
            {
                ProductId = secondProduct.Id,
                Product = secondProduct,
                Quantity = 2,
                UnitPrice = secondProduct.UnitPrice,
                TotalPrice = secondProduct.UnitPrice * 2
            });
        }

        order1.TotalAmount = order1.OrderDetails.Sum(x => x.TotalPrice ?? 0m)
            - (order1.Discount ?? 0m)
            + (order1.Taxes ?? 0m)
            + ShippingCostCalculator.Calculate(order1.ShippingBox);
        order1.ShippingDetail.OrderId = order1.Id;

        var order2 = new Order
        {
            CustomerId = customers.Count > 1 ? customers[1].Id : customer.Id,
            Customer = customers.Count > 1 ? customers[1] : customer,
            ShippingBoxId = shippingBox2.Id,
            ShippingBox = shippingBox2,
            Status = OrderStatus.Shipped,
            OrderDate = DateTime.UtcNow.AddDays(-1),
            Discount = 0m,
            Taxes = 8m,
            Notes = "Pedido demo com frete e mais produtos.",
            Payment = new Payment
            {
                Name = "Pagamento do Pedido 2",
                Description = "Pagamento via transferência bancária.",
                Status = PaymentStatus.Paid,
                PaymentDateTime = DateTime.UtcNow.AddDays(-1),
                Amount = 99.90m,
                PaymentTypeId = paymentType.Id,
                PaymentType = paymentType,
                PaymentDetail = new PaymentDetail
                {
                    PaymentMethod = "Bank Transfer",
                    TransactionId = "TXN-2002",
                    AuthorizationCode = "AUTH2002",
                    ReferenceNumber = "REF2002",
                    CardHolderName = customers.Count > 1 ? customers[1].Name : customer.Name,
                    CardLast4 = "0000"
                }
            },
            ShippingDetail = new ShippingDetail
            {
                FirstName = "Carlos",
                LastName = "Pereira",
                Email = "carlos.pereira@example.com",
                PhoneNumber = "+55 (21) 98888-7777",
                Street = "Avenida Oceânica",
                Number = "987",
                Neighborhood = "Copacabana",
                City = "Rio de Janeiro",
                State = "RJ",
                PostCode = "22070-002",
                OrderId = string.Empty
            },
            OrderDetails = new List<OrderDetail>()
        };

        var thirdProduct = products.Count > 2 ? products[2] : firstProduct;
        order2.OrderDetails.Add(new OrderDetail
        {
            ProductId = thirdProduct.Id,
            Product = thirdProduct,
            Quantity = 1,
            UnitPrice = thirdProduct.UnitPrice,
            TotalPrice = thirdProduct.UnitPrice
        });

        order2.TotalAmount = order2.OrderDetails.Sum(x => x.TotalPrice ?? 0m)
            - (order2.Discount ?? 0m)
            + (order2.Taxes ?? 0m)
            + ShippingCostCalculator.Calculate(order2.ShippingBox);
        order2.ShippingDetail.OrderId = order2.Id;

        await _orderRepository.CreateAsync(order1);
        await _orderRepository.CreateAsync(order2);

        await _unitOfWork.SaveAsync();
    }
}
