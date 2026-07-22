using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ShippingBoxManager.Queries;

public record GetShippingBoxListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public decimal? Width { get; init; }
    public decimal? Length { get; init; }
    public decimal? Height { get; init; }
    public decimal? Weight { get; init; }
    public decimal? InsuranceValue { get; init; }
    public bool IsActive { get; init; }
    public bool IsInUse { get; set; }
    public string? PackageCategoryId { get; init; }
    public string? PackageCategoryName { get; init; }
    public int CapacityPoints { get; init; }
    public int StockQuantity { get; init; }
    public decimal? MaxWeight { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class GetShippingBoxListProfile : Profile
{
    public GetShippingBoxListProfile()
    {
        CreateMap<ShippingBox, GetShippingBoxListDto>();
    }
}

public class GetShippingBoxListResult
{
    public List<GetShippingBoxListDto>? Data { get; init; }
}

public class GetShippingBoxListRequest : IRequest<GetShippingBoxListResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetShippingBoxListHandler : IRequestHandler<GetShippingBoxListRequest, GetShippingBoxListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetShippingBoxListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetShippingBoxListResult> Handle(GetShippingBoxListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .ShippingBox
            .AsNoTracking()
            .ApplyIsDeletedFilter(request.IsDeleted)
            .AsQueryable();

        var entities = await query.Include(x => x.PackageCategory).ToListAsync(cancellationToken);
        var dtos = _mapper.Map<List<GetShippingBoxListDto>>(entities);
        for (var index = 0; index < dtos.Count; index++)
        {
            dtos[index] = dtos[index] with
            {
                PackageCategoryName = entities[index].PackageCategory?.Name,
                CapacityPoints = entities[index].PackageCategory?.CapacityPoints ?? 0
            };
        }

        var shippingBoxIdsInUse = await _context.Order
            .AsNoTracking()
            .Where(x => !x.IsDeleted &&
                        x.ShippingBoxId != null &&
                        x.Status != OrderStatus.Delivered &&
                        x.Status != OrderStatus.Cancelled)
            .Select(x => x.ShippingBoxId!)
            .Distinct()
            .ToListAsync(cancellationToken);
        var shippingBoxesInUse = shippingBoxIdsInUse.ToHashSet();

        foreach (var dto in dtos)
        {
            dto.IsInUse = dto.Id != null && shippingBoxesInUse.Contains(dto.Id);
        }

        return new GetShippingBoxListResult
        {
            Data = dtos
        };
    }
}
