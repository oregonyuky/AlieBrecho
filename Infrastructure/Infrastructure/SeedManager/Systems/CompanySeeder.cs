using Application.Common.Repositories;
using Domain.Entities;

namespace Infrastructure.SeedManager.Systems;

public class CompanySeeder
{
    private readonly ICommandRepository<Company> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public CompanySeeder(
        ICommandRepository<Company> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }
    public async Task GenerateDataAsync()
    {
        var entity = new Company
        {
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false,
            Name = "AlieBrecho",
            Currency = "BRL",
            Street = "Rua das Flores",
            City = "Sao Paulo",
            State = "SP",
            ZipCode = "01001000",
            Country = "Brasil",
            PhoneNumber = "11999999999",
            FaxNumber = "",
            EmailAddress = "contato@aliebrecho.com",
            Website = "https://www.aliebrecho.com"
        };

        await _repository.CreateAsync(entity);
        await _unitOfWork.SaveAsync();
    }

}
