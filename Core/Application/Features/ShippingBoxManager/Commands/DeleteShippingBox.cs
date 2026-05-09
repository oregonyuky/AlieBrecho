using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.ShippingBoxManager.Commands;

public class DeleteShippingBoxResult
{
    public ShippingBox? Data { get; set; }
}

public class DeleteShippingBoxRequest : IRequest<DeleteShippingBoxResult>
{
    public string? Id { get; init; }
}

public class DeleteShippingBoxValidator : AbstractValidator<DeleteShippingBoxRequest>
{
    public DeleteShippingBoxValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteShippingBoxHandler : IRequestHandler<DeleteShippingBoxRequest, DeleteShippingBoxResult>
{
    private readonly ICommandRepository<ShippingBox> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteShippingBoxHandler(
        ICommandRepository<ShippingBox> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteShippingBoxResult> Handle(DeleteShippingBoxRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteShippingBoxResult
        {
            Data = entity
        };
    }
}