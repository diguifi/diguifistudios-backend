using Diguifi.Application.DTOs.Purchases;

namespace Diguifi.Application.Interfaces;

public interface IPurchaseService
{
    Task<IReadOnlyCollection<PurchaseResponse>> GetPurchasesForUserAsync(Guid userId, CancellationToken cancellationToken);
}
