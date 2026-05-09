using Application.Common.CQS.Queries;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ShippingBoxManager.Queries;

public record GetShippingBoxSingleDto
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

public class GetShippingBoxSingleProfile : Profile
{
    public GetShippingBoxSingleProfile()
    {
        CreateMap<ShippingBox, GetShippingBoxSingleDto>();
    }
}

public class GetShippingBoxSingleResult
{
    public GetShippingBoxSingleDto? Data { get; init; }
}

public class GetShippingBoxSingleRequest : IRequest<GetShippingBoxSingleResult>
{
    public string? Id { get; init; }
}

public class GetShippingBoxSingleValidator : AbstractValidator<GetShippingBoxSingleRequest>
{
    public GetShippingBoxSingleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetShippingBoxSingleHandler : IRequestHandler<GetShippingBoxSingleRequest, GetShippingBoxSingleResult>
{
    private readonly IQueryContext _context;
    private readonly IMapper _mapper;

    public GetShippingBoxSingleHandler(
        IQueryContext context,
        IMapper mapper
    )
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<GetShippingBoxSingleResult> Handle(GetShippingBoxSingleRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .ShippingBox
            .AsNoTracking()
            .AsQueryable();

        query = query
            .Where(x => x.Id == request.Id);

        var entity = await query.SingleOrDefaultAsync(cancellationToken);

        var dto = _mapper.Map<GetShippingBoxSingleDto>(entity);

        return new GetShippingBoxSingleResult
        {
            Data = dto
        };
    }
}