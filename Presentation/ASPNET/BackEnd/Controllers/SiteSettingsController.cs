using System.Text.Json;
using ASPNET.BackEnd.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SiteSettingsController(IWebHostEnvironment environment) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private string SettingsPath => Path.Combine(
        environment.WebRootPath,
        "app_data",
        "site-settings.json");

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ApiSuccessResult<SiteSettingsResponse>>> GetAsync(
        CancellationToken cancellationToken)
    {
        var settings = await ReadSettingsAsync(cancellationToken);
        return Ok(Wrap(settings));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ApiSuccessResult<SiteSettingsResponse>>> UpdateAsync(
        UpdateSiteSettingsRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.HeroImageName) ||
            request.HeroImageName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            Path.GetFileName(request.HeroImageName) != request.HeroImageName)
        {
            return BadRequest("Imagem do hero invalida.");
        }

        var settings = new SiteSettingsResponse
        {
            HeroImageName = request.HeroImageName.Trim(),
            HeroImageUrl = $"/api/FileImage/GetImage?imageName={Uri.EscapeDataString(request.HeroImageName.Trim())}"
        };

        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        await System.IO.File.WriteAllTextAsync(
            SettingsPath,
            JsonSerializer.Serialize(settings, JsonOptions),
            cancellationToken);

        return Ok(Wrap(settings));
    }

    private async Task<SiteSettingsResponse> ReadSettingsAsync(CancellationToken cancellationToken)
    {
        if (!System.IO.File.Exists(SettingsPath))
        {
            return new SiteSettingsResponse();
        }

        var json = await System.IO.File.ReadAllTextAsync(SettingsPath, cancellationToken);
        return JsonSerializer.Deserialize<SiteSettingsResponse>(json, JsonOptions)
            ?? new SiteSettingsResponse();
    }

    private static ApiSuccessResult<SiteSettingsResponse> Wrap(SiteSettingsResponse settings) => new()
    {
        Code = StatusCodes.Status200OK,
        Message = "Configuracao do site carregada com sucesso.",
        Content = settings
    };
}

public sealed record UpdateSiteSettingsRequest
{
    public string? HeroImageName { get; init; }
}

public sealed record SiteSettingsResponse
{
    public string? HeroImageName { get; init; }
    public string? HeroImageUrl { get; init; }
}
