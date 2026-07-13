using Application.Common.CQS.Queries;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.BagManager.Queries;

public record BagItemDto
{
    public string? Id { get; init; }
    public string? ProductId { get; init; }
    public string? ProductName { get; init; }
    public string? ProductImageUrl { get; init; }
    public int Quantity { get; init; }
    public decimal Price { get; init; }
    public decimal Weight { get; init; }
    public bool IsPaid { get; init; }
    public bool IsReserved { get; init; }
    public DateTime? ReservationExpiresAt { get; init; }
    public DateTime AddedAt { get; init; }
    public DateTime? PaidAt { get; init; }
}

public record GetBagSingleDto
{
    public string? Id { get; init; }
    public string? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public string? Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime ExpirationDate { get; init; }
    public DateTime LastInteractionAt { get; init; }
    public DateTime? ClosedAt { get; init; }
    public decimal TotalItemsValue { get; init; }
    public decimal? ShippingCost { get; init; }
    public decimal TotalWeight { get; init; }
    public bool AllItemsPaid { get; init; }
    public string? Notes { get; init; }
    public List<BagItemDto>? Items { get; init; }
    public List<BagExpirationHistoryDto>? ExpirationHistory { get; init; }
}

public record BagExpirationHistoryDto
{
    public string? Id { get; init; }
    public DateTime OldExpirationDate { get; init; }
    public DateTime NewExpirationDate { get; init; }
    public string? ChangedBy { get; init; }
    public DateTime ChangedAtUtc { get; init; }
    public string? Note { get; init; }
}

public class GetBagSingleProfile : Profile
{
    public GetBagSingleProfile()
    {
        CreateMap<BagItem, BagItemDto>()
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId));

        CreateMap<BagExpirationHistory, BagExpirationHistoryDto>();

        CreateMap<Bag, GetBagSingleDto>()
            .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.CustomerId))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
    }
}

public class GetBagSingleResult
{
    public GetBagSingleDto? Data { get; init; }
}

public class GetBagSingleRequest : IRequest<GetBagSingleResult>
{
    public string? Id { get; init; }
}

public class GetBagSingleHandler : IRequestHandler<GetBagSingleRequest, GetBagSingleResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetBagSingleHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetBagSingleResult> Handle(GetBagSingleRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Id))
        {
            return new GetBagSingleResult { Data = null };
        }

        var entity = await _context
            .Bag
            .AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.ExpirationHistory)
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            return new GetBagSingleResult { Data = null };
        }

        var dto = _mapper.Map<GetBagSingleDto>(entity);

        var customerName = await _context.Customer
            .AsNoTracking()
            .Where(x => x.Id == entity.CustomerId)
            .Select(x => x.Name)
            .SingleOrDefaultAsync(cancellationToken);

        var productIds = entity.Items?
            .Select(x => x.ProductId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct()
            .ToList() ?? [];

        var productImages = await _context.Product
            .AsNoTracking()
            .Where(x => x.Id != null && productIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                ProductImageUrl = x.MainImageURL ?? x.Picture1
            })
            .ToListAsync(cancellationToken);

        var productImageMap = productImages
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .ToDictionary(x => x.Id!, x => x.ProductImageUrl);

        var items = dto.Items?.Select(item => item with
        {
            ProductImageUrl = !string.IsNullOrWhiteSpace(item.ProductId)
                && productImageMap.TryGetValue(item.ProductId, out var productImageUrl)
                    ? productImageUrl
                    : null
        }).ToList();

        return new GetBagSingleResult
        {
            Data = dto with
            {
                CustomerName = customerName,
                Items = items,
                ExpirationHistory = dto.ExpirationHistory?
                    .OrderByDescending(x => x.ChangedAtUtc)
                    .ToList()
            }
        };
    }
}
