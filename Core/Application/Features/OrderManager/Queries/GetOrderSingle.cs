using Application.Common.CQS.Queries;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.OrderManager.Queries;

public record PaymentDetailDto
{
    public string? PaymentMethod { get; init; }
    public string? TransactionId { get; init; }
    public string? AuthorizationCode { get; init; }
    public string? ReferenceNumber { get; init; }
    public string? CardHolderName { get; init; }
    public string? CardLast4 { get; init; }
    public string? CardBrand { get; init; }
    public DateTime? CapturedAt { get; init; }
    public string? Notes { get; init; }
}

public record PaymentDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Status { get; init; }
    public DateTime? PaymentDateTime { get; init; }
    public decimal? Amount { get; init; }
    public string? PaymentTypeId { get; init; }
    public string? PaymentTypeName { get; init; }
    public PaymentDetailDto? PaymentDetail { get; init; }
}

public record ShippingDetailDto
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Street { get; init; }
    public string? Number { get; init; }
    public string? Neighborhood { get; init; }
    public string? Complement { get; init; }
    public string? State { get; init; }
    public string? City { get; init; }
    public string? PostCode { get; init; }
}

public record OrderDetailDto
{
    public string? ProductId { get; init; }
    public string? ProductName { get; init; }
    public string? ProductImageUrl { get; init; }
    public int Quantity { get; init; }
    public decimal? UnitPrice { get; init; }
    public decimal? TotalPrice { get; init; }
}

public record GetOrderSingleDto
{
    public string? Id { get; init; }
    public string? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public string? Status { get; init; }
    public decimal? Discount { get; init; }
    public decimal? Taxes { get; init; }
    public decimal? TotalAmount { get; init; }
    public decimal? ShippingCost { get; init; }
    public decimal? Height { get; init; }
    public decimal? Width { get; init; }
    public decimal? Length { get; init; }
    public decimal? Weight { get; init; }
    public decimal? InsuranceCost { get; init; }
    public string? Notes { get; init; }
    public DateTime OrderDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public PaymentDto? Payment { get; init; }
    public ShippingDetailDto? ShippingDetail { get; init; }
    public List<OrderDetailDto>? OrderDetails { get; init; }
}

public class GetOrderSingleProfile : Profile
{
    public GetOrderSingleProfile()
    {
        CreateMap<PaymentDetail, PaymentDetailDto>();
        CreateMap<Payment, PaymentDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status != null ? src.Status.ToString() : null))
            .ForMember(dest => dest.PaymentTypeName, opt => opt.MapFrom(src => src.PaymentType != null ? src.PaymentType.TypeName : null));

        CreateMap<ShippingDetail, ShippingDetailDto>();
        CreateMap<OrderDetail, OrderDetailDto>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : null))
            .ForMember(dest => dest.ProductImageUrl, opt => opt.MapFrom(src => src.Product != null ? src.Product.MainImageURL ?? src.Product.Picture1 : null));

        CreateMap<Order, GetOrderSingleDto>()
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Name : null))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.InsuranceCost, opt => opt.MapFrom(src => src.Insurance_cost));
    }
}

public class GetOrderSingleResult
{
    public GetOrderSingleDto? Data { get; init; }
}

public class GetOrderSingleRequest : IRequest<GetOrderSingleResult>
{
    public string? Id { get; init; }
}

public class GetOrderSingleHandler : IRequestHandler<GetOrderSingleRequest, GetOrderSingleResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetOrderSingleHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetOrderSingleResult> Handle(GetOrderSingleRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Id))
        {
            return new GetOrderSingleResult { Data = null };
        }

        var entity = await _context
            .Order
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Payment)
                .ThenInclude(x => x!.PaymentType)
            .Include(x => x.Payment)
                .ThenInclude(x => x!.PaymentDetail)
            .Include(x => x.ShippingDetail)
            .Include(x => x.OrderDetails)
                .ThenInclude(x => x.Product)
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        var dto = entity == null ? null : _mapper.Map<GetOrderSingleDto>(entity);

        return new GetOrderSingleResult
        {
            Data = dto
        };
    }
}
