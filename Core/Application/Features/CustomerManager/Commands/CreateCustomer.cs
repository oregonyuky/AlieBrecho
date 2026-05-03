using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.CustomerManager.Commands;

public class CreateCustomerResult
{
    public Customer? Data { get; set; }
}

public class CreateCustomerRequest : IRequest<CreateCustomerResult>
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Cpf { get; init; }
    public string? PhoneNumber { get; init; }
    public string? EmailAddress { get; init; }
    public string? Street { get; init; }
    public string? Number { get; init; }
    public string? Neighborhood { get; init; }
    public string? Complement { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
    public string? Website { get; init; }
    public string? Instagram { get; init; }
    public string? TwitterX { get; init; }
    public string? TikTok { get; init; }
    public string? CustomerStatus { get; init; }
}

public class CreateCustomerValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class CreateCustomerHandler : IRequestHandler<CreateCustomerRequest, CreateCustomerResult>
{
    private readonly ICommandRepository<Customer> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCustomerHandler(
        ICommandRepository<Customer> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateCustomerResult> Handle(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var entity = new Customer
        {
            Name = request.Name,
            Description = request.Description,
            Cpf = request.Cpf,
            PhoneNumber = request.PhoneNumber,
            EmailAddress = request.EmailAddress,
            Street = request.Street,
            Number = request.Number,
            Neighborhood = request.Neighborhood,
            Complement = request.Complement,
            City = request.City,
            State = request.State,
            PostalCode = request.PostalCode,
            Country = request.Country,
            Website = request.Website,
            Instagram = request.Instagram,
            TwitterX = request.TwitterX,
            TikTok = request.TikTok,
            CustomerStatus = string.IsNullOrWhiteSpace(request.CustomerStatus) ? "Active" : request.CustomerStatus,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateCustomerResult
        {
            Data = entity
        };
    }
}
