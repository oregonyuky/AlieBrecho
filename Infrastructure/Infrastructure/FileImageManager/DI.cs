using Application.Common.Services.FileImageManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.FileImageManager;

public static class DI
{
    public static IServiceCollection RegisterFileImageManager(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FileImageSettings>(configuration.GetSection("FileImageManager"));
        services.AddHttpClient<IFileImageService, FileImageService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
