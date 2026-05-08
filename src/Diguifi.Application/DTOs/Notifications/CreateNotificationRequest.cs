namespace Diguifi.Application.DTOs.Notifications;

public sealed class CreateNotificationRequest
{
    public Guid UserId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Path { get; set; }
}
