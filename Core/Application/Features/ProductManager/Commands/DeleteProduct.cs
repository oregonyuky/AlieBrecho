using Application.Common.Repositories;
using Application.Common.Services.FileImageManager;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.ProductManager.Commands;

public class DeleteProductResult
{
    public Product? Data { get; set; }
}

public class DeleteProductRequest : IRequest<DeleteProductResult>
{
    public string? Id { get; init; }
}

public class DeleteProductValidator : AbstractValidator<DeleteProductRequest>
{
    public DeleteProductValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteProductHandler : IRequestHandler<DeleteProductRequest, DeleteProductResult>
{
    private readonly ICommandRepository<Product> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileImageService _fileImageService;

    public DeleteProductHandler(
        ICommandRepository<Product> repository,
        IUnitOfWork unitOfWork,
        IFileImageService fileImageService
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _fileImageService = fileImageService;
    }

    public async Task<DeleteProductResult> Handle(DeleteProductRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        var imageNames = new[]
        {
            entity.MainImageURL,
            entity.Picture1,
            entity.Picture2,
            entity.Picture3,
            entity.Picture4
        }
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => x!.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var imageName in imageNames)
        {
            await _fileImageService.DeleteAsync(imageName, cancellationToken);
        }

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteProductResult
        {
            Data = entity
        };
    }
}
