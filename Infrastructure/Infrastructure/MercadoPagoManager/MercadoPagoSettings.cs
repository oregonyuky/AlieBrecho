namespace Infrastructure.MercadoPagoManager;

public sealed class MercadoPagoSettings
{
    public string? AccessToken { get; set; }
    public string? BaseUrl { get; set; } = "https://api.mercadopago.com";
    public int ExpirationMinutes { get; set; } = 30;
}
