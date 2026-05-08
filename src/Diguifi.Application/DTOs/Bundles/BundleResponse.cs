namespace Diguifi.Application.DTOs.Bundles;

public sealed class BundleResponse
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string DriveUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}
