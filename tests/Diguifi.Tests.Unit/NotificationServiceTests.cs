using Diguifi.Application.DTOs.Notifications;
using Diguifi.Domain.Entities;
using Diguifi.Infrastructure.Services;
using Diguifi.Tests.Unit.Helpers;
using FluentAssertions;
using Xunit;

namespace Diguifi.Tests.Unit;

public sealed class NotificationServiceTests
{
    private static readonly Guid UserId1 = Guid.NewGuid();
    private static readonly Guid UserId2 = Guid.NewGuid();

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_NoFilter_ReturnsAllNotifications()
    {
        await using var db = DbContextFactory.Create();
        db.Notifications.AddRange(
            new Notification { UserId = UserId1, Text = "Hello 1" },
            new Notification { UserId = UserId2, Text = "Hello 2" }
        );
        await db.SaveChangesAsync();

        var result = await new NotificationService(db).GetAllAsync(null, CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_WithUserIdFilter_ReturnsOnlyThatUsersNotifications()
    {
        await using var db = DbContextFactory.Create();
        db.Notifications.AddRange(
            new Notification { UserId = UserId1, Text = "For user 1" },
            new Notification { UserId = UserId2, Text = "For user 2" }
        );
        await db.SaveChangesAsync();

        var result = await new NotificationService(db).GetAllAsync(UserId1, CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().UserId.Should().Be(UserId1);
        result.First().Text.Should().Be("For user 1");
    }

    [Fact]
    public async Task GetAllAsync_OrderedByCreatedAtDescending()
    {
        await using var db = DbContextFactory.Create();
        var older = new Notification { UserId = UserId1, Text = "older", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5) };
        var newer = new Notification { UserId = UserId1, Text = "newer", CreatedAt = DateTimeOffset.UtcNow };
        db.Notifications.AddRange(older, newer);
        await db.SaveChangesAsync();

        var result = await new NotificationService(db).GetAllAsync(null, CancellationToken.None);

        result.First().Text.Should().Be("newer");
        result.Last().Text.Should().Be("older");
    }

    [Fact]
    public async Task GetAllAsync_NoNotifications_ReturnsEmptyCollection()
    {
        await using var db = DbContextFactory.Create();

        var result = await new NotificationService(db).GetAllAsync(null, CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_NotificationExists_ReturnsNotification()
    {
        await using var db = DbContextFactory.Create();
        var notification = new Notification { UserId = UserId1, Text = "Test notification", Path = "/some/path" };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        var result = await new NotificationService(db).GetByIdAsync(notification.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(notification.Id);
        result.Text.Should().Be("Test notification");
        result.Path.Should().Be("/some/path");
    }

    [Fact]
    public async Task GetByIdAsync_NotificationNotFound_ReturnsNull()
    {
        await using var db = DbContextFactory.Create();

        var result = await new NotificationService(db).GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesAndReturnsNotification()
    {
        await using var db = DbContextFactory.Create();
        var request = new CreateNotificationRequest
        {
            UserId = UserId1,
            Text = "New notification",
            Path = "/orders"
        };

        var result = await new NotificationService(db).CreateAsync(request, CancellationToken.None);

        result.UserId.Should().Be(UserId1);
        result.Text.Should().Be("New notification");
        result.Path.Should().Be("/orders");
        result.IsRead.Should().BeFalse();
        db.Notifications.Should().ContainSingle(n => n.Text == "New notification");
    }

    [Fact]
    public async Task CreateAsync_WithoutPath_CreatesNotificationWithNullPath()
    {
        await using var db = DbContextFactory.Create();
        var request = new CreateNotificationRequest { UserId = UserId1, Text = "No path", Path = null };

        var result = await new NotificationService(db).CreateAsync(request, CancellationToken.None);

        result.Path.Should().BeNull();
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_NotificationExists_UpdatesFields()
    {
        await using var db = DbContextFactory.Create();
        var notification = new Notification { UserId = UserId1, Text = "Original", Path = "/old" };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        var result = await new NotificationService(db).UpdateAsync(
            notification.Id,
            new UpdateNotificationRequest { Text = "Updated", Path = "/new", IsRead = true },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Text.Should().Be("Updated");
        result.Value.Path.Should().Be("/new");
        result.Value.IsRead.Should().BeTrue();
        db.Notifications.Single(n => n.Id == notification.Id).IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_NotificationNotFound_ReturnsNotificationNotFound()
    {
        await using var db = DbContextFactory.Create();

        var result = await new NotificationService(db).UpdateAsync(
            Guid.NewGuid(),
            new UpdateNotificationRequest { Text = "X", IsRead = false },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("notification_not_found");
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_NotificationExists_RemovesRecord()
    {
        await using var db = DbContextFactory.Create();
        var notification = new Notification { UserId = UserId1, Text = "To delete" };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        var result = await new NotificationService(db).DeleteAsync(notification.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.Notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_NotificationNotFound_ReturnsNotificationNotFound()
    {
        await using var db = DbContextFactory.Create();

        var result = await new NotificationService(db).DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("notification_not_found");
    }

    // ── MarkAllAsReadAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task MarkAllAsReadAsync_OwnUnreadNotifications_MarksAllAsRead()
    {
        await using var db = DbContextFactory.Create();
        var n1 = new Notification { UserId = UserId1, Text = "A", IsRead = false };
        var n2 = new Notification { UserId = UserId1, Text = "B", IsRead = false };
        db.Notifications.AddRange(n1, n2);
        await db.SaveChangesAsync();

        await new NotificationService(db).MarkAllAsReadAsync([n1.Id, n2.Id], UserId1, CancellationToken.None);

        db.Notifications.All(n => n.IsRead).Should().BeTrue();
    }

    [Fact]
    public async Task MarkAllAsReadAsync_IdsFromOtherUser_AreNotMarked()
    {
        await using var db = DbContextFactory.Create();
        var own = new Notification { UserId = UserId1, Text = "Own", IsRead = false };
        var other = new Notification { UserId = UserId2, Text = "Other", IsRead = false };
        db.Notifications.AddRange(own, other);
        await db.SaveChangesAsync();

        await new NotificationService(db).MarkAllAsReadAsync([own.Id, other.Id], UserId1, CancellationToken.None);

        db.Notifications.Single(n => n.Id == own.Id).IsRead.Should().BeTrue();
        db.Notifications.Single(n => n.Id == other.Id).IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task MarkAllAsReadAsync_AlreadyReadNotifications_AreSkipped()
    {
        await using var db = DbContextFactory.Create();
        var n = new Notification { UserId = UserId1, Text = "Already read", IsRead = true };
        db.Notifications.Add(n);
        await db.SaveChangesAsync();

        await new NotificationService(db).MarkAllAsReadAsync([n.Id], UserId1, CancellationToken.None);

        db.Notifications.Single().IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAllAsReadAsync_EmptyList_DoesNothing()
    {
        await using var db = DbContextFactory.Create();
        db.Notifications.Add(new Notification { UserId = UserId1, Text = "X", IsRead = false });
        await db.SaveChangesAsync();

        await new NotificationService(db).MarkAllAsReadAsync([], UserId1, CancellationToken.None);

        db.Notifications.Single().IsRead.Should().BeFalse();
    }
}
