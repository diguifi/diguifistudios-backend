namespace Diguifi.Application.DTOs.Checkout;

public sealed class CreateCheckoutSessionRequest
{
    public string ProductId { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
}
