using System.Globalization;
using System.Text.Json;
using Application.Common.Services.MelhorEnvioManager;
using FluentValidation;
using MediatR;

namespace Application.Features.OrderManager.Commands;

public class InsertMelhorEnvioBalanceResult
{
    public string? PaymentUrl { get; init; }
    public string? PixQrCode { get; init; }
    public string? PixCopyPaste { get; init; }
    public string? RawResponse { get; init; }
}

public class InsertMelhorEnvioBalanceRequest : IRequest<InsertMelhorEnvioBalanceResult>
{
    public decimal Value { get; init; }
    public string? Slug { get; init; }
    public string? RedirectUrl { get; init; }
    public string? FingerPrint { get; init; }
    public string? CompanyName { get; init; }
    public string? Cnpj { get; init; }
}

public class InsertMelhorEnvioBalanceValidator : AbstractValidator<InsertMelhorEnvioBalanceRequest>
{
    public InsertMelhorEnvioBalanceValidator()
    {
        RuleFor(x => x.Value).GreaterThan(0m);
        RuleFor(x => x.Slug).Must(x => x == "pix" || x == "boleto")
            .WithMessage("Informe pix ou boleto.");
    }
}

public class InsertMelhorEnvioBalanceHandler : IRequestHandler<InsertMelhorEnvioBalanceRequest, InsertMelhorEnvioBalanceResult>
{
    private readonly IMelhorEnvioService _melhorEnvioService;

    public InsertMelhorEnvioBalanceHandler(IMelhorEnvioService melhorEnvioService)
    {
        _melhorEnvioService = melhorEnvioService;
    }

    public async Task<InsertMelhorEnvioBalanceResult> Handle(
        InsertMelhorEnvioBalanceRequest request,
        CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object?>
        {
            ["gateway"] = "yapay-transparente",
            ["slug"] = request.Slug,
            ["value"] = request.Value.ToString("0.00", CultureInfo.InvariantCulture)
        };

        AddIfNotEmpty(payload, "redirect_url", request.RedirectUrl);
        AddIfNotEmpty(payload, "finger_print", request.FingerPrint);
        AddIfNotEmpty(payload, "company_name", request.CompanyName);
        AddIfNotEmpty(payload, "cnpj", OnlyDigits(request.Cnpj));

        var rawResponse = await _melhorEnvioService.InserirSaldoAsync(payload, cancellationToken);

        return new InsertMelhorEnvioBalanceResult
        {
            PaymentUrl = ExtractFirstUrl(rawResponse),
            PixQrCode = ExtractValue(rawResponse, IsQrCodeKey),
            PixCopyPaste = ExtractValue(rawResponse, IsPixCopyPasteKey),
            RawResponse = rawResponse
        };
    }

    private static void AddIfNotEmpty(Dictionary<string, object?> payload, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            payload[key] = value.Trim();
        }
    }

    private static string? OnlyDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }

    private static string? ExtractFirstUrl(string rawResponse)
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(rawResponse);
            return FindFirstUrl(document.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ExtractValue(string rawResponse, Func<string, bool> keyMatcher)
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(rawResponse);
            return FindValue(document.RootElement, keyMatcher);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? FindValue(JsonElement element, Func<string, bool> keyMatcher, string? propertyName = null)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                var value = element.GetString();
                return propertyName != null && keyMatcher(propertyName) && !string.IsNullOrWhiteSpace(value)
                    ? value
                    : null;
            case JsonValueKind.Number:
                return propertyName != null && keyMatcher(propertyName)
                    ? element.ToString()
                    : null;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var arrayValue = FindValue(item, keyMatcher);
                    if (!string.IsNullOrWhiteSpace(arrayValue))
                    {
                        return arrayValue;
                    }
                }
                break;
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var objectValue = FindValue(property.Value, keyMatcher, property.Name);
                    if (!string.IsNullOrWhiteSpace(objectValue))
                    {
                        return objectValue;
                    }
                }
                break;
        }

        return null;
    }

    private static string? FindFirstUrl(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                var value = element.GetString();
                return IsUrl(value) ? value : null;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var url = FindFirstUrl(item);
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        return url;
                    }
                }
                break;
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var url = FindFirstUrl(property.Value);
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        return url;
                    }
                }
                break;
        }

        return null;
    }

    private static bool IsUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static bool IsQrCodeKey(string key)
    {
        var normalized = NormalizeKey(key);
        return normalized.Contains("qrcode") ||
            normalized.Contains("qrcodeurl") ||
            normalized.Contains("qrimage");
    }

    private static bool IsPixCopyPasteKey(string key)
    {
        var normalized = NormalizeKey(key);
        return normalized.Contains("pix") &&
            (normalized.Contains("copy") ||
                normalized.Contains("copia") ||
                normalized.Contains("payload") ||
                normalized.Contains("brcode") ||
                normalized.Contains("emv"));
    }

    private static string NormalizeKey(string key)
    {
        return new string(key.Where(char.IsLetterOrDigit).ToArray())
            .ToLowerInvariant();
    }
}
