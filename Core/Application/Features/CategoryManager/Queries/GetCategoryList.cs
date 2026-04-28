using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CategoryManager.Queries;

public record GetCategoryListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}
public class GetCategoryListProfile : Profile
{
    public GetCategoryListProfile()
    {
        CreateMap<Category, GetCategoryListDto>();
    }
}
public class GetCategoryListResult
{
    public List<GetCategoryListDto>? Data { get; init; }
}
public class GetCategoryListRequest : IRequest<GetCategoryListResult>
{
    public bool IsDeleted { get; init; } = false;
}
public class GetCategoryListHandler : IRequestHandler<GetCategoryListRequest, GetCategoryListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetCategoryListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetCategoryListResult> Handle(GetCategoryListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Category
            .AsNoTracking()
            .ApplyIsDeletedFilter(request.IsDeleted)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetCategoryListDto>>(entities);

        return new GetCategoryListResult
        {
            Data = dtos
        };
    }
}