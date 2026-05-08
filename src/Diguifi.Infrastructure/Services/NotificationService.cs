using Diguifi.Application.Common;
using Diguifi.Application.DTOs.Notifications;
using Diguifi.Application.Interfaces;
using Diguifi.Domain.Entities;
using Diguifi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Diguifi.Infrastructure.Services;

public sealed class NotificationService(AppDbContext dbContext) : INotificationService
{
    public async Task<IReadOnlyCollection<NotificationResponse>> GetAllAsync(Guid? userId, CancellationToken ct)
        => await dbContext.Notifications
            .AsNoTracking()
            .Where(n => userId == null || n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => Map(n))
            .ToListAsync(ct);

    public async Task<NotificationResponse?> GetByIdAsync(Guid id, CancellationToken ct)
        => await dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.Id == id)
            .Select(n => Map(n))
            .FirstOrDefaultAsync(ct);

    public async Task<NotificationResponse> CreateAsync(CreateNotificationRequest request, CancellationToken ct)
    {
        var entity = new Notification
        {
            UserId = request.UserId,
            Text = request.Text,
            Path = request.Path
        };

        dbContext.Notifications.Add(entity);
        await dbContext.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<Result<NotificationResponse>> UpdateAsync(Guid id, UpdateNotificationRequest request, CancellationToken ct)
    {
        var entity = await dbContext.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (entity is null)
            return Result<NotificationResponse>.Failure("notification_not_found", "Notificação não encontrada.");

        entity.Text = request.Text;
        entity.Path = request.Path;
        entity.IsRead = request.IsRead;
        await dbContext.SaveChangesAsync(ct);
        return Result<NotificationResponse>.Success(Map(entity));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await dbContext.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (entity is null)
            return Result<bool>.Failure("notification_not_found", "Notificação não encontrada.");

        dbContext.Notifications.Remove(entity);
        await dbContext.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task MarkAllAsReadAsync(IReadOnlyCollection<Guid> ids, Guid userId, CancellationToken ct)
    {
        if (ids.Count == 0) return;

        var entities = await dbContext.Notifications
            .AsNoTracking()
            .Where(n => ids.Contains(n.Id) && n.UserId == userId && !n.IsRead)
            .ToListAsync(ct);

        if (entities.Count == 0) return;

        foreach (var e in entities)
            e.IsRead = true;

        dbContext.Notifications.UpdateRange(entities);
        await dbContext.SaveChangesAsync(ct);
    }

    public Task<bool> HasUnreadAsync(Guid userId, CancellationToken ct)
        => dbContext.Notifications.AnyAsync(n => n.UserId == userId && !n.IsRead, ct);

    private static NotificationResponse Map(Notification n) => new()
    {
        Id = n.Id,
        UserId = n.UserId,
        Text = n.Text,
        Path = n.Path,
        CreatedAt = n.CreatedAt,
        IsRead = n.IsRead
    };
}
