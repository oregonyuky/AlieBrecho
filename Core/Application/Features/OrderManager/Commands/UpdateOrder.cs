using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.OrderManager.Commands;

public record PaymentEditDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Status { get; init; }
    public DateTime? PaymentDateTime { get; init; }
    public decimal? Amount { get; init; }
    public string? PaymentTypeId { get; init; }
    public string? PaymentMethod { get; init; }
    public string? TransactionId { get; init; }
    public string? AuthorizationCode { get; init; }
    public string? ReferenceNumber { get; init; }
    public string? CardHolderName { get; init; }
    public string? CardLast4 { get; init; }
}

public record ShippingDetailEditDto
{
    public string? Id { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Street { get; init; }
    public string? Number { get; init; }
    public string? Neighborhood { get; init; }
    public string? Complement { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostCode { get; init; }
}

public class UpdateOrderResult
{
    public Order? Data { get; set; }
}

public class UpdateOrderRequest : IRequest<UpdateOrderResult>
{
    public string? Id { get; init; }
    public string? Status { get; init; }
    public decimal? Discount { get; init; }
    public decimal? Taxes { get; init; }
    public decimal? TotalAmount { get; init; }
    public string? Notes { get; init; }
    public PaymentEditDto? Payment { get; init; }
    public ShippingDetailEditDto? ShippingDetail { get; init; }
}

public class UpdateOrderValidator : AbstractValidator<UpdateOrderRequest>
{
    public UpdateOrderValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class UpdateOrderHandler : IRequestHandler<UpdateOrderRequest, UpdateOrderResult>
{
    private readonly ICommandRepository<Order> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateOrderHandler(
        ICommandRepository<Order> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateOrderResult> Handle(UpdateOrderRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetQuery()
            .Include(x => x.Payment)
                .ThenInclude(x => x!.PaymentDetail)
            .Include(x => x.ShippingDetail)
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Order not found: {request.Id}");
        }

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<OrderStatus>(request.Status, out var status))
        {
            entity.Status = status;
        }

        entity.Discount = request.Discount;
        entity.Taxes = request.Taxes;
        entity.TotalAmount = request.TotalAmount;
        entity.Notes = request.Notes;

        if (request.Payment != null)
        {
            var payment = entity.Payment ?? new Payment();
            payment.Name = request.Payment.Name;
            payment.Description = request.Payment.Description;
            payment.Status = !string.IsNullOrWhiteSpace(request.Payment.Status) && Enum.TryParse<PaymentStatus>(request.Payment.Status, out var paymentStatus)
                ? paymentStatus
                : payment.Status;
            payment.PaymentDateTime = request.Payment.PaymentDateTime;
            payment.Amount = request.Payment.Amount;
            payment.PaymentTypeId = request.Payment.PaymentTypeId;

            if (payment.PaymentDetail == null)
            {
                payment.PaymentDetail = new PaymentDetail
                {
                    PaymentId = payment.Id
                };
            }
            else if (string.IsNullOrWhiteSpace(payment.PaymentDetail.PaymentId))
            {
                payment.PaymentDetail.PaymentId = payment.Id;
            }

            payment.PaymentDetail.PaymentMethod = request.Payment.PaymentMethod;
            payment.PaymentDetail.TransactionId = request.Payment.TransactionId;
            payment.PaymentDetail.AuthorizationCode = request.Payment.AuthorizationCode;
            payment.PaymentDetail.ReferenceNumber = request.Payment.ReferenceNumber;
            payment.PaymentDetail.CardHolderName = request.Payment.CardHolderName;
            payment.PaymentDetail.CardLast4 = request.Payment.CardLast4;

            if (entity.Payment == null)
            {
                entity.Payment = payment;
                entity.PaymentId = payment.Id;
            }
        }

        if (request.ShippingDetail != null)
        {
            var shipping = entity.ShippingDetail ?? new ShippingDetail();
            shipping.FirstName = request.ShippingDetail.FirstName;
            shipping.LastName = request.ShippingDetail.LastName;
            shipping.Email = request.ShippingDetail.Email;
            shipping.PhoneNumber = request.ShippingDetail.PhoneNumber;
            shipping.Street = request.ShippingDetail.Street;
            shipping.Number = request.ShippingDetail.Number;
            shipping.Neighborhood = request.ShippingDetail.Neighborhood;
            shipping.Complement = request.ShippingDetail.Complement;
            shipping.City = request.ShippingDetail.City;
            shipping.State = request.ShippingDetail.State;
            shipping.PostCode = request.ShippingDetail.PostCode;

            if (entity.ShippingDetail == null)
            {
                entity.ShippingDetail = shipping;
                shipping.OrderId = entity.Id;
            }
        }

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateOrderResult
        {
            Data = entity
        };
    }
}
