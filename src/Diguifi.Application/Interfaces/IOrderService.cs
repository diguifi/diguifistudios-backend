using Diguifi.Application.Common;
using Diguifi.Application.DTOs.Orders;

namespace Diguifi.Application.Interfaces;

public interface IOrderService
{
    Task<Result<CancelSubscriptionResponse>> CancelSubscriptionAsync(
        Guid orderId, Guid userId, string returnUrl, CancellationToken ct);

    Task<Result<BundleDownloadResponse>> GetBundleDownloadAsync(
        Guid orderId, Guid userId, CancellationToken ct);
}
