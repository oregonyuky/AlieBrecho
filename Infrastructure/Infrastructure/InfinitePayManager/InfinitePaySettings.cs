namespace Infrastructure.InfinitePayManager;

public class InfinitePaySettings
{
    public string? Handle { get; init; }
    public string? BaseUrl { get; init; }
    public string? CreateCheckoutPath { get; init; } = "links";
    public string? RedirectUrl { get; init; }
    public string? WebhookUrl { get; init; }
}
