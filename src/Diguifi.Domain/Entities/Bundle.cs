using Diguifi.Domain.Enums;

namespace Diguifi.Domain.Entities;

public sealed class Bundle
{
    public string ProductId { get; set; } = string.Empty;
    public string DriveUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public BundleType BundleType { get; set; } = BundleType.GameNotion;

    public Product? Product { get; set; }
}
