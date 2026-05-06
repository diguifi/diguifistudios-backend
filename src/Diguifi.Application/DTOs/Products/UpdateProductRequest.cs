using Diguifi.Domain.Enums;

namespace Diguifi.Application.DTOs.Products;

public sealed class UpdateProductRequest
{
    public string? Slug { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public ProductCategory? Category { get; set; }
    public decimal? Price { get; set; }
    public string? Currency { get; set; }
    public string? StripeProductId { get; set; }
    public string? StripePriceId { get; set; }
    public bool? IsActive { get; set; }
}
