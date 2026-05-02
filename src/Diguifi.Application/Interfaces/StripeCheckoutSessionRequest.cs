namespace Diguifi.Application.Interfaces;

public sealed class StripeCheckoutSessionRequest
{
    public Guid OrderId { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public string? StripePriceId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
}
