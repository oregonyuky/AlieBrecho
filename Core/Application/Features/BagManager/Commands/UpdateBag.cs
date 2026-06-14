using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.BagManager.Commands;

public class UpdateBagResult
{
    public Bag? Data { get; set; }
}

public class UpdateBagRequest : IRequest<UpdateBagResult>
{
    public string? Id { get; init; }
    public string? Status { get; init; }
    public DateTime ExpirationDate { get; init; }
    public DateTime LastInteractionAt { get; init; }
    public DateTime? ClosedAt { get; init; }
    public decimal TotalItemsValue { get; init; }
    public decimal? ShippingCost { get; init; }
    public decimal TotalWeight { get; init; }
    public bool AllItemsPaid { get; init; }
    public string? Notes { get; init; }
}

public class UpdateBagValidator : AbstractValidator<UpdateBagRequest>
{
    public UpdateBagValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class UpdateBagHandler : IRequestHandler<UpdateBagRequest, UpdateBagResult>
{
    private readonly ICommandRepository<Bag> _repository;
    private readonly ICommandRepository<Product> _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBagHandler(
        ICommandRepository<Bag> repository,
        ICommandRepository<Product> productRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateBagResult> Handle(UpdateBagRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetQuery()
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Bag not found: {request.Id}");
        }

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<BagStatus>(request.Status, out var status))
        {
            entity.Status = status;
        }

        entity.ExpirationDate = request.ExpirationDate;
        entity.LastInteractionAt = request.LastInteractionAt;
        entity.ClosedAt = request.ClosedAt;
        entity.TotalItemsValue = request.TotalItemsValue;
        entity.ShippingCost = request.ShippingCost;
        entity.TotalWeight = request.TotalWeight;
        entity.AllItemsPaid = request.AllItemsPaid;
        entity.Notes = request.Notes;

        await MarkProductsUnavailableWhenPaidAsync(entity, cancellationToken);

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateBagResult
        {
            Data = entity
        };
    }

    private async Task MarkProductsUnavailableWhenPaidAsync(Bag entity, CancellationToken cancellationToken)
    {
        if (!entity.AllItemsPaid)
        {
            return;
        }

        var productIds = (entity.Items ?? [])
            .Where(x => !x.IsDeleted && !string.IsNullOrWhiteSpace(x.ProductId))
            .Select(x => x.ProductId!)
            .Distinct()
            .ToList();

        if (productIds.Count == 0)
        {
            return;
        }

        var products = await _productRepository.GetQuery()
            .Where(x => productIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        foreach (var product in products)
        {
            product.ProductAvailable = false;
            _productRepository.Update(product);
        }
    }
}
