using Diguifi.Domain.Enums;

namespace Diguifi.Application.DTOs.Products;

public sealed class CreateProductRequest
{
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ProductCategory Category { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "BRL";
    public string? StripeProductId { get; set; }
    public string? StripePriceId { get; set; }
    public bool IsActive { get; set; } = true;
}
