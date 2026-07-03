using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

    public GetActiveDropConfigHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetActiveDropConfigResult> Handle(
        GetActiveDropConfigRequest request,
        CancellationToken cancellationToken)
    {
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
