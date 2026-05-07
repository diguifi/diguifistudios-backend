using Diguifi.Application.Common;
using Diguifi.Application.DTOs.Bundles;
using Diguifi.Application.DTOs.Orders;

namespace Diguifi.Application.Interfaces;

public interface IBundleService
{
    Task<Result<bool>> UpsertAsync(string productId, UpsertBundleRequest request, CancellationToken ct);
    Task<Result<BundleDownloadResponse>> GetAsync(string productId, CancellationToken ct);
}
