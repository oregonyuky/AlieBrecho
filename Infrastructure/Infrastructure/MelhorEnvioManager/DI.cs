using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Application.Common.Services.MelhorEnvioManager;

namespace Infrastructure.MelhorEnvioManager;

public static class DI
{
    public static IServiceCollection AddMelhorEnvio(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MelhorEnvioSettings>(configuration.GetSection("MelhorEnvio"));
        services.AddHttpClient<IMelhorEnvioService, MelhorEnvioService>();

        return services;
    }
}