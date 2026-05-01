using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.CategoryManager.Commands;

public class DeleteCategoryResult
{
    public Category? Data { get; set; }
}

public class DeleteCategoryRequest : IRequest<DeleteCategoryResult>
{
    public string? Id { get; init; }
}

public class DeleteCategoryValidator : AbstractValidator<DeleteCategoryRequest>
{
    public DeleteCategoryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteCategoryHandler : IRequestHandler<DeleteCategoryRequest, DeleteCategoryResult>
{
    private readonly ICommandRepository<Category> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCategoryHandler(
        ICommandRepository<Category> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteCategoryResult> Handle(DeleteCategoryRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteCategoryResult
        {
            Data = entity
        };
    }
}
