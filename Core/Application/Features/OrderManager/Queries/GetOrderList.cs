using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.OrderManager.Queries;

public record GetOrderListDto
{
    public string? Id { get; init; }
    public string? CustomerName { get; init; }
    public string? Status { get; init; }
    public decimal? TotalAmount { get; init; }
    public decimal? Discount { get; init; }
    public decimal? Taxes { get; init; }
    public string? PaymentStatus { get; init; }
    public string? PaymentTypeName { get; init; }
    public string? ShippingCity { get; init; }
    public string? ShippingState { get; init; }
    public DateTime OrderDate { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class GetOrderListProfile : Profile
{
    public GetOrderListProfile()
    {
        CreateMap<Order, GetOrderListDto>()
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Name : null))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => src.Payment != null && src.Payment.Status != null ? src.Payment.Status.ToString() : null))
            .ForMember(dest => dest.PaymentTypeName, opt => opt.MapFrom(src => src.Payment != null && src.Payment.PaymentType != null ? src.Payment.PaymentType.TypeName : null))
            .ForMember(dest => dest.ShippingCity, opt => opt.MapFrom(src => src.ShippingDetail != null ? src.ShippingDetail.City : null))
            .ForMember(dest => dest.ShippingState, opt => opt.MapFrom(src => src.ShippingDetail != null ? src.ShippingDetail.State : null));
    }
}

public class GetOrderListResult
{
    public List<GetOrderListDto>? Data { get; init; }
}

public class GetOrderListRequest : IRequest<GetOrderListResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetOrderListHandler : IRequestHandler<GetOrderListRequest, GetOrderListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetOrderListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetOrderListResult> Handle(GetOrderListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Order
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Payment)
                .ThenInclude(x => x!.PaymentType)
            .Include(x => x.ShippingDetail)
            .ApplyIsDeletedFilter(request.IsDeleted)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);
        var dtos = _mapper.Map<List<GetOrderListDto>>(entities);

        return new GetOrderListResult
        {
            Data = dtos
        };
    }
}
