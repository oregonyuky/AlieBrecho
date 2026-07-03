using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.DropConfigManager.Commands;

public class DeleteDropConfigResult
{
    public DropConfig? Data { get; set; }
}

public class DeleteDropConfigRequest : IRequest<DeleteDropConfigResult>
{
    public string? Id { get; init; }
}

public class DeleteDropConfigValidator : AbstractValidator<DeleteDropConfigRequest>
{
    public DeleteDropConfigValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteDropConfigHandler : IRequestHandler<DeleteDropConfigRequest, DeleteDropConfigResult>
{
    private readonly ICommandRepository<DropConfig> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDropConfigHandler(
        ICommandRepository<DropConfig> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteDropConfigResult> Handle(
        DeleteDropConfigRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteDropConfigResult
        {
            Data = entity
        };
    }
}
