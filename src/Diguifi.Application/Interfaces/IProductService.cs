using Diguifi.Application.Common;
using Diguifi.Application.DTOs.Products;

namespace Diguifi.Application.Interfaces;

public interface IProductService
{
    Task<IReadOnlyCollection<ProductResponse>> GetProductsAsync(Guid? userId, CancellationToken cancellationToken, bool includeInactive = false);
    Task<ProductResponse?> GetByIdAsync(string productId, CancellationToken cancellationToken);
    Task<Result<ProductResponse>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken);
    Task<Result<ProductResponse>> UpdateAsync(string id, UpdateProductRequest request, CancellationToken cancellationToken);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken cancellationToken);
}
