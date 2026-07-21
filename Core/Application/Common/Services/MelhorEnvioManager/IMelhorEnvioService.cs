namespace Application.Common.Services.MelhorEnvioManager;

using Domain.Entities;

public sealed record MelhorEnvioShippingQuote(decimal Price, string? CarrierName);

public interface IMelhorEnvioService
{
    Task<string> CalcularFreteAsync(object request);
    Task<string> CriarEtiquetaAsync(object request);
    Task<string> AdicionarEtiquetaAoCarrinhoAsync(object request, CancellationToken cancellationToken = default);
    Task<string> ConsultarSaldoAsync(CancellationToken cancellationToken = default);
    Task<string> InserirSaldoAsync(object request, CancellationToken cancellationToken = default);
    Task<string> ComprarFretesAsync(object request, CancellationToken cancellationToken = default);
    Task<string> GerarEtiquetasAsync(object request, CancellationToken cancellationToken = default);
    Task<byte[]> BaixarEtiquetaPdfAsync(string labelId, CancellationToken cancellationToken = default);
    Task<decimal?> CalculateShippingCostAsync(
        ShippingBox shippingBox,
        string originPostCode,
        string destinationPostCode,
        CancellationToken cancellationToken = default);
    Task<MelhorEnvioShippingQuote?> CalculateCheapestShippingAsync(
        ShippingBox shippingBox,
        string originPostCode,
        string destinationPostCode,
        CancellationToken cancellationToken = default);
}
