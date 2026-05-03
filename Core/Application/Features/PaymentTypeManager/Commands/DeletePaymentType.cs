using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.PaymentTypeManager.Commands;

public class DeletePaymentTypeResult
{
    public PaymentType? Data { get; set; }
}

public class DeletePaymentTypeRequest : IRequest<DeletePaymentTypeResult>
{
    public string? Id { get; init; }
}

public class DeletePaymentTypeValidator : AbstractValidator<DeletePaymentTypeRequest>
{
    public DeletePaymentTypeValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeletePaymentTypeHandler : IRequestHandler<DeletePaymentTypeRequest, DeletePaymentTypeResult>
{
    private readonly ICommandRepository<PaymentType> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeletePaymentTypeHandler(
        ICommandRepository<PaymentType> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeletePaymentTypeResult> Handle(DeletePaymentTypeRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeletePaymentTypeResult
        {
            Data = entity
        };
    }
}
