using Diguifi.Application.Interfaces;
using Diguifi.Infrastructure.Services;
using Diguifi.Tests.Unit.Helpers;
using FluentAssertions;
using Stripe;
using Xunit;

namespace Diguifi.Tests.Unit;

public sealed class StripeCheckoutGatewayTests
{
    private const string OneTimePriceJson = """
        {
          "id": "price_onetime",
          "object": "price",
          "type": "one_time",
          "currency": "brl",
          "unit_amount": 1000,
          "livemode": false,
          "active": true
        }
        """;

    private const string RecurringPriceJson = """
        {
          "id": "price_recurring",
          "object": "price",
          "type": "recurring",
          "currency": "brl",
          "unit_amount": 1000,
          "recurring": { "interval": "month", "interval_count": 1 },
          "livemode": false,
          "active": true
        }
        """;

    private const string PaymentSessionJson = """
        {
          "id": "cs_payment_test",
          "object": "checkout.session",
          "mode": "payment",
          "url": "https://checkout.stripe.com/pay/cs_payment_test",
          "payment_intent": "pi_test_123",
          "livemode": false,
          "status": "open",
          "payment_status": "unpaid"
        }
        """;

    private const string SubscriptionSessionJson = """
        {
          "id": "cs_sub_test",
          "object": "checkout.session",
          "mode": "subscription",
          "url": "https://checkout.stripe.com/pay/cs_sub_test",
          "subscription": "sub_test_123",
          "livemode": false,
          "status": "open",
          "payment_status": "unpaid"
        }
        """;

    [Fact]
    public async Task CreateCheckoutSessionAsync_WithOneTimeStripePriceId_ReturnsCheckoutUrl()
    {
        var handler = new FakeStripeHttpHandler();
        handler.AddStub("prices", "GET", OneTimePriceJson);
        handler.AddStub("checkout/sessions", "POST", PaymentSessionJson);

        var sut = BuildGateway(handler);
        var result = await sut.CreateCheckoutSessionAsync(BuildRequest(stripePriceId: "price_onetime"), CancellationToken.None);

        result.CheckoutUrl.Should().Be("https://checkout.stripe.com/pay/cs_payment_test");
        result.CheckoutSessionId.Should().Be("cs_payment_test");
        result.PaymentIntentId.Should().Be("pi_test_123");
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_WithRecurringStripePriceId_ReturnsSubscriptionSession()
    {
        var handler = new FakeStripeHttpHandler();
        handler.AddStub("prices", "GET", RecurringPriceJson);
        handler.AddStub("checkout/sessions", "POST", SubscriptionSessionJson);

        var sut = BuildGateway(handler);
        var result = await sut.CreateCheckoutSessionAsync(BuildRequest(stripePriceId: "price_recurring"), CancellationToken.None);

        result.CheckoutUrl.Should().Be("https://checkout.stripe.com/pay/cs_sub_test");
        result.CheckoutSessionId.Should().Be("cs_sub_test");
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_WithoutStripePriceId_UsesInlinePriceData()
    {
        var handler = new FakeStripeHttpHandler();
        handler.AddStub("checkout/sessions", "POST", PaymentSessionJson);

        var sut = BuildGateway(handler);
        var result = await sut.CreateCheckoutSessionAsync(BuildRequest(stripePriceId: null), CancellationToken.None);

        result.CheckoutUrl.Should().Be("https://checkout.stripe.com/pay/cs_payment_test");
        result.CheckoutSessionId.Should().Be("cs_payment_test");
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_RecurringPrice_PaymentIntentIdIsNull()
    {
        var handler = new FakeStripeHttpHandler();
        handler.AddStub("prices", "GET", RecurringPriceJson);
        handler.AddStub("checkout/sessions", "POST", SubscriptionSessionJson);

        var sut = BuildGateway(handler);
        var result = await sut.CreateCheckoutSessionAsync(BuildRequest(stripePriceId: "price_recurring"), CancellationToken.None);

        result.PaymentIntentId.Should().BeNull();
    }

    private static StripeCheckoutGateway BuildGateway(FakeStripeHttpHandler handler)
    {
        var httpClient = new SystemNetHttpClient(new HttpClient(handler));
        var stripeClient = new StripeClient("sk_test_fakekeyfortesting", httpClient: httpClient);
        return new StripeCheckoutGateway(stripeClient);
    }

    private static StripeCheckoutSessionRequest BuildRequest(string? stripePriceId) => new()
    {
        OrderId = Guid.NewGuid(),
        ProductId = "prod-1",
        ProductName = "Test Product",
        Amount = 10m,
        Currency = "BRL",
        StripePriceId = stripePriceId,
        ReturnUrl = "https://example.com/success",
        CancelUrl = "https://example.com/cancel",
        CustomerEmail = "customer@example.com"
    };
}
