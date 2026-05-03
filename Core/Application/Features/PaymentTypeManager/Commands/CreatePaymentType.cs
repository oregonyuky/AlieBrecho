using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.PaymentTypeManager.Commands;

public class CreatePaymentTypeResult
{
    public PaymentType? Data { get; set; }
}

public class CreatePaymentTypeRequest : IRequest<CreatePaymentTypeResult>
{
    public string? TypeName { get; init; }
    public string? Description { get; init; }
    public bool? IsActive { get; init; }
}

public class CreatePaymentTypeValidator : AbstractValidator<CreatePaymentTypeRequest>
{
    public CreatePaymentTypeValidator()
    {
        RuleFor(x => x.TypeName).NotEmpty();
    }
}

public class CreatePaymentTypeHandler : IRequestHandler<CreatePaymentTypeRequest, CreatePaymentTypeResult>
{
    private readonly ICommandRepository<PaymentType> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePaymentTypeHandler(
        ICommandRepository<PaymentType> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreatePaymentTypeResult> Handle(CreatePaymentTypeRequest request, CancellationToken cancellationToken)
    {
        var entity = new PaymentType
        {
            TypeName = request.TypeName ?? string.Empty,
            Description = request.Description,
            IsActive = request.IsActive ?? true,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreatePaymentTypeResult
        {
            Data = entity
        };
    }
}
