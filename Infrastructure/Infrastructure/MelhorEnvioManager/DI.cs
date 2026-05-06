using Application.Common.Services.MelhorEnvioManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.MelhorEnvioManager;

public static class DI
{
    public static IServiceCollection RegisterMelhorEnvioManager(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MelhorEnvioSettings>(configuration.GetSection("MelhorEnvio"));
        services.AddHttpClient<IMelhorEnvioService, MelhorEnvioService>();

        return services;
    }
}
