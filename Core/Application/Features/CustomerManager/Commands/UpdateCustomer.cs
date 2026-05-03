using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.CustomerManager.Commands;

public class UpdateCustomerResult
{
    public Customer? Data { get; set; }
}

public class UpdateCustomerRequest : IRequest<UpdateCustomerResult>
{
    public string? Id { get; init; }
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

public class UpdateCustomerValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class UpdateCustomerHandler : IRequestHandler<UpdateCustomerRequest, UpdateCustomerResult>
{
    private readonly ICommandRepository<Customer> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCustomerHandler(
        ICommandRepository<Customer> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateCustomerResult> Handle(UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.Cpf = request.Cpf;
        entity.PhoneNumber = request.PhoneNumber;
        entity.EmailAddress = request.EmailAddress;
        entity.Street = request.Street;
        entity.Number = request.Number;
        entity.Neighborhood = request.Neighborhood;
        entity.Complement = request.Complement;
        entity.City = request.City;
        entity.State = request.State;
        entity.PostalCode = request.PostalCode;
        entity.Country = request.Country;
        entity.Website = request.Website;
        entity.Instagram = request.Instagram;
        entity.TwitterX = request.TwitterX;
        entity.TikTok = request.TikTok;
        entity.CustomerStatus = request.CustomerStatus;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateCustomerResult
        {
            Data = entity
        };
    }
}
