using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PackageCategoryManager.Commands;

public sealed class SavePackageCategoryResult { public PackageCategory? Data { get; init; } }

public sealed class CreatePackageCategoryRequest : IRequest<SavePackageCategoryResult>
{
    public string? Name { get; init; }
    public int CapacityPoints { get; init; }
    public string? Description { get; init; }
    public bool? IsActive { get; init; }
}

public sealed class CreatePackageCategoryValidator : AbstractValidator<CreatePackageCategoryRequest>
{
    public CreatePackageCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.CapacityPoints).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class CreatePackageCategoryHandler(
    ICommandRepository<PackageCategory> repository,
    IUnitOfWork unitOfWork) : IRequestHandler<CreatePackageCategoryRequest, SavePackageCategoryResult>
{
    public async Task<SavePackageCategoryResult> Handle(CreatePackageCategoryRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name!.Trim();
        if (repository.GetQuery().Any(x => !x.IsDeleted && x.Name.ToLower() == name.ToLower()))
            throw new ValidationException("Ja existe uma categoria de embalagem com este nome.");

        var entity = new PackageCategory
        {
            Name = name,
            CapacityPoints = request.CapacityPoints,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsActive = request.IsActive ?? true,
            CreatedAt = DateTime.UtcNow
        };
        await repository.CreateAsync(entity, cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);
        return new SavePackageCategoryResult { Data = entity };
    }
}

public sealed class UpdatePackageCategoryRequest : IRequest<SavePackageCategoryResult>
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public int CapacityPoints { get; init; }
    public string? Description { get; init; }
    public bool? IsActive { get; init; }
}

public sealed class UpdatePackageCategoryValidator : AbstractValidator<UpdatePackageCategoryRequest>
{
    public UpdatePackageCategoryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.CapacityPoints).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class UpdatePackageCategoryHandler(
    ICommandRepository<PackageCategory> repository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdatePackageCategoryRequest, SavePackageCategoryResult>
{
    public async Task<SavePackageCategoryResult> Handle(UpdatePackageCategoryRequest request, CancellationToken cancellationToken)
    {
        var entity = await repository.GetAsync(request.Id!, cancellationToken)
            ?? throw new Exception($"Entity not found: {request.Id}");
        var name = request.Name!.Trim();
        if (repository.GetQuery().Any(x => !x.IsDeleted && x.Id != entity.Id && x.Name.ToLower() == name.ToLower()))
            throw new ValidationException("Ja existe uma categoria de embalagem com este nome.");

        entity.Name = name;
        entity.CapacityPoints = request.CapacityPoints;
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
        repository.Update(entity);
        await unitOfWork.SaveAsync(cancellationToken);
        return new SavePackageCategoryResult { Data = entity };
    }
}

public sealed class DeactivatePackageCategoryRequest : IRequest<SavePackageCategoryResult>
{
    public string? Id { get; init; }
}

public sealed class DeactivatePackageCategoryValidator : AbstractValidator<DeactivatePackageCategoryRequest>
{
    public DeactivatePackageCategoryValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class DeactivatePackageCategoryHandler(
    ICommandRepository<PackageCategory> repository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeactivatePackageCategoryRequest, SavePackageCategoryResult>
{
    public async Task<SavePackageCategoryResult> Handle(DeactivatePackageCategoryRequest request, CancellationToken cancellationToken)
    {
        var entity = await repository.GetAsync(request.Id!, cancellationToken)
            ?? throw new Exception($"Entity not found: {request.Id}");
        entity.IsActive = false;
        repository.Update(entity);
        await unitOfWork.SaveAsync(cancellationToken);
        return new SavePackageCategoryResult { Data = entity };
    }
}
