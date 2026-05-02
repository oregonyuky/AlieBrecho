using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ProductManager.Commands;

public class UpdateProductResult
{
    public Product? Data { get; set; }
}

public class UpdateProductSizeRequest
{
    public string? Size { get; init; }
    public decimal? Bust { get; init; }
    public decimal? Sleeve { get; init; }
    public decimal? Length { get; init; }
}

public class UpdateProductRequest : IRequest<UpdateProductResult>
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? CategoryID { get; init; }
    public decimal? UnitPrice { get; init; }
    public decimal? OldPrice { get; init; }
    public decimal? UnitWeight { get; init; }
    public decimal? DiscountPercent { get; init; }
    public bool? ProductAvailable { get; init; }
    public string? MainImageURL { get; init; }
    public string? AltText { get; init; }
    public bool? AddBadge { get; init; }
    public string? ShortDescription { get; init; }
    public string? LongDescription { get; init; }
    public string? Picture1 { get; init; }
    public string? Picture2 { get; init; }
    public string? Picture3 { get; init; }
    public string? Picture4 { get; init; }
    public string? Note { get; init; }
    public List<UpdateProductSizeRequest>? Sizes { get; init; }
}

public class UpdateProductValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleForEach(x => x.Sizes).ChildRules(size =>
        {
            size.RuleFor(x => x.Size).NotEmpty();
        });
    }
}

public class UpdateProductHandler : IRequestHandler<UpdateProductRequest, UpdateProductResult>
{
    private readonly ICommandRepository<Product> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductHandler(
        ICommandRepository<Product> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateProductResult> Handle(UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository
            .GetQuery()
            .Include(x => x.Sizes)
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.Name = request.Name ?? string.Empty;
        entity.CategoryID = request.CategoryID;
        entity.UnitPrice = request.UnitPrice;
        entity.OldPrice = request.OldPrice;
        entity.UnitWeight = request.UnitWeight;
        entity.DiscountPercent = request.DiscountPercent;
        entity.ProductAvailable = request.ProductAvailable;
        entity.MainImageURL = request.MainImageURL;
        entity.AltText = request.AltText;
        entity.AddBadge = request.AddBadge;
        entity.ShortDescription = request.ShortDescription;
        entity.LongDescription = request.LongDescription;
        entity.Picture1 = request.Picture1;
        entity.Picture2 = request.Picture2;
        entity.Picture3 = request.Picture3;
        entity.Picture4 = request.Picture4;
        entity.Note = request.Note;

        entity.Sizes ??= new List<ProductSize>();
        entity.Sizes.Clear();

        foreach (var size in request.Sizes ?? [])
        {
            entity.Sizes.Add(new ProductSize
            {
                ProductId = entity.Id,
                Size = size.Size ?? string.Empty,
                Bust = size.Bust,
                Sleeve = size.Sleeve,
                Length = size.Length
            });
        }

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateProductResult
        {
            Data = entity
        };
    }
}
