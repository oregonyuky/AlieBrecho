using Application.Common.Repositories;
using Domain.Entities;

namespace Infrastructure.SeedManager.Demos;

public class CustomerSeeder
{
    private readonly ICommandRepository<Customer> _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerSeeder(
        ICommandRepository<Customer> customerRepository,
        IUnitOfWork unitOfWork
    )
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var customers = new List<Customer>
        {
            new Customer
            {
                Name = "Cliente Exemplo 1",
                EmailAddress = "contato@cliente1.com",
                PhoneNumber = "11999999999",
                Cpf = "52998224725",
                Street = "Rua Teste",
                Number = "123",
                Neighborhood = "Centro",
                City = "Sao Paulo",
                State = "SP",
                PostalCode = "01001000",
                Complement = "Apto 1",
                CustomerStatus = "Active"
            },
            new Customer
            {
                Name = "Brecho das Palmeiras",
                EmailAddress = "vendas@palmeiras.com",
                PhoneNumber = "11888888888",
                Cpf = "15350946056",
                Street = "Av. das Flores",
                Number = "500",
                Neighborhood = "Jardins",
                City = "Sao Paulo",
                State = "SP",
                PostalCode = "01405001",
                Complement = "Loja B",
                CustomerStatus = "Active"
            }
        };

        foreach (var customer in customers)
        {
            var exists = _customerRepository.GetQuery().Any(c => c.EmailAddress == customer.EmailAddress);
            if (!exists)
            {
                await _customerRepository.CreateAsync(customer);
            }
        }

        await _unitOfWork.SaveAsync();
    }
}
