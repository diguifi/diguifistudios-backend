using Diguifi.Domain.Entities;
using Diguifi.Domain.Enums;
using Diguifi.Infrastructure.Services;
using Diguifi.Tests.Unit.Helpers;
using FluentAssertions;
using Xunit;

namespace Diguifi.Tests.Unit;

public sealed class PurchaseServiceTests
{
    [Fact]
    public async Task GetPurchasesForUserAsync_NoOrders_ReturnsEmpty()
    {
        await using var db = DbContextFactory.Create();
        var sut = new PurchaseService(db);

        var result = await sut.GetPurchasesForUserAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPurchasesForUserAsync_ReturnsOnlyOrdersForSpecificUser()
    {
        await using var db = DbContextFactory.Create();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        db.Orders.AddRange(
            BuildOrder(userId, "prod-1"),
            BuildOrder(userId, "prod-2"),
            BuildOrder(otherUserId, "prod-3")
        );
        await db.SaveChangesAsync();

        var sut = new PurchaseService(db);
        var result = await sut.GetPurchasesForUserAsync(userId, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(r => r.Should().NotBeNull());
    }

    [Fact]
    public async Task GetPurchasesForUserAsync_MapsFieldsCorrectly()
    {
        await using var db = DbContextFactory.Create();
        var userId = Guid.NewGuid();
        var order = BuildOrder(userId, "prod-1", OrderStatus.Paid, 99.90m, "BRL");
        order.PaidAt = DateTimeOffset.UtcNow;
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var sut = new PurchaseService(db);
        var result = await sut.GetPurchasesForUserAsync(userId, CancellationToken.None);

        var purchase = result.Single();
        purchase.Id.Should().Be(order.Id.ToString());
        purchase.ProductId.Should().Be("prod-1");
        purchase.Status.Should().Be("paid");
        purchase.Amount.Should().Be(99.90m);
        purchase.Currency.Should().Be("BRL");
        purchase.PurchasedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPurchasesForUserAsync_ReturnsOrderedByCreatedAtDescending()
    {
        await using var db = DbContextFactory.Create();
        var userId = Guid.NewGuid();

        var older = BuildOrder(userId, "prod-1");
        older.CreatedAt = DateTimeOffset.UtcNow.AddDays(-2);

        var newer = BuildOrder(userId, "prod-2");
        newer.CreatedAt = DateTimeOffset.UtcNow.AddDays(-1);

        db.Orders.AddRange(older, newer);
        await db.SaveChangesAsync();

        var sut = new PurchaseService(db);
        var result = await sut.GetPurchasesForUserAsync(userId, CancellationToken.None);

        result.First().Id.Should().Be(newer.Id.ToString());
    }

    [Fact]
    public async Task GetPurchasesForUserAsync_StatusMappedToLowercase()
    {
        await using var db = DbContextFactory.Create();
        var userId = Guid.NewGuid();
        db.Orders.Add(BuildOrder(userId, "prod-1", OrderStatus.Refunded));
        await db.SaveChangesAsync();

        var sut = new PurchaseService(db);
        var result = await sut.GetPurchasesForUserAsync(userId, CancellationToken.None);

        result.Single().Status.Should().Be("refunded");
    }

    private static Order BuildOrder(Guid userId, string productId,
        OrderStatus status = OrderStatus.Pending, decimal amount = 10m, string currency = "BRL") => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        ProductId = productId,
        Status = status,
        Amount = amount,
        Currency = currency
    };
}
