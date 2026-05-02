using Diguifi.Application.DTOs.Products;
using Diguifi.Application.Interfaces;
using Diguifi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Diguifi.Infrastructure.Services;

public sealed class ProductService(AppDbContext dbContext) : IProductService
{
    public async Task<IReadOnlyCollection<ProductResponse>> GetProductsAsync(CancellationToken cancellationToken)
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
                IsActive = x.IsActive
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
