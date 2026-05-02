using Diguifi.Application.DTOs.Products;

namespace Diguifi.Application.Interfaces;

public interface IProductService
{
    Task<IReadOnlyCollection<ProductResponse>> GetProductsAsync(CancellationToken cancellationToken);
    Task<ProductResponse?> GetByIdAsync(string productId, CancellationToken cancellationToken);
}
