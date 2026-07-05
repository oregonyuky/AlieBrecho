using ASPNET.BackEnd;
using ASPNET.BackEnd.Common.Middlewares;
using ASPNET.FrontEnd;
using DotNetEnv;
using Microsoft.AspNetCore.DataProtection;

LoadEnvironmentVariables();

var builder = WebApplication.CreateBuilder(args);

//>>> Create Logs folder for Serilog
var logPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "app_data", "logs");
if (!Directory.Exists(logPath))
{
    Directory.CreateDirectory(logPath);
}

var dataProtectionPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "app_data", "dataprotection-keys");
Directory.CreateDirectory(dataProtectionPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));

builder.Services.AddBackEndServices(builder.Configuration);
builder.Services.AddFrontEndServices();

var app = builder.Build();

app.RegisterBackEndBuilder(app.Environment, app, builder.Configuration);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseRouting();
app.UseCors();
app.UseMiddleware<GlobalApiExceptionHandlerMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();

app.MapFrontEndRoutes();
app.MapBackEndRoutes();

app.Run();

static void LoadEnvironmentVariables()
{
    var candidates = new[]
    {
        Path.Combine(Directory.GetCurrentDirectory(), ".env"),
        Path.Combine(Directory.GetCurrentDirectory(), "Presentation", "ASPNET", ".env"),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env"))
    };

    foreach (var path in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
    {
        if (File.Exists(path))
        {
            Env.Load(path);
        }
    }
}
