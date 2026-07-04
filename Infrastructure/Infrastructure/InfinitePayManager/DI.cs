using Application.Common.Services.InfinitePayManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.InfinitePayManager;

public static class DI
{
    public static IServiceCollection AddInfinitePay(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<InfinitePaySettings>(configuration.GetSection("InfinitePay"));
        services.AddHttpClient<IInfinitePayService, InfinitePayService>();

        return services;
    }
}
