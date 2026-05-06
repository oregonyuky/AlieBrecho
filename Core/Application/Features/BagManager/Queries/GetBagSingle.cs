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
}

public class GetBagSingleProfile : Profile
{
    public GetBagSingleProfile()
    {
        CreateMap<BagItem, BagItemDto>()
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId));

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

        return new GetBagSingleResult
        {
            Data = dto with
            {
                CustomerName = customerName
            }
        };
    }
}
