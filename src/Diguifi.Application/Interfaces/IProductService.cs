using Diguifi.Application.DTOs.Products;

namespace Diguifi.Application.Interfaces;

public interface IProductService
{
    Task<IReadOnlyCollection<ProductResponse>> GetProductsAsync(Guid? userId, CancellationToken cancellationToken);
    Task<ProductResponse?> GetByIdAsync(string productId, CancellationToken cancellationToken);
}
