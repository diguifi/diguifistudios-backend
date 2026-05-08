namespace Diguifi.Application.DTOs.Notifications;

public sealed class UpdateNotificationRequest
{
    public string Text { get; set; } = string.Empty;
    public string? Path { get; set; }
    public bool IsRead { get; set; }
}
