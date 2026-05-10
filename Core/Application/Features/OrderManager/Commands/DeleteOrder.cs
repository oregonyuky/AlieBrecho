using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.OrderManager.Commands;

public class DeleteOrderResult
{
    public Order? Data { get; set; }
}

public class DeleteOrderRequest : IRequest<DeleteOrderResult>
{
    public string? Id { get; init; }
}

public class DeleteOrderValidator : AbstractValidator<DeleteOrderRequest>
{
    public DeleteOrderValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteOrderHandler : IRequestHandler<DeleteOrderRequest, DeleteOrderResult>
{
    private readonly ICommandRepository<Order> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteOrderHandler(
        ICommandRepository<Order> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteOrderResult> Handle(DeleteOrderRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetQuery()
            .Include(x => x.Payment)
                .ThenInclude(x => x!.PaymentDetail)
            .Include(x => x.ShippingDetail)
            .Include(x => x.OrderDetails)
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Order not found: {request.Id}");
        }

        _repository.Delete(entity);

        if (entity.Payment != null)
        {
            entity.Payment.IsDeleted = true;
            if (entity.Payment.PaymentDetail != null)
            {
                entity.Payment.PaymentDetail.IsDeleted = true;
            }
        }

        if (entity.ShippingDetail != null)
        {
            entity.ShippingDetail.IsDeleted = true;
        }

        foreach (var item in entity.OrderDetails)
        {
            item.IsDeleted = true;
        }

        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteOrderResult
        {
            Data = entity
        };
    }
}
