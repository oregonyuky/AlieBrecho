using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Common.Time;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.DropConfigManager.Commands;

public class UpdateDropConfigResult
{
    public DropConfig? Data { get; set; }
}

public class UpdateDropConfigRequest : IRequest<UpdateDropConfigResult>
{
    public string? Id { get; init; }
    public string? Titulo { get; init; }
    public string? Subtitulo { get; init; }
    public DateTime DataLiberacaoBrasilia { get; init; }
    public bool Ativo { get; init; }
}

public class UpdateDropConfigValidator : AbstractValidator<UpdateDropConfigRequest>
{
    public UpdateDropConfigValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Titulo).NotEmpty();
        RuleFor(x => x.DataLiberacaoBrasilia).NotEmpty();
    }
}

public class UpdateDropConfigHandler : IRequestHandler<UpdateDropConfigRequest, UpdateDropConfigResult>
{
    private readonly ICommandRepository<DropConfig> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBrazilTimeZoneConverter _timeZoneConverter;

    public UpdateDropConfigHandler(
        ICommandRepository<DropConfig> repository,
        IUnitOfWork unitOfWork,
        IBrazilTimeZoneConverter timeZoneConverter)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _timeZoneConverter = timeZoneConverter;
    }

    public async Task<UpdateDropConfigResult> Handle(
        UpdateDropConfigRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        if (request.Ativo)
        {
            await DisableOtherActiveDropsAsync(entity.Id, cancellationToken);
        }

        entity.Titulo = request.Titulo ?? string.Empty;
        entity.Subtitulo = request.Subtitulo;
        entity.DataLiberacao = _timeZoneConverter.ConvertBrasiliaToUtc(request.DataLiberacaoBrasilia);
        entity.Ativo = request.Ativo;
        entity.UpdatedAt = DateTime.UtcNow;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateDropConfigResult
        {
            Data = entity
        };
    }

    private async Task DisableOtherActiveDropsAsync(string currentId, CancellationToken cancellationToken)
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
