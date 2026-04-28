using Application.Common.CQS.Queries;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CategoryManager.Queries;

public record GetCategorySingleDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}
public class GetCategorySingleProfile : Profile
{
    public GetCategorySingleProfile()
    {
        CreateMap<Category, GetCategorySingleDto>();
    }
}
public class GetCategorySingleResult
{
    public GetCategorySingleDto? Data { get; init; }
}
public class GetCategorySingleRequest : IRequest<GetCategorySingleResult>
{
    public string? Id { get; init; }
}
public class GetCategorySingleValidator : AbstractValidator<GetCategorySingleRequest>
{
    public GetCategorySingleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
public class GetCategorySingleHandler : IRequestHandler<GetCategorySingleRequest, GetCategorySingleResult>
{
    private readonly IQueryContext _context;
    private readonly IMapper _mapper;

    public GetCategorySingleHandler(
        IQueryContext context,
        IMapper mapper
        )
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<GetCategorySingleResult> Handle(GetCategorySingleRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Category
            .AsNoTracking()
            .AsQueryable();

        query = query
            .Where(x => x.Id == request.Id);

        var entity = await query.SingleOrDefaultAsync(cancellationToken);

        var dto = _mapper.Map<GetCategorySingleDto>(entity);

        return new GetCategorySingleResult
        {
            Data = dto
        };
    }
}