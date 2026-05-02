using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.ProductManager.Commands;

public class CreateProductResult
{
    public Product? Data { get; set; }
}

public class CreateProductSizeRequest
{
    public string? Size { get; init; }
    public decimal? Bust { get; init; }
    public decimal? Sleeve { get; init; }
    public decimal? Length { get; init; }
}

public class CreateProductRequest : IRequest<CreateProductResult>
{
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
    public List<CreateProductSizeRequest>? Sizes { get; init; }
}

public class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleForEach(x => x.Sizes).ChildRules(size =>
        {
            size.RuleFor(x => x.Size).NotEmpty();
        });
    }
}

public class CreateProductHandler : IRequestHandler<CreateProductRequest, CreateProductResult>
{
    private readonly ICommandRepository<Product> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductHandler(
        ICommandRepository<Product> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateProductResult> Handle(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var entity = new Product
        {
            Name = request.Name ?? string.Empty,
            CategoryID = request.CategoryID,
            UnitPrice = request.UnitPrice,
            OldPrice = request.OldPrice,
            UnitWeight = request.UnitWeight,
            DiscountPercent = request.DiscountPercent,
            ProductAvailable = request.ProductAvailable,
            MainImageURL = request.MainImageURL,
            AltText = request.AltText,
            AddBadge = request.AddBadge,
            ShortDescription = request.ShortDescription,
            LongDescription = request.LongDescription,
            Picture1 = request.Picture1,
            Picture2 = request.Picture2,
            Picture3 = request.Picture3,
            Picture4 = request.Picture4,
            Note = request.Note,
            Sizes = new List<ProductSize>()
        };

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

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateProductResult
        {
            Data = entity
        };
    }
}
