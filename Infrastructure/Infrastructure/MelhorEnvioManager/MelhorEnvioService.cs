using Application.Common.Services.MelhorEnvioManager;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Infrastructure.MelhorEnvioManager;

public class MelhorEnvioService : IMelhorEnvioService
{
    private readonly HttpClient _httpClient;
    private readonly MelhorEnvioSettings _settings;

    public MelhorEnvioService(HttpClient httpClient, IOptions<MelhorEnvioSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<string> CriarEtiquetaAsync(object request)
    {
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            throw new InvalidOperationException("MelhorEnvio BaseUrl is not configured.");
        }

        var requestContent = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.BaseUrl)
        {
            Content = requestContent
        };

        if (!string.IsNullOrWhiteSpace(_settings.AccessToken))
        {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.AccessToken);
        }

        using var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}
