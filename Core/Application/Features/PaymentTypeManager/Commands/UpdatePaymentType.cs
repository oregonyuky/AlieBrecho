using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.PaymentTypeManager.Commands;

public class UpdatePaymentTypeResult
{
    public PaymentType? Data { get; set; }
}

public class UpdatePaymentTypeRequest : IRequest<UpdatePaymentTypeResult>
{
    public string? Id { get; init; }
    public string? TypeName { get; init; }
    public string? Description { get; init; }
    public bool? IsActive { get; init; }
}

public class UpdatePaymentTypeValidator : AbstractValidator<UpdatePaymentTypeRequest>
{
    public UpdatePaymentTypeValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.TypeName).NotEmpty();
    }
}

public class UpdatePaymentTypeHandler : IRequestHandler<UpdatePaymentTypeRequest, UpdatePaymentTypeResult>
{
    private readonly ICommandRepository<PaymentType> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePaymentTypeHandler(
        ICommandRepository<PaymentType> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdatePaymentTypeResult> Handle(UpdatePaymentTypeRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.TypeName = request.TypeName;
        entity.Description = request.Description;

        if (request.IsActive.HasValue)
        {
            entity.IsActive = request.IsActive.Value;
        }

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdatePaymentTypeResult
        {
            Data = entity
        };
    }
}
