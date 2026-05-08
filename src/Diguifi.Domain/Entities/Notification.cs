namespace Diguifi.Domain.Entities;

public sealed class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Path { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsRead { get; set; } = false;

    public User? User { get; set; }
}
