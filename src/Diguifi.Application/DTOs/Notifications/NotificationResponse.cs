namespace Diguifi.Application.DTOs.Notifications;

public sealed class NotificationResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Path { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsRead { get; set; }
}
