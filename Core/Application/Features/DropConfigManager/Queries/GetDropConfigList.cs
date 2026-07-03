using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Time;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.DropConfigManager.Queries;

public record DropConfigAdminDto
{
    public string? Id { get; init; }
    public string? Titulo { get; init; }
    public string? Subtitulo { get; init; }
    public DateTime DataLiberacaoBrasilia { get; init; }
    public string TimeZoneId { get; init; } = "America/Sao_Paulo";
    public bool Ativo { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record DropConfigActiveDto
{
    public string? Id { get; init; }
    public string? Titulo { get; init; }
    public string? Subtitulo { get; init; }
    public DateTime DataLiberacaoUtc { get; init; }
    public bool Ativo { get; init; }
}

public class GetDropConfigListResult
{
    public List<DropConfigAdminDto>? Data { get; init; }
}

public class GetDropConfigListRequest : IRequest<GetDropConfigListResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetDropConfigListHandler : IRequestHandler<GetDropConfigListRequest, GetDropConfigListResult>
{
    private readonly IQueryContext _context;
    private readonly IBrazilTimeZoneConverter _timeZoneConverter;

    public GetDropConfigListHandler(
        IQueryContext context,
        IBrazilTimeZoneConverter timeZoneConverter)
    {
        _context = context;
        _timeZoneConverter = timeZoneConverter;
    }

    public async Task<GetDropConfigListResult> Handle(
        GetDropConfigListRequest request,
        CancellationToken cancellationToken)
    {
        var entities = await _context
            .DropConfig
            .AsNoTracking()
            .ApplyIsDeletedFilter(request.IsDeleted)
            .OrderByDescending(x => x.Ativo)
            .ThenByDescending(x => x.DataLiberacao)
            .ToListAsync(cancellationToken);

        var dtos = entities.Select(entity => new DropConfigAdminDto
        {
            Id = entity.Id,
            Titulo = entity.Titulo,
            Subtitulo = entity.Subtitulo,
            DataLiberacaoBrasilia = _timeZoneConverter.ConvertUtcToBrasilia(entity.DataLiberacao),
            TimeZoneId = _timeZoneConverter.TimeZoneId,
            Ativo = entity.Ativo,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        }).ToList();

        return new GetDropConfigListResult
        {
            Data = dtos
        };
    }
}
