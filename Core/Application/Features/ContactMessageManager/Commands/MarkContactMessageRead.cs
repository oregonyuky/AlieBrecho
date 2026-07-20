using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.ContactMessageManager.Commands;

public sealed class MarkContactMessageReadResult
{
    public string? Id { get; init; }
    public bool IsRead { get; init; }
    public DateTime? ReadAtUtc { get; init; }
}

public sealed class MarkContactMessageReadRequest : IRequest<MarkContactMessageReadResult>
{
    public string? Id { get; init; }
}

public sealed class MarkContactMessageReadValidator : AbstractValidator<MarkContactMessageReadRequest>
{
    public MarkContactMessageReadValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class MarkContactMessageReadHandler
    : IRequestHandler<MarkContactMessageReadRequest, MarkContactMessageReadResult>
{
    private readonly ICommandRepository<ContactMessage> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkContactMessageReadHandler(
        ICommandRepository<ContactMessage> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MarkContactMessageReadResult> Handle(
        MarkContactMessageReadRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken)
            ?? throw new KeyNotFoundException($"Mensagem não encontrada: {request.Id}");

        if (!entity.IsRead)
        {
            entity.IsRead = true;
            entity.ReadAtUtc = DateTime.UtcNow;
            entity.UpdatedAtUtc = entity.ReadAtUtc;
            _repository.Update(entity);
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        return new MarkContactMessageReadResult
        {
            Id = entity.Id,
            IsRead = entity.IsRead,
            ReadAtUtc = entity.ReadAtUtc
        };
    }
}
