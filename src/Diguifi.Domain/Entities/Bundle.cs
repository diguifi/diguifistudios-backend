namespace Diguifi.Domain.Entities;

public sealed class Bundle
{
    public string ProductId { get; set; } = string.Empty;
    public string DriveUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Product? Product { get; set; }
}
