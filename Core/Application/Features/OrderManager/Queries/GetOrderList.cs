using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Services;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.OrderManager.Queries;

public record GetOrderListDto
{
    public string? Id { get; init; }
    public string? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public string? Status { get; init; }
    public decimal? TotalAmount { get; init; }
    public decimal? TotalWithShipping { get; init; }
    public decimal? Discount { get; init; }
    public decimal? Taxes { get; init; }
    public decimal? ShippingCost { get; init; }
    public string? ShippingBoxData { get; init; }
    public decimal? ShippingBoxWidth { get; init; }
    public decimal? ShippingBoxLength { get; init; }
    public decimal? ShippingBoxHeight { get; init; }
    public decimal? ShippingBoxWeight { get; init; }
    public string? PaymentStatus { get; init; }
    public string? PaymentTypeName { get; init; }
    public string? ShippingRecipientName { get; init; }
    public string? ShippingCity { get; init; }
    public string? ShippingState { get; init; }
    public string? ShippingPostCode { get; init; }
    public string? MelhorEnvioCartId { get; init; }
    public DateTime? MelhorEnvioCartAddedAt { get; init; }
    public DateTime? MelhorEnvioCheckoutAt { get; init; }
    public DateTime? MelhorEnvioGeneratedAt { get; init; }
    public bool IsMelhorEnvioCartAdded { get; init; }
    public bool IsMelhorEnvioCheckedOut { get; init; }
    public bool IsMelhorEnvioGenerated { get; init; }
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
            .ForMember(dest => dest.ShippingCost, opt => opt.MapFrom(src => ShippingCostCalculator.Calculate(src.ShippingBox)))
            .ForMember(dest => dest.TotalWithShipping, opt => opt.MapFrom(src => (src.TotalAmount ?? 0m) + ShippingCostCalculator.Calculate(src.ShippingBox)))
            .ForMember(dest => dest.ShippingBoxData, opt => opt.MapFrom(src => FormatShippingBoxData(src.ShippingBox)))
            .ForMember(dest => dest.ShippingBoxWidth, opt => opt.MapFrom(src => src.ShippingBox != null ? src.ShippingBox.Width : null))
            .ForMember(dest => dest.ShippingBoxLength, opt => opt.MapFrom(src => src.ShippingBox != null ? src.ShippingBox.Length : null))
            .ForMember(dest => dest.ShippingBoxHeight, opt => opt.MapFrom(src => src.ShippingBox != null ? src.ShippingBox.Height : null))
            .ForMember(dest => dest.ShippingBoxWeight, opt => opt.MapFrom(src => src.ShippingBox != null ? src.ShippingBox.Weight : null))
            .ForMember(dest => dest.ShippingRecipientName, opt => opt.MapFrom(src => src.ShippingDetail != null ? $"{src.ShippingDetail.FirstName} {src.ShippingDetail.LastName}".Trim() : null))
            .ForMember(dest => dest.ShippingCity, opt => opt.MapFrom(src => src.ShippingDetail != null ? src.ShippingDetail.City : null))
            .ForMember(dest => dest.ShippingState, opt => opt.MapFrom(src => src.ShippingDetail != null ? src.ShippingDetail.State : null))
            .ForMember(dest => dest.ShippingPostCode, opt => opt.MapFrom(src => src.ShippingDetail != null ? src.ShippingDetail.PostCode : null))
            .ForMember(dest => dest.IsMelhorEnvioCartAdded, opt => opt.MapFrom(src => !string.IsNullOrWhiteSpace(src.MelhorEnvioCartId)))
            .ForMember(dest => dest.IsMelhorEnvioCheckedOut, opt => opt.MapFrom(src => src.MelhorEnvioCheckoutAt != null))
            .ForMember(dest => dest.IsMelhorEnvioGenerated, opt => opt.MapFrom(src => src.MelhorEnvioGeneratedAt != null));
    }

    private static string? FormatShippingBoxData(ShippingBox? shippingBox)
    {
        if (shippingBox == null)
        {
            return null;
        }

        return $"{shippingBox.Width:0.##}cm x {shippingBox.Length:0.##}cm x {shippingBox.Height:0.##}cm {shippingBox.Weight:0.##}kg";
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
    private readonly IShippingCostService _shippingCostService;

    public GetOrderListHandler(
        IMapper mapper,
        IQueryContext context,
        IShippingCostService shippingCostService)
    {
        _mapper = mapper;
        _context = context;
        _shippingCostService = shippingCostService;
    }

    public async Task<GetOrderListResult> Handle(GetOrderListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Order
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Payment)
                .ThenInclude(x => x!.PaymentType)
            .Include(x => x.ShippingBox)
            .Include(x => x.ShippingDetail)
            .ApplyIsDeletedFilter(request.IsDeleted)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);
        var dtos = _mapper.Map<List<GetOrderListDto>>(entities);

        for (var i = 0; i < entities.Count; i++)
        {
            var shippingCost = await _shippingCostService.CalculateAsync(
                entities[i].ShippingBox,
                entities[i].ShippingDetail?.PostCode,
                cancellationToken);

            dtos[i] = dtos[i] with
            {
                ShippingCost = shippingCost,
                TotalWithShipping = (entities[i].TotalAmount ?? 0m) + shippingCost
            };
        }

        return new GetOrderListResult
        {
            Data = dtos
        };
    }
}
