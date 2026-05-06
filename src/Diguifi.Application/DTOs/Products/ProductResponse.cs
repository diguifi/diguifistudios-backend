namespace Diguifi.Application.DTOs.Products;

public sealed class ProductResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsPurchased { get; set; }
}
