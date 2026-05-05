namespace Application.Common.Services.MelhorEnvioManager;
public interface IMelhorEnvioService
{
    Task<string> CalcularFreteAsync(object request);
    Task<string> CriarEtiquetaAsync(object request);
}