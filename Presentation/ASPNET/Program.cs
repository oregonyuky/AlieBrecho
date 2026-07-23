using ASPNET.BackEnd;
using ASPNET.BackEnd.Common.Middlewares;
using ASPNET.FrontEnd;
using DotNetEnv;
using Microsoft.AspNetCore.DataProtection;
using Npgsql;

LoadEnvironmentVariables();

var builder = WebApplication.CreateBuilder(args);

ConfigureRailway(builder);

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
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }))
    .AllowAnonymous();

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

static void ConfigureRailway(WebApplicationBuilder builder)
{
    if (int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var railwayPort))
    {
        builder.WebHost.UseUrls($"http://0.0.0.0:{railwayPort}");
    }

    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (string.IsNullOrWhiteSpace(databaseUrl))
    {
        builder.Configuration["DatabaseProvider"] = "Sqlite";
        builder.Configuration["ConnectionStrings:DefaultConnection"] = "Data Source=app.db";
        return;
    }

    if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri) ||
        (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
    {
        throw new InvalidOperationException("DATABASE_URL must be a valid PostgreSQL URL.");
    }

    var credentials = uri.UserInfo.Split(':', 2);
    if (credentials.Length != 2)
    {
        throw new InvalidOperationException("DATABASE_URL must include a username and password.");
    }

    var connectionString = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.IsDefaultPort ? 5432 : uri.Port,
        Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
        Username = Uri.UnescapeDataString(credentials[0]),
        Password = Uri.UnescapeDataString(credentials[1]),
        SslMode = SslMode.Prefer
    };

    builder.Configuration["DatabaseProvider"] = "PostgreSQL";
    builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString.ConnectionString;
}
