using Diguifi.Domain.Enums;

namespace Diguifi.Domain.Entities;

public sealed class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string? StripeCheckoutSessionId { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }

    public User? User { get; set; }
    public Product? Product { get; set; }
}
