using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ShippingBoxManager.Queries;

public record GetShippingBoxListDto
{
    public string? Id { get; init; }
    public decimal? Width { get; init; }
    public decimal? Length { get; init; }
    public decimal? Height { get; init; }
    public decimal? Weight { get; init; }
    public decimal? InsuranceValue { get; init; }
    public bool IsActive { get; init; }
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

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetShippingBoxListDto>>(entities);

        return new GetShippingBoxListResult
        {
            Data = dtos
        };
    }
}