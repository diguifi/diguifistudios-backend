using Diguifi.Application.DTOs.Products;
using Diguifi.Application.Interfaces;
using Diguifi.Domain.Enums;
using Diguifi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Diguifi.Infrastructure.Services;

public sealed class ProductService(AppDbContext dbContext) : IProductService
{
    public async Task<IReadOnlyCollection<ProductResponse>> GetProductsAsync(Guid? userId, CancellationToken cancellationToken)
        => await dbContext.Products
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => new ProductResponse
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Price = x.Price,
                Currency = x.Currency,
                Category = x.Category.ToString().ToLowerInvariant(),
                IsActive = x.IsActive,
                IsPurchased = userId != null && dbContext.Orders.Any(o =>
                    o.UserId == userId &&
                    o.ProductId == x.Id &&
                    o.Status == OrderStatus.Paid)
            })
            .ToListAsync(cancellationToken);

    public async Task<ProductResponse?> GetByIdAsync(string productId, CancellationToken cancellationToken)
        => await dbContext.Products
            .AsNoTracking()
            .Where(x => x.Id == productId)
            .Select(x => new ProductResponse
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Price = x.Price,
                Currency = x.Currency,
                Category = x.Category.ToString().ToLowerInvariant(),
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);
}
