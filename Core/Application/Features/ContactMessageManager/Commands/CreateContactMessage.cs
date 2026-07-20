using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.ContactMessageManager.Commands;

public sealed class CreateContactMessageResult
{
    public string? Id { get; init; }
}

public sealed class CreateContactMessageRequest : IRequest<CreateContactMessageResult>
{
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Subject { get; init; }
    public string? Message { get; init; }
}

public sealed class CreateContactMessageValidator : AbstractValidator<CreateContactMessageRequest>
{
    public CreateContactMessageValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(180);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Message).NotEmpty().MinimumLength(10).MaximumLength(4000);
    }
}

public sealed class CreateContactMessageHandler
    : IRequestHandler<CreateContactMessageRequest, CreateContactMessageResult>
{
    private readonly ICommandRepository<ContactMessage> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateContactMessageHandler(
        ICommandRepository<ContactMessage> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateContactMessageResult> Handle(
        CreateContactMessageRequest request,
        CancellationToken cancellationToken)
    {
        var receivedAt = DateTime.UtcNow;
        var entity = new ContactMessage
        {
            Name = request.Name?.Trim() ?? string.Empty,
            Email = request.Email?.Trim() ?? string.Empty,
            Phone = request.Phone?.Trim() ?? string.Empty,
            Subject = request.Subject?.Trim() ?? string.Empty,
            Message = request.Message?.Trim() ?? string.Empty,
            IsRead = false,
            ReceivedAtUtc = receivedAt,
            CreatedAtUtc = receivedAt
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateContactMessageResult { Id = entity.Id };
    }
}
