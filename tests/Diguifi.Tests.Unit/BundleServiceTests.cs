using Diguifi.Application.DTOs.Bundles;
using Diguifi.Domain.Entities;
using Diguifi.Domain.Enums;
using Diguifi.Infrastructure.Persistence;
using Diguifi.Infrastructure.Services;
using Diguifi.Tests.Unit.Helpers;
using FluentAssertions;
using Xunit;

namespace Diguifi.Tests.Unit;

public sealed class BundleServiceTests
{
    // ── UpsertAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpsertAsync_NewBundle_CreatesRecord()
    {
        await using var db = DbContextFactory.Create();
        await SeedBundleProduct(db, "p-bnd-1");

        var result = await new BundleService(db)
            .UpsertAsync("p-bnd-1", new UpsertBundleRequest { DriveUrl = "https://drive.google.com/file/abc", FileName = "course.zip" }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.Bundles.Should().ContainSingle(b => b.ProductId == "p-bnd-1" && b.DriveUrl == "https://drive.google.com/file/abc");
    }

    [Fact]
    public async Task UpsertAsync_ExistingBundle_UpdatesInPlace()
    {
        await using var db = DbContextFactory.Create();
        await SeedBundleProduct(db, "p-bnd-2");
        db.Bundles.Add(new Bundle { ProductId = "p-bnd-2", DriveUrl = "old-url", FileName = "old.zip" });
        await db.SaveChangesAsync();

        await new BundleService(db)
            .UpsertAsync("p-bnd-2", new UpsertBundleRequest { DriveUrl = "new-url", FileName = "new.zip" }, CancellationToken.None);

        db.Bundles.Should().ContainSingle();
        db.Bundles.Single(b => b.ProductId == "p-bnd-2").DriveUrl.Should().Be("new-url");
        db.Bundles.Single(b => b.ProductId == "p-bnd-2").FileName.Should().Be("new.zip");
    }

    [Fact]
    public async Task UpsertAsync_ProductDoesNotExist_ReturnsProductNotFound()
    {
        await using var db = DbContextFactory.Create();

        var result = await new BundleService(db)
            .UpsertAsync("nonexistent", new UpsertBundleRequest { DriveUrl = "url", FileName = "file.zip" }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("product_not_found");
    }

    [Fact]
    public async Task UpsertAsync_ProductCategoryIsNotBundle_ReturnsNotBundle()
    {
        await using var db = DbContextFactory.Create();
        db.Products.Add(new Product { Id = "p-svc", Slug = "s", Name = "P", Description = "D", Price = 10m, Currency = "BRL", Category = ProductCategory.Service });
        await db.SaveChangesAsync();

        var result = await new BundleService(db)
            .UpsertAsync("p-svc", new UpsertBundleRequest { DriveUrl = "url", FileName = "file.zip" }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("not_bundle");
    }

    // ── GetAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_BundleExists_ReturnsBundleDownloadResponse()
    {
        await using var db = DbContextFactory.Create();
        await SeedBundleProduct(db, "p-bnd-3");
        db.Bundles.Add(new Bundle { ProductId = "p-bnd-3", DriveUrl = "https://drive.google.com/x", FileName = "x.zip" });
        await db.SaveChangesAsync();

        var result = await new BundleService(db).GetAsync("p-bnd-3", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DownloadUrl.Should().Be("https://drive.google.com/x");
        result.Value!.FileName.Should().Be("x.zip");
    }

    [Fact]
    public async Task GetAsync_BundleNotFound_ReturnsBundleNotConfigured()
    {
        await using var db = DbContextFactory.Create();
        await SeedBundleProduct(db, "p-bnd-4");

        var result = await new BundleService(db).GetAsync("p-bnd-4", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("bundle_not_configured");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static async Task SeedBundleProduct(AppDbContext db, string productId)
    {
        db.Products.Add(new Product
        {
            Id = productId, Slug = productId, Name = "Bundle Product",
            Description = "D", Price = 49.90m, Currency = "BRL", Category = ProductCategory.Bundle
        });
        await db.SaveChangesAsync();
    }
}
