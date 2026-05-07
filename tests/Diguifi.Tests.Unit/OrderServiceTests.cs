using Diguifi.Application.DTOs.Orders;
using Diguifi.Application.Interfaces;
using Diguifi.Domain.Entities;
using Diguifi.Domain.Enums;
using Diguifi.Infrastructure.Persistence;
using Diguifi.Infrastructure.Services;
using Diguifi.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace Diguifi.Tests.Unit;

public sealed class OrderServiceTests
{
    // ── CancelSubscriptionAsync ──────────────────────────────────────────────

    [Fact]
    public async Task CancelSubscriptionAsync_ValidSubscriptionOrder_ReturnsPortalUrl()
    {
        await using var db = DbContextFactory.Create();
        var (user, order) = await SeedSubscriptionOrder(db, subscriptionId: "sub_123", customerId: "cus_456");

        var result = await new OrderService(db, PortalGatewayReturning("https://billing.stripe.com/session/test").Object)
            .CancelSubscriptionAsync(order.Id, user.Id, "https://app.example.com/#/orders", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PortalUrl.Should().Be("https://billing.stripe.com/session/test");
    }

    [Fact]
    public async Task CancelSubscriptionAsync_OrderBelongsToAnotherUser_ReturnsOrderNotFound()
    {
        await using var db = DbContextFactory.Create();
        var (_, order) = await SeedSubscriptionOrder(db);

        var result = await new OrderService(db, new Mock<IStripeBillingPortalGateway>().Object)
            .CancelSubscriptionAsync(order.Id, Guid.NewGuid(), "https://return.url", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("order_not_found");
    }

    [Fact]
    public async Task CancelSubscriptionAsync_OrderStatusNotPaid_ReturnsOrderNotPaid()
    {
        await using var db = DbContextFactory.Create();
        var (user, order) = await SeedSubscriptionOrder(db, status: OrderStatus.Pending);

        var result = await new OrderService(db, new Mock<IStripeBillingPortalGateway>().Object)
            .CancelSubscriptionAsync(order.Id, user.Id, "https://return.url", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("order_not_paid");
    }

    [Fact]
    public async Task CancelSubscriptionAsync_ProductCategoryIsNotSubscription_ReturnsNotSubscription()
    {
        await using var db = DbContextFactory.Create();
        var (user, order) = await SeedOrderWithCategory(db, ProductCategory.Bundle, customerId: "cus_123");

        var result = await new OrderService(db, new Mock<IStripeBillingPortalGateway>().Object)
            .CancelSubscriptionAsync(order.Id, user.Id, "https://return.url", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("not_subscription");
    }

    [Fact]
    public async Task CancelSubscriptionAsync_ValidOrder_CallsGatewayWithCorrectCustomerIdAndSubscriptionId()
    {
        await using var db = DbContextFactory.Create();
        var (user, order) = await SeedSubscriptionOrder(db, subscriptionId: "sub_abc", customerId: "cus_xyz");

        var gateway = PortalGatewayReturning("https://billing.stripe.com/session/x");
        await new OrderService(db, gateway.Object)
            .CancelSubscriptionAsync(order.Id, user.Id, "https://return.url", CancellationToken.None);

        gateway.Verify(x => x.CreatePortalSessionAsync(
            "cus_xyz", "sub_abc", "https://return.url", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelSubscriptionAsync_UserHasNoStripeCustomerId_ReturnsNoCustomer()
    {
        await using var db = DbContextFactory.Create();
        var (user, order) = await SeedSubscriptionOrder(db, subscriptionId: "sub_123", customerId: null);

        var result = await new OrderService(db, new Mock<IStripeBillingPortalGateway>().Object)
            .CancelSubscriptionAsync(order.Id, user.Id, "https://return.url", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("no_customer");
    }

    // ── GetBundleDownloadAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetBundleDownloadAsync_ValidOrderWithBundle_ReturnsDownloadUrlAndFileName()
    {
        await using var db = DbContextFactory.Create();
        var (user, product, order) = await SeedBundleOrder(db);
        db.Bundles.Add(new Bundle
        {
            ProductId = product.Id,
            DriveUrl = "https://drive.google.com/file/abc",
            FileName = "diguifi-bundle-v1.zip"
        });
        await db.SaveChangesAsync();

        var result = await new OrderService(db, new Mock<IStripeBillingPortalGateway>().Object)
            .GetBundleDownloadAsync(order.Id, user.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DownloadUrl.Should().Be("https://drive.google.com/file/abc");
        result.Value!.FileName.Should().Be("diguifi-bundle-v1.zip");
    }

    [Fact]
    public async Task GetBundleDownloadAsync_OrderBelongsToAnotherUser_ReturnsOrderNotFound()
    {
        await using var db = DbContextFactory.Create();
        var (_, _, order) = await SeedBundleOrder(db);

        var result = await new OrderService(db, new Mock<IStripeBillingPortalGateway>().Object)
            .GetBundleDownloadAsync(order.Id, Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("order_not_found");
    }

    [Fact]
    public async Task GetBundleDownloadAsync_OrderStatusNotPaid_ReturnsOrderNotPaid()
    {
        await using var db = DbContextFactory.Create();
        var (user, _, order) = await SeedBundleOrder(db, status: OrderStatus.Pending);

        var result = await new OrderService(db, new Mock<IStripeBillingPortalGateway>().Object)
            .GetBundleDownloadAsync(order.Id, user.Id, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("order_not_paid");
    }

    [Fact]
    public async Task GetBundleDownloadAsync_ProductCategoryIsNotBundle_ReturnsNotBundle()
    {
        await using var db = DbContextFactory.Create();
        var (user, order) = await SeedOrderWithCategory(db, ProductCategory.Subscription, customerId: null);

        var result = await new OrderService(db, new Mock<IStripeBillingPortalGateway>().Object)
            .GetBundleDownloadAsync(order.Id, user.Id, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("not_bundle");
    }

    [Fact]
    public async Task GetBundleDownloadAsync_BundleNotConfigured_ReturnsBundleNotConfigured()
    {
        await using var db = DbContextFactory.Create();
        var (user, _, order) = await SeedBundleOrder(db);

        var result = await new OrderService(db, new Mock<IStripeBillingPortalGateway>().Object)
            .GetBundleDownloadAsync(order.Id, user.Id, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("bundle_not_configured");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Mock<IStripeBillingPortalGateway> PortalGatewayReturning(string url)
    {
        var mock = new Mock<IStripeBillingPortalGateway>();
        mock.Setup(x => x.CreatePortalSessionAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(url);
        return mock;
    }

    private static async Task<(User user, Order order)> SeedSubscriptionOrder(
        AppDbContext db,
        string subscriptionId = "sub_test",
        string? customerId = "cus_test",
        OrderStatus status = OrderStatus.Paid)
    {
        var user = new User { Email = "u@example.com", Name = "U", FirstName = "U", StripeCustomerId = customerId };
        var product = new Product
        {
            Id = "p-sub", Slug = "sub", Name = "Pro Plan", Description = "D",
            Price = 29.90m, Currency = "BRL", Category = ProductCategory.Subscription
        };
        var order = new Order
        {
            UserId = user.Id, ProductId = product.Id, Status = status,
            Amount = 29.90m, Currency = "BRL", StripeSubscriptionId = subscriptionId
        };
        db.Users.Add(user);
        db.Products.Add(product);
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return (user, order);
    }

    private static async Task<(User user, Order order)> SeedOrderWithCategory(
        AppDbContext db, ProductCategory category, string? customerId)
    {
        var user = new User { Email = "u@example.com", Name = "U", FirstName = "U", StripeCustomerId = customerId };
        var product = new Product
        {
            Id = $"p-{(int)category}", Slug = "s", Name = "P", Description = "D",
            Price = 10m, Currency = "BRL", Category = category
        };
        var order = new Order
        {
            UserId = user.Id, ProductId = product.Id, Status = OrderStatus.Paid,
            Amount = 10m, Currency = "BRL"
        };
        db.Users.Add(user);
        db.Products.Add(product);
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return (user, order);
    }

    private static async Task<(User user, Product product, Order order)> SeedBundleOrder(
        AppDbContext db, OrderStatus status = OrderStatus.Paid)
    {
        var user = new User { Email = "u@example.com", Name = "U", FirstName = "U" };
        var product = new Product
        {
            Id = "p-bnd", Slug = "bundle", Name = "Diguifi Bundle", Description = "D",
            Price = 49.90m, Currency = "BRL", Category = ProductCategory.Bundle
        };
        var order = new Order
        {
            UserId = user.Id, ProductId = product.Id, Status = status,
            Amount = 49.90m, Currency = "BRL"
        };
        db.Users.Add(user);
        db.Products.Add(product);
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return (user, product, order);
    }
}
