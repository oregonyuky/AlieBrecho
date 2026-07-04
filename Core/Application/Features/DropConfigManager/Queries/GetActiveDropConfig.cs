using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Features.DropConfigManager.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.DropConfigManager.Queries;

public class GetActiveDropConfigResult
{
    public DropConfigActiveDto? Data { get; init; }
}

public class GetActiveDropConfigRequest : IRequest<GetActiveDropConfigResult>
{
}

public class GetActiveDropConfigHandler : IRequestHandler<GetActiveDropConfigRequest, GetActiveDropConfigResult>
{
    private readonly IQueryContext _context;
    private readonly IDropConfigReleaseService _dropConfigReleaseService;
    private readonly ILogger<GetActiveDropConfigHandler> _logger;

    public GetActiveDropConfigHandler(
        IQueryContext context,
        IDropConfigReleaseService dropConfigReleaseService,
        ILogger<GetActiveDropConfigHandler> logger)
    {
        _context = context;
        _dropConfigReleaseService = dropConfigReleaseService;
        _logger = logger;
    }

    public async Task<GetActiveDropConfigResult> Handle(
        GetActiveDropConfigRequest request,
        CancellationToken cancellationToken)
    {
        var sweepResult = await _dropConfigReleaseService.ReleaseDueDropsAsync(
            source: "active-drop-query",
            cancellationToken);

        _logger.LogInformation(
            "Consulta de drop ativo executou verificacao automatica. DropsVerificados={DropsChecked}; ProdutosLiberados={ProductsReleased}.",
            sweepResult.DropsChecked,
            sweepResult.ProductsReleased);

        var entity = await _context
            .DropConfig
            .AsNoTracking()
            .ApplyIsDeletedFilter()
            .Where(x => x.Ativo)
            .OrderByDescending(x => x.DataLiberacao)
            .FirstOrDefaultAsync(cancellationToken);

        var dto = entity is null
            ? null
            : new DropConfigActiveDto
            {
                Id = entity.Id,
                Titulo = entity.Titulo,
                Subtitulo = entity.Subtitulo,
                DataLiberacaoUtc = DateTime.SpecifyKind(entity.DataLiberacao, DateTimeKind.Utc),
                Ativo = entity.Ativo
            };

        return new GetActiveDropConfigResult
        {
            Data = dto
        };
    }
}
