using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PackageCategoryManager.Queries;

public sealed class PackageCategoryDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int CapacityPoints { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class GetPackageCategoryListResult { public List<PackageCategoryDto> Data { get; init; } = []; }
public sealed class GetPackageCategoryListRequest : IRequest<GetPackageCategoryListResult>
{
    public bool IsDeleted { get; init; }
    public bool ActiveOnly { get; init; }
}

public sealed class GetPackageCategoryListHandler(IQueryContext context)
    : IRequestHandler<GetPackageCategoryListRequest, GetPackageCategoryListResult>
{
    public async Task<GetPackageCategoryListResult> Handle(GetPackageCategoryListRequest request, CancellationToken cancellationToken)
    {
        var query = context.PackageCategory.AsNoTracking().ApplyIsDeletedFilter(request.IsDeleted);
        if (request.ActiveOnly) query = query.Where(x => x.IsActive);
        var data = await query.OrderBy(x => x.CapacityPoints).ThenBy(x => x.Name)
            .Select(x => new PackageCategoryDto
            {
                Id = x.Id, Name = x.Name, CapacityPoints = x.CapacityPoints,
                Description = x.Description, IsActive = x.IsActive, CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
        return new GetPackageCategoryListResult { Data = data };
    }
}

public sealed class GetPackageCategorySingleRequest : IRequest<PackageCategoryDto?> { public string? Id { get; init; } }
public sealed class GetPackageCategorySingleValidator : FluentValidation.AbstractValidator<GetPackageCategorySingleRequest>
{
    public GetPackageCategorySingleValidator() => RuleFor(x => x.Id).NotEmpty();
}
public sealed class GetPackageCategorySingleHandler(IQueryContext context)
    : IRequestHandler<GetPackageCategorySingleRequest, PackageCategoryDto?>
{
    public Task<PackageCategoryDto?> Handle(GetPackageCategorySingleRequest request, CancellationToken cancellationToken) =>
        context.PackageCategory.AsNoTracking().Where(x => x.Id == request.Id && !x.IsDeleted)
            .Select(x => new PackageCategoryDto
            {
                Id = x.Id, Name = x.Name, CapacityPoints = x.CapacityPoints,
                Description = x.Description, IsActive = x.IsActive, CreatedAt = x.CreatedAt
            })
            .SingleOrDefaultAsync(cancellationToken);
}
