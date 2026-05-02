namespace Diguifi.Application.DTOs.Webhooks;

public sealed class StripeWebhookRequest
{
    public string Payload { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}
