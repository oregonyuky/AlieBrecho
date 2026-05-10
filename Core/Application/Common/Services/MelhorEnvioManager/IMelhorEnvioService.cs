namespace Application.Common.Services.MelhorEnvioManager;

using Domain.Entities;

public interface IMelhorEnvioService
{
    Task<string> CalcularFreteAsync(object request);
    Task<string> CriarEtiquetaAsync(object request);
    Task<decimal?> CalculateShippingCostAsync(
        ShippingBox shippingBox,
        string originPostCode,
        string destinationPostCode,
        CancellationToken cancellationToken = default);
}
