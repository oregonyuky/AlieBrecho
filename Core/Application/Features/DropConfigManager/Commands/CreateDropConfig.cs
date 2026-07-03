using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Common.Time;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.DropConfigManager.Commands;

public class CreateDropConfigResult
{
    public DropConfig? Data { get; set; }
}

public class CreateDropConfigRequest : IRequest<CreateDropConfigResult>
{
    public string? Titulo { get; init; }
    public string? Subtitulo { get; init; }
    public DateTime DataLiberacaoBrasilia { get; init; }
    public bool Ativo { get; init; }
}

public class CreateDropConfigValidator : AbstractValidator<CreateDropConfigRequest>
{
    public CreateDropConfigValidator()
    {
        RuleFor(x => x.Titulo).NotEmpty();
        RuleFor(x => x.DataLiberacaoBrasilia).NotEmpty();
    }
}

public class CreateDropConfigHandler : IRequestHandler<CreateDropConfigRequest, CreateDropConfigResult>
{
    private readonly ICommandRepository<DropConfig> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBrazilTimeZoneConverter _timeZoneConverter;

    public CreateDropConfigHandler(
        ICommandRepository<DropConfig> repository,
        IUnitOfWork unitOfWork,
        IBrazilTimeZoneConverter timeZoneConverter)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _timeZoneConverter = timeZoneConverter;
    }

    public async Task<CreateDropConfigResult> Handle(
        CreateDropConfigRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Ativo)
        {
            await DisableOtherActiveDropsAsync(null, cancellationToken);
        }

        var now = DateTime.UtcNow;
        var dataLiberacaoUtc = _timeZoneConverter.ConvertBrasiliaToUtc(request.DataLiberacaoBrasilia);
        var entity = new DropConfig
        {
            Titulo = request.Titulo ?? string.Empty,
            Subtitulo = request.Subtitulo,
            DataLiberacao = dataLiberacaoUtc,
            Ativo = request.Ativo,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateDropConfigResult
        {
            Data = entity
        };
    }

    private async Task DisableOtherActiveDropsAsync(string? currentId, CancellationToken cancellationToken)
    {
        var activeDrops = await _repository
            .GetQuery()
            .ApplyIsDeletedFilter()
            .Where(x => x.Ativo && x.Id != currentId)
            .ToListAsync(cancellationToken);

        foreach (var drop in activeDrops)
        {
            drop.Ativo = false;
            drop.UpdatedAt = DateTime.UtcNow;
            _repository.Update(drop);
        }
    }
}
