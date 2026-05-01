using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.CategoryManager.Commands;

public class CreateCategoryResult
{
    public Category? Data { get; set; }
}

public class CreateCategoryRequest : IRequest<CreateCategoryResult>
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public bool? IsActive { get; init; }
}

public class CreateCategoryValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class CreateCategoryHandler : IRequestHandler<CreateCategoryRequest, CreateCategoryResult>
{
    private readonly ICommandRepository<Category> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCategoryHandler(
        ICommandRepository<Category> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateCategoryResult> Handle(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var entity = new Category
        {
            Name = request.Name ?? string.Empty,
            Description = request.Description,
            IsActive = request.IsActive ?? true,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateCategoryResult
        {
            Data = entity
        };
    }
}
