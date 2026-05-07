namespace Diguifi.Application.DTOs.Bundles;

public sealed class UpsertBundleRequest
{
    public string DriveUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
