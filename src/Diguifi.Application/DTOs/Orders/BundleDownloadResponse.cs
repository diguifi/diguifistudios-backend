namespace Diguifi.Application.DTOs.Orders;

public sealed class BundleDownloadResponse
{
    public string DownloadUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
