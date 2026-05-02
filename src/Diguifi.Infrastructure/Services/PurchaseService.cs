using Diguifi.Application.DTOs.Purchases;
using Diguifi.Application.Interfaces;
using Diguifi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Diguifi.Infrastructure.Services;

public sealed class PurchaseService(AppDbContext dbContext) : IPurchaseService
{
    public async Task<IReadOnlyCollection<PurchaseResponse>> GetPurchasesForUserAsync(Guid userId, CancellationToken cancellationToken)
        => await dbContext.Orders
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new PurchaseResponse
            {
                Id = x.Id.ToString(),
                ProductId = x.ProductId,
                Status = x.Status.ToString().ToLowerInvariant(),
                Amount = x.Amount,
                Currency = x.Currency,
                PurchasedAt = x.PaidAt
            })
            .ToListAsync(cancellationToken);
}
