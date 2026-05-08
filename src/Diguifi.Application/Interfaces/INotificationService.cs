using Diguifi.Application.Common;
using Diguifi.Application.DTOs.Notifications;

namespace Diguifi.Application.Interfaces;

public interface INotificationService
{
    Task<IReadOnlyCollection<NotificationResponse>> GetAllAsync(Guid? userId, CancellationToken ct);
    Task<NotificationResponse?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<NotificationResponse> CreateAsync(CreateNotificationRequest request, CancellationToken ct);
    Task<Result<NotificationResponse>> UpdateAsync(Guid id, UpdateNotificationRequest request, CancellationToken ct);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct);
    Task MarkAllAsReadAsync(IReadOnlyCollection<Guid> ids, Guid userId, CancellationToken ct);
    Task<bool> HasUnreadAsync(Guid userId, CancellationToken ct);
}
