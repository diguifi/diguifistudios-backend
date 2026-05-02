namespace Diguifi.Application.Interfaces;

public sealed class StripeCheckoutSessionResult
{
    public string CheckoutUrl { get; set; } = string.Empty;
    public string CheckoutSessionId { get; set; } = string.Empty;
    public string? PaymentIntentId { get; set; }
}
