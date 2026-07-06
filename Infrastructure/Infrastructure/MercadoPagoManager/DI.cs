using Application.Common.Services.MercadoPagoManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.MercadoPagoManager;

public static class DI
{
    public static IServiceCollection AddMercadoPago(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MercadoPagoSettings>(configuration.GetSection("MercadoPago"));
        services.AddHttpClient<IMercadoPagoService, MercadoPagoService>();

        return services;
    }
}
