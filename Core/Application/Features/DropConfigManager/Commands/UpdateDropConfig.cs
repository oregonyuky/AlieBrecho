using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Common.Time;
using Application.Features.DropConfigManager.Services;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.DropConfigManager.Commands;

public class UpdateDropConfigResult
{
    public DropConfig? Data { get; set; }
    public int ReleasedProductsCount { get; set; }
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
    private readonly IDropConfigReleaseService _dropConfigReleaseService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBrazilTimeZoneConverter _timeZoneConverter;
    private readonly ILogger<UpdateDropConfigHandler> _logger;

    public UpdateDropConfigHandler(
        ICommandRepository<DropConfig> repository,
        IDropConfigReleaseService dropConfigReleaseService,
        IUnitOfWork unitOfWork,
        IBrazilTimeZoneConverter timeZoneConverter,
        ILogger<UpdateDropConfigHandler> logger)
    {
        _repository = repository;
        _dropConfigReleaseService = dropConfigReleaseService;
        _unitOfWork = unitOfWork;
        _timeZoneConverter = timeZoneConverter;
        _logger = logger;
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

        var shouldReleaseProducts = request.Ativo && !entity.Ativo;

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
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

            var releasedProductsCount = shouldReleaseProducts
                ? await _dropConfigReleaseService.ReleaseProductsForDropAsync(
                    entity,
                    DateTime.UtcNow,
                    requireReleaseTime: false,
                    source: "manual-update",
                    cancellationToken)
                : 0;

            await _unitOfWork.SaveAsync(cancellationToken);

            _logger.LogInformation(
                "Drop {DropId} atualizado. Produtos liberados automaticamente: {ReleasedProductsCount}.",
                entity.Id,
                releasedProductsCount);

            return new UpdateDropConfigResult
            {
                Data = entity,
                ReleasedProductsCount = releasedProductsCount
            };
        }, cancellationToken);
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
