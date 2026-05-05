using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

public class MelhorEnvioService : IMelhorEnvioService
{
    private readonly HttpClient _httpClient;
    private readonly MelhorEnvioSettings _settings;

    public MelhorEnvioService(
        HttpClient httpClient,
        IOptions<MelhorEnvioSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    private void AddAuth()
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.Token);
    }

    public async Task<string> CalcularFreteAsync(object request)
    {
        AddAuth();

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync(
            $"{_settings.BaseUrl}v2/me/shipment/calculate",
            content
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error);
        }

        return await response.Content.ReadAsStringAsync();
    }
}