using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
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
}

public record GetProductListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? CategoryID { get; init; }
    public decimal? UnitPrice { get; init; }
    public decimal? OldPrice { get; init; }
    public decimal? DiscountPercent { get; init; }
    public bool? ProductAvailable { get; init; }
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
        CreateMap<Product, GetProductListDto>();
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
            .Include(x => x.Sizes)
            .ApplyIsDeletedFilter(request.IsDeleted)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetProductListDto>>(entities);

        return new GetProductListResult
        {
            Data = dtos
        };
    }
}
