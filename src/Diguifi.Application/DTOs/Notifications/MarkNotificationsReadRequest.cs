namespace Diguifi.Application.DTOs.Notifications;

public sealed class MarkNotificationsReadRequest
{
    public IReadOnlyCollection<Guid> Ids { get; set; } = [];
}
