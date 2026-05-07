using Diguifi.Application.DTOs.Checkout;
using Diguifi.Application.Interfaces;
using Diguifi.Domain.Entities;
using Diguifi.Domain.Enums;
using Diguifi.Infrastructure.Services;
using Diguifi.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace Diguifi.Tests.Unit;

public sealed class CheckoutServiceTests
{
    private readonly Mock<IStripeCheckoutGateway> _gateway = new();

    [Fact]
    public async Task CreateSessionAsync_UserNotFound_ReturnsFailure()
    {
        await using var db = DbContextFactory.Create();
        var sut = new CheckoutService(db, _gateway.Object);

        var result = await sut.CreateSessionAsync(Guid.NewGuid(), ValidRequest("p1"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("user_not_found");
        _gateway.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateSessionAsync_ProductNotFound_ReturnsFailure()
    {
        await using var db = DbContextFactory.Create();
        db.Users.Add(BuildUser());
        await db.SaveChangesAsync();
        var userId = db.Users.Single().Id;

        var sut = new CheckoutService(db, _gateway.Object);
        var result = await sut.CreateSessionAsync(userId, ValidRequest("nonexistent"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("product_not_found");
        _gateway.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateSessionAsync_InactiveProduct_ReturnsFailure()
    {
        await using var db = DbContextFactory.Create();
        var user = BuildUser();
        var product = BuildProduct("p1", isActive: false);
        db.Users.Add(user);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var sut = new CheckoutService(db, _gateway.Object);
        var result = await sut.CreateSessionAsync(user.Id, ValidRequest("p1"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("product_not_found");
    }

    [Fact]
    public async Task CreateSessionAsync_ValidRequest_CreatesOrderAndReturnsCheckoutUrl()
    {
        await using var db = DbContextFactory.Create();
        var user = BuildUser();
        var product = BuildProduct("p1", isActive: true);
        db.Users.Add(user);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        _gateway
            .Setup(g => g.CreateCheckoutSessionAsync(It.IsAny<StripeCheckoutSessionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StripeCheckoutSessionResult
            {
                CheckoutUrl = "https://checkout.stripe.com/pay/cs_test",
                CheckoutSessionId = "cs_test_abc",
                PaymentIntentId = "pi_test_abc"
            });

        var sut = new CheckoutService(db, _gateway.Object);
        var result = await sut.CreateSessionAsync(user.Id, ValidRequest("p1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CheckoutUrl.Should().Be("https://checkout.stripe.com/pay/cs_test");
    }

    [Fact]
    public async Task CreateSessionAsync_ValidRequest_SavesOrderWithPendingStatus()
    {
        await using var db = DbContextFactory.Create();
        var user = BuildUser();
        var product = BuildProduct("p1", isActive: true);
        db.Users.Add(user);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        _gateway
            .Setup(g => g.CreateCheckoutSessionAsync(It.IsAny<StripeCheckoutSessionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StripeCheckoutSessionResult
            {
                CheckoutUrl = "https://checkout.stripe.com/test",
                CheckoutSessionId = "cs_session",
                PaymentIntentId = "pi_intent"
            });

        var sut = new CheckoutService(db, _gateway.Object);
        await sut.CreateSessionAsync(user.Id, ValidRequest("p1"), CancellationToken.None);

        var order = db.Orders.Single();
        order.Status.Should().Be(OrderStatus.Pending);
        order.UserId.Should().Be(user.Id);
        order.ProductId.Should().Be("p1");
        order.StripeCheckoutSessionId.Should().Be("cs_session");
        order.StripePaymentIntentId.Should().Be("pi_intent");
    }

    [Fact]
    public async Task CreateSessionAsync_PassesCorrectAmountAndCurrencyToGateway()
    {
        await using var db = DbContextFactory.Create();
        var user = BuildUser();
        var product = new Product
        {
            Id = "p1", Slug = "s", Name = "N", Description = "D",
            Price = 49.90m, Currency = "BRL", IsActive = true
        };
        db.Users.Add(user);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        StripeCheckoutSessionRequest? capturedRequest = null;
        _gateway
            .Setup(g => g.CreateCheckoutSessionAsync(It.IsAny<StripeCheckoutSessionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<StripeCheckoutSessionRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new StripeCheckoutSessionResult { CheckoutUrl = "https://x", CheckoutSessionId = "cs", PaymentIntentId = "pi" });

        var sut = new CheckoutService(db, _gateway.Object);
        await sut.CreateSessionAsync(user.Id, ValidRequest("p1"), CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Amount.Should().Be(49.90m);
        capturedRequest.Currency.Should().Be("BRL");
        capturedRequest.CustomerEmail.Should().Be(user.Email);
    }

    private static CreateCheckoutSessionRequest ValidRequest(string productId) => new()
    {
        ProductId = productId,
        ReturnUrl = "https://example.com/success",
        CancelUrl = "https://example.com/cancel"
    };

    private static User BuildUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "buyer@example.com",
        Name = "Buyer",
        FirstName = "Buyer"
    };

    private static Product BuildProduct(string id, bool isActive) => new()
    {
        Id = id, Slug = id, Name = "Product", Description = "Desc",
        Price = 10m, Currency = "BRL", IsActive = isActive
    };
}
