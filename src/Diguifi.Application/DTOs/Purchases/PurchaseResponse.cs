namespace Diguifi.Application.DTOs.Purchases;

public sealed class PurchaseResponse
{
    public string Id { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTimeOffset? PurchasedAt { get; set; }
}
