using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.CategoryManager.Commands;

public class UpdateCategoryResult
{
    public Category? Data { get; set; }
}

public class UpdateCategoryRequest : IRequest<UpdateCategoryResult>
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public bool? IsActive { get; init; }
}
public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}
public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryRequest, UpdateCategoryResult>
{
    private readonly ICommandRepository<Category> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCategoryHandler(
        ICommandRepository<Category> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateCategoryResult> Handle(UpdateCategoryRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.Name = request.Name;
        entity.Description = request.Description;

        if (request.IsActive.HasValue)
        {
            entity.IsActive = request.IsActive.Value;
        }

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateCategoryResult
        {
            Data = entity
        };
    }
}