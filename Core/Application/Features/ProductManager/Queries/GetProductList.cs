using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ProductManager.Queries;

public record GetProductListSizeDto
{
    public string? Id { get; init; }
    public string? Size { get; init; }
    public decimal? Bust { get; init; }
    public decimal? Sleeve { get; init; }
    public decimal? Length { get; init; }
    public int? StockQuantity { get; init; }
}

public record GetProductListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? CategoryID { get; init; }
    public string? DropConfigId { get; init; }
    public string? DropTitulo { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
    public decimal? UnitPrice { get; init; }
    public decimal? OldPrice { get; init; }
    public decimal? DiscountPercent { get; init; }
    public bool? ProductAvailable { get; init; }
    public bool IsSold { get; set; }
    public int SoldQuantity { get; set; }
    public string? MainImageURL { get; init; }
    public string? AltText { get; init; }
    public string? ShortDescription { get; init; }
    public List<GetProductListSizeDto>? Sizes { get; init; }
}

public class GetProductListProfile : Profile
{
    public GetProductListProfile()
    {
        CreateMap<ProductSize, GetProductListSizeDto>();
        CreateMap<Product, GetProductListDto>()
            .ForMember(dest => dest.DropTitulo, opt => opt.MapFrom(src => src.DropConfig == null ? null : src.DropConfig.Titulo));
    }
}

public class GetProductListResult
{
    public List<GetProductListDto>? Data { get; init; }
}

public class GetProductListRequest : IRequest<GetProductListResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetProductListHandler : IRequestHandler<GetProductListRequest, GetProductListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetProductListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetProductListResult> Handle(GetProductListRequest request, CancellationToken cancellationToken)
    {
        var entities = await _context
            .Product
            .AsNoTracking()
            .Include(x => x.DropConfig)
            .Include(x => x.Sizes)
            .ApplyIsDeletedFilter(request.IsDeleted)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetProductListDto>>(entities);
        var productIds = entities.Select(x => x.Id).ToList();

        var orderSoldQuantities = await _context
            .OrderDetail
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted
                && x.ProductId != null
                && productIds.Contains(x.ProductId)
                && x.Order != null
                && x.Order.Status != OrderStatus.Pending
                && x.Order.Status != OrderStatus.Cancelled)
            .GroupBy(x => x.ProductId!)
            .Select(x => new
            {
                ProductId = x.Key,
                Quantity = x.Sum(item => item.Quantity)
            })
            .ToDictionaryAsync(x => x.ProductId, x => x.Quantity, cancellationToken);

        var bagSoldQuantities = await _context
            .BagItem
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted
                && x.ProductId != null
                && productIds.Contains(x.ProductId)
                && x.IsPaid)
            .GroupBy(x => x.ProductId!)
            .Select(x => new
            {
                ProductId = x.Key,
                Quantity = x.Sum(item => item.Quantity)
            })
            .ToDictionaryAsync(x => x.ProductId, x => x.Quantity, cancellationToken);

        foreach (var dto in dtos)
        {
            if (string.IsNullOrWhiteSpace(dto.Id))
            {
                continue;
            }

            var soldQuantity = 0;
            if (orderSoldQuantities.TryGetValue(dto.Id, out var orderQuantity))
            {
                soldQuantity += orderQuantity;
            }

            if (bagSoldQuantities.TryGetValue(dto.Id, out var bagQuantity))
            {
                soldQuantity += bagQuantity;
            }

            dto.SoldQuantity = soldQuantity;
            dto.IsSold = soldQuantity > 0;
        }

        return new GetProductListResult
        {
            Data = dtos
        };
    }
}
