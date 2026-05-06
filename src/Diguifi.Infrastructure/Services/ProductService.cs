using Diguifi.Application.Common;
using Diguifi.Application.DTOs.Products;
using Diguifi.Application.Interfaces;
using Diguifi.Domain.Entities;
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
                Slug = x.Slug,
                Name = x.Name,
                Description = x.Description,
                Price = x.Price,
                Currency = x.Currency,
                Category = x.Category.ToString().ToLowerInvariant(),
                IsActive = x.IsActive,
                IsPurchased = userId != null && dbContext.Orders.Any(o =>
                    o.UserId == userId &&
                    o.ProductId == x.Id &&
                    o.Status == OrderStatus.Paid),
                StripeProductId = x.StripeProductId,
                StripePriceId = x.StripePriceId
            })
            .ToListAsync(cancellationToken);

    public async Task<ProductResponse?> GetByIdAsync(string productId, CancellationToken cancellationToken)
        => await dbContext.Products
            .AsNoTracking()
            .Where(x => x.Id == productId)
            .Select(x => new ProductResponse
            {
                Id = x.Id,
                Slug = x.Slug,
                Name = x.Name,
                Description = x.Description,
                Price = x.Price,
                Currency = x.Currency,
                Category = x.Category.ToString().ToLowerInvariant(),
                IsActive = x.IsActive,
                StripeProductId = x.StripeProductId,
                StripePriceId = x.StripePriceId
            })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<Result<ProductResponse>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var slugExists = await dbContext.Products.AnyAsync(x => x.Slug == request.Slug, cancellationToken);
        if (slugExists)
            return Result<ProductResponse>.Failure("slug_conflict", "Já existe um produto com esse slug.");

        var product = new Product
        {
            Id = Guid.NewGuid().ToString("N"),
            Slug = request.Slug,
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            Price = request.Price,
            Currency = request.Currency,
            StripeProductId = request.StripeProductId,
            StripePriceId = request.StripePriceId,
            IsActive = request.IsActive
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<ProductResponse>.Success(MapResponse(product));
    }

    public async Task<Result<ProductResponse>> UpdateAsync(string id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (product is null)
            return Result<ProductResponse>.Failure("product_not_found", "Produto não encontrado.");

        if (request.Slug is not null) product.Slug = request.Slug;
        if (request.Name is not null) product.Name = request.Name;
        if (request.Description is not null) product.Description = request.Description;
        if (request.Category is not null) product.Category = request.Category.Value;
        if (request.Price is not null) product.Price = request.Price.Value;
        if (request.Currency is not null) product.Currency = request.Currency;
        if (request.StripeProductId is not null) product.StripeProductId = request.StripeProductId;
        if (request.StripePriceId is not null) product.StripePriceId = request.StripePriceId;
        if (request.IsActive is not null) product.IsActive = request.IsActive.Value;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<ProductResponse>.Success(MapResponse(product));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (product is null)
            return Result<bool>.Failure("product_not_found", "Produto não encontrado.");

        var hasOrders = await dbContext.Orders.AnyAsync(o => o.ProductId == id, cancellationToken);
        if (hasOrders)
            return Result<bool>.Failure("product_has_orders", "Produto possui pedidos associados e não pode ser excluído.");

        dbContext.Products.Remove(product);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    private static ProductResponse MapResponse(Product product) => new()
    {
        Id = product.Id,
        Slug = product.Slug,
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        Currency = product.Currency,
        Category = product.Category.ToString().ToLowerInvariant(),
        IsActive = product.IsActive,
        StripeProductId = product.StripeProductId,
        StripePriceId = product.StripePriceId
    };
}
