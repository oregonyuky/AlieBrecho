using System.Globalization;
using System.Text.Json;
using Application.Common.Services.MelhorEnvioManager;
using MediatR;

namespace Application.Features.OrderManager.Queries;

public record GetMelhorEnvioBalanceDto
{
    public decimal Balance { get; init; }
    public decimal Reserved { get; init; }
    public decimal Debts { get; init; }
}

public class GetMelhorEnvioBalanceResult
{
    public GetMelhorEnvioBalanceDto? Data { get; init; }
}

public class GetMelhorEnvioBalanceRequest : IRequest<GetMelhorEnvioBalanceResult>
{
}

public class GetMelhorEnvioBalanceHandler : IRequestHandler<GetMelhorEnvioBalanceRequest, GetMelhorEnvioBalanceResult>
{
    private readonly IMelhorEnvioService _melhorEnvioService;

    public GetMelhorEnvioBalanceHandler(IMelhorEnvioService melhorEnvioService)
    {
        _melhorEnvioService = melhorEnvioService;
    }

    public async Task<GetMelhorEnvioBalanceResult> Handle(
        GetMelhorEnvioBalanceRequest request,
        CancellationToken cancellationToken)
    {
        var rawResponse = await _melhorEnvioService.ConsultarSaldoAsync(cancellationToken);

        using var document = JsonDocument.Parse(rawResponse);
        var root = document.RootElement;

        return new GetMelhorEnvioBalanceResult
        {
            Data = new GetMelhorEnvioBalanceDto
            {
                Balance = GetDecimal(root, "balance") ?? 0m,
                Reserved = GetDecimal(root, "reserved") ?? 0m,
                Debts = GetDecimal(root, "debts") ?? 0m
            }
        };
    }

    private static decimal? GetDecimal(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetDecimal(out var number) => number,
            JsonValueKind.String when decimal.TryParse(property.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var number) => number,
            _ => null
        };
    }
}
