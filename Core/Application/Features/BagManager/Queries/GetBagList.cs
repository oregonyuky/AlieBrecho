using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.BagManager.Queries;

public record GetBagListDto
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
    public int ItemCount { get; init; }
    public string? Notes { get; init; }
}

public class GetBagListProfile : Profile
{
    public GetBagListProfile()
    {
        CreateMap<Bag, GetBagListDto>()
            .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.CustomerId))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.ItemCount, opt => opt.MapFrom(src => src.Items != null ? src.Items.Count : 0));
    }
}

public class GetBagListResult
{
    public List<GetBagListDto>? Data { get; init; }
}

public class GetBagListRequest : IRequest<GetBagListResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetBagListHandler : IRequestHandler<GetBagListRequest, GetBagListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetBagListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetBagListResult> Handle(GetBagListRequest request, CancellationToken cancellationToken)
    {
        var entities = await _context
            .Bag
            .AsNoTracking()
            .Include(x => x.Items)
            .ApplyIsDeletedFilter(request.IsDeleted)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetBagListDto>>(entities);

        var customerIds = entities
            .Select(x => x.CustomerId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct()
            .ToList();

        var customers = await _context.Customer
            .AsNoTracking()
            .Where(x => x.Id != null && customerIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Name })
            .ToListAsync(cancellationToken);

        var customerMap = customers
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .ToDictionary(x => x.Id!, x => x.Name);

        var data = dtos.Select(x => x with
        {
            CustomerName = !string.IsNullOrWhiteSpace(x.CustomerId) && customerMap.TryGetValue(x.CustomerId, out var customerName)
                ? customerName
                : null
        }).ToList();

        return new GetBagListResult
        {
            Data = data
        };
    }
}
