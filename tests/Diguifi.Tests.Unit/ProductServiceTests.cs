using Diguifi.Application.DTOs.Products;
using Diguifi.Domain.Entities;
using Diguifi.Domain.Enums;
using Diguifi.Infrastructure.Services;
using Diguifi.Tests.Unit.Helpers;
using FluentAssertions;
using Xunit;

namespace Diguifi.Tests.Unit;

public sealed class ProductServiceTests
{
    // ── GetProductsAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetProductsAsync_ReturnsOnlyActiveProducts()
    {
        await using var db = DbContextFactory.Create();
        db.Products.AddRange(
            BuildProduct("p1", isActive: true),
            BuildProduct("p2", isActive: false)
        );
        await db.SaveChangesAsync();

        var sut = new ProductService(db);
        var result = await sut.GetProductsAsync(null, CancellationToken.None);

        result.Should().ContainSingle(p => p.Id == "p1");
    }

    [Fact]
    public async Task GetProductsAsync_WithoutUserId_IsPurchasedIsFalse()
    {
        await using var db = DbContextFactory.Create();
        db.Products.Add(BuildProduct("p1", isActive: true));
        await db.SaveChangesAsync();

        var sut = new ProductService(db);
        var result = await sut.GetProductsAsync(null, CancellationToken.None);

        result.Single().IsPurchased.Should().BeFalse();
    }

    [Fact]
    public async Task GetProductsAsync_UserHasPaidOrder_IsPurchasedIsTrue()
    {
        await using var db = DbContextFactory.Create();
        var userId = Guid.NewGuid();
        db.Products.Add(BuildProduct("p1", isActive: true));
        db.Orders.Add(BuildOrder(userId, "p1", OrderStatus.Paid));
        await db.SaveChangesAsync();

        var sut = new ProductService(db);
        var result = await sut.GetProductsAsync(userId, CancellationToken.None);

        result.Single().IsPurchased.Should().BeTrue();
    }

    [Fact]
    public async Task GetProductsAsync_UserHasPendingOrder_IsPurchasedIsFalse()
    {
        await using var db = DbContextFactory.Create();
        var userId = Guid.NewGuid();
        db.Products.Add(BuildProduct("p1", isActive: true));
        db.Orders.Add(BuildOrder(userId, "p1", OrderStatus.Pending));
        await db.SaveChangesAsync();

        var sut = new ProductService(db);
        var result = await sut.GetProductsAsync(userId, CancellationToken.None);

        result.Single().IsPurchased.Should().BeFalse();
    }

    [Fact]
    public async Task GetProductsAsync_MapsFieldsCorrectly()
    {
        await using var db = DbContextFactory.Create();
        db.Products.Add(new Product
        {
            Id = "p1",
            Slug = "my-slug",
            Name = "My Product",
            Description = "A product",
            Category = ProductCategory.Service,
            Price = 49.90m,
            Currency = "BRL",
            IsActive = true,
            StripeProductId = "stripe_prod",
            StripePriceId = "stripe_price"
        });
        await db.SaveChangesAsync();

        var sut = new ProductService(db);
        var result = await sut.GetProductsAsync(null, CancellationToken.None);

        var p = result.Single();
        p.Slug.Should().Be("my-slug");
        p.Name.Should().Be("My Product");
        p.Category.Should().Be("service");
        p.Price.Should().Be(49.90m);
        p.Currency.Should().Be("BRL");
        p.StripeProductId.Should().Be("stripe_prod");
        p.StripePriceId.Should().Be("stripe_price");
    }

    // ── GetByIdAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsProduct()
    {
        await using var db = DbContextFactory.Create();
        db.Products.Add(BuildProduct("p1", isActive: true));
        await db.SaveChangesAsync();

        var sut = new ProductService(db);
        var result = await sut.GetByIdAsync("p1", CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be("p1");
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        await using var db = DbContextFactory.Create();
        var sut = new ProductService(db);

        var result = await sut.GetByIdAsync("nonexistent", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_InactiveProduct_StillReturnsIt()
    {
        await using var db = DbContextFactory.Create();
        db.Products.Add(BuildProduct("p1", isActive: false));
        await db.SaveChangesAsync();

        var sut = new ProductService(db);
        var result = await sut.GetByIdAsync("p1", CancellationToken.None);

        result.Should().NotBeNull();
    }

    // ── CreateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_NewSlug_CreatesProductAndReturnsSuccess()
    {
        await using var db = DbContextFactory.Create();
        var sut = new ProductService(db);
        var request = new CreateProductRequest
        {
            Slug = "new-product",
            Name = "New Product",
            Description = "Desc",
            Category = ProductCategory.Bundle,
            Price = 10m,
            Currency = "BRL",
            IsActive = true
        };

        var result = await sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("New Product");
        db.Products.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateAsync_DuplicateSlug_ReturnsFailure()
    {
        await using var db = DbContextFactory.Create();
        db.Products.Add(BuildProduct("p1", isActive: true, slug: "taken-slug"));
        await db.SaveChangesAsync();

        var sut = new ProductService(db);
        var request = new CreateProductRequest { Slug = "taken-slug", Name = "X", Description = "Y", Price = 1m, Currency = "BRL" };

        var result = await sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("slug_conflict");
    }

    [Fact]
    public async Task CreateAsync_AssignsGeneratedId()
    {
        await using var db = DbContextFactory.Create();
        var sut = new ProductService(db);
        var request = new CreateProductRequest { Slug = "s", Name = "N", Description = "D", Price = 1m, Currency = "BRL", IsActive = true };

        var result = await sut.CreateAsync(request, CancellationToken.None);

        result.Value!.Id.Should().NotBeNullOrWhiteSpace();
    }

    // ── UpdateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ExistingProduct_AppliesChanges()
    {
        await using var db = DbContextFactory.Create();
        db.Products.Add(BuildProduct("p1", isActive: true));
        await db.SaveChangesAsync();

        var sut = new ProductService(db);
        var result = await sut.UpdateAsync("p1", new UpdateProductRequest { Name = "Updated Name", Price = 99m }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated Name");
        result.Value.Price.Should().Be(99m);
    }

    [Fact]
    public async Task UpdateAsync_OnlyNonNullFieldsAreChanged()
    {
        await using var db = DbContextFactory.Create();
        var product = BuildProduct("p1", isActive: true);
        product.Description = "Original description";
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var sut = new ProductService(db);
        await sut.UpdateAsync("p1", new UpdateProductRequest { Name = "New Name" }, CancellationToken.None);

        var stored = db.Products.Single();
        stored.Description.Should().Be("Original description");
        stored.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ReturnsFailure()
    {
        await using var db = DbContextFactory.Create();
        var sut = new ProductService(db);

        var result = await sut.UpdateAsync("nope", new UpdateProductRequest { Name = "X" }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("product_not_found");
    }

    [Fact]
    public async Task UpdateAsync_CanToggleIsActive()
    {
        await using var db = DbContextFactory.Create();
        db.Products.Add(BuildProduct("p1", isActive: true));
        await db.SaveChangesAsync();

        var sut = new ProductService(db);
        var result = await sut.UpdateAsync("p1", new UpdateProductRequest { IsActive = false }, CancellationToken.None);

        result.Value!.IsActive.Should().BeFalse();
        db.Products.Single().IsActive.Should().BeFalse();
    }

    // ── DeleteAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_NoAssociatedOrders_DeletesProduct()
    {
        await using var db = DbContextFactory.Create();
        db.Products.Add(BuildProduct("p1", isActive: true));
        await db.SaveChangesAsync();

        var sut = new ProductService(db);
        var result = await sut.DeleteAsync("p1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.Products.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_WithAssociatedOrders_ReturnsFailure()
    {
        await using var db = DbContextFactory.Create();
        db.Products.Add(BuildProduct("p1", isActive: true));
        db.Orders.Add(BuildOrder(Guid.NewGuid(), "p1", OrderStatus.Paid));
        await db.SaveChangesAsync();

        var sut = new ProductService(db);
        var result = await sut.DeleteAsync("p1", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("product_has_orders");
        db.Products.Should().ContainSingle();
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ReturnsFailure()
    {
        await using var db = DbContextFactory.Create();
        var sut = new ProductService(db);

        var result = await sut.DeleteAsync("nope", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("product_not_found");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static Product BuildProduct(string id, bool isActive, string slug = "slug") => new()
    {
        Id = id,
        Slug = slug,
        Name = "Product " + id,
        Description = "Desc",
        Category = ProductCategory.Bundle,
        Price = 10m,
        Currency = "BRL",
        IsActive = isActive
    };

    private static Order BuildOrder(Guid userId, string productId, OrderStatus status) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        ProductId = productId,
        Status = status,
        Amount = 10m,
        Currency = "BRL"
    };
}
