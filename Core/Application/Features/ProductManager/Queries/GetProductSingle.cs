using Application.Common.CQS.Queries;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ProductManager.Queries;

public record GetProductSingleSizeDto
{
    public string? Id { get; init; }
    public string? Size { get; init; }
    public decimal? Bust { get; init; }
    public decimal? Sleeve { get; init; }
    public decimal? Length { get; init; }
}

public record GetProductSingleDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? CategoryID { get; init; }
    public decimal? UnitPrice { get; init; }
    public decimal? OldPrice { get; init; }
    public decimal? UnitWeight { get; init; }
    public decimal? DiscountPercent { get; init; }
    public bool? ProductAvailable { get; init; }
    public string? MainImageURL { get; init; }
    public string? AltText { get; init; }
    public bool? AddBadge { get; init; }
    public string? ShortDescription { get; init; }
    public string? LongDescription { get; init; }
    public string? Picture1 { get; init; }
    public string? Picture2 { get; init; }
    public string? Picture3 { get; init; }
    public string? Picture4 { get; init; }
    public string? Note { get; init; }
    public List<GetProductSingleSizeDto>? Sizes { get; init; }
}

public class GetProductSingleProfile : Profile
{
    public GetProductSingleProfile()
    {
        CreateMap<ProductSize, GetProductSingleSizeDto>();
        CreateMap<Product, GetProductSingleDto>();
    }
}

public class GetProductSingleResult
{
    public GetProductSingleDto? Data { get; init; }
}

public class GetProductSingleRequest : IRequest<GetProductSingleResult>
{
    public string? Id { get; init; }
}

public class GetProductSingleValidator : AbstractValidator<GetProductSingleRequest>
{
    public GetProductSingleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetProductSingleHandler : IRequestHandler<GetProductSingleRequest, GetProductSingleResult>
{
    private readonly IQueryContext _context;
    private readonly IMapper _mapper;

    public GetProductSingleHandler(
        IQueryContext context,
        IMapper mapper
        )
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<GetProductSingleResult> Handle(GetProductSingleRequest request, CancellationToken cancellationToken)
    {
        var entity = await _context
            .Product
            .AsNoTracking()
            .Include(x => x.Sizes)
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        var dto = _mapper.Map<GetProductSingleDto>(entity);

        return new GetProductSingleResult
        {
            Data = dto
        };
    }
}
