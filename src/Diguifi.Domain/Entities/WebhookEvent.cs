using Diguifi.Domain.Enums;

namespace Diguifi.Domain.Entities;

public sealed class WebhookEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Provider { get; set; } = string.Empty;
    public string ExternalEventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
    public WebhookEventStatus Status { get; set; } = WebhookEventStatus.Pending;
}
