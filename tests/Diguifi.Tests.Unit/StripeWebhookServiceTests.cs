using Diguifi.Domain.Entities;
using Diguifi.Domain.Enums;
using Diguifi.Infrastructure.Options;
using Diguifi.Infrastructure.Persistence;
using Diguifi.Infrastructure.Services;
using Diguifi.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Diguifi.Tests.Unit;

public sealed class StripeWebhookServiceTests
{
    private const string WebhookSecret = "whsec_test_secret_key_for_unit_tests_long";

    // ── Signature validation ────────────────────────────────────────────────

    [Fact]
    public async Task ProcessAsync_EmptySignature_ReturnsFailure()
    {
        await using var db = DbContextFactory.Create();
        var sut = BuildSut(db);

        var result = await sut.ProcessAsync("{}", "", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("invalid_signature");
    }

    [Fact]
    public async Task ProcessAsync_NoWebhookSecretConfigured_ReturnsFailure()
    {
        await using var db = DbContextFactory.Create();
        var sut = BuildSut(db, webhookSecret: "", cliSecret: "");

        var result = await sut.ProcessAsync("{}", "t=1,v1=abc", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("missing_webhook_secret");
    }

    [Fact]
    public async Task ProcessAsync_InvalidSignature_ReturnsFailure()
    {
        await using var db = DbContextFactory.Create();
        var sut = BuildSut(db);
        var payload = StripeWebhookHelper.SessionCompletedPayload("evt_1", "cs_1", Guid.NewGuid().ToString());

        var result = await sut.ProcessAsync(payload, "t=1,v1=badsig", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("invalid_signature");
    }

    [Fact]
    public async Task ProcessAsync_CliSecretValidates_WhenMainSecretFails()
    {
        await using var db = DbContextFactory.Create();
        const string cliSecret = "whsec_cli_secret_key_for_unit_tests_long";
        var sut = BuildSut(db, webhookSecret: "whsec_wrong", cliSecret: cliSecret);

        var orderId = await SeedPendingOrder(db, "cs_cli");
        var payload = StripeWebhookHelper.SessionCompletedPayload("evt_cli", "cs_cli", orderId.ToString());
        var sig = StripeWebhookHelper.Sign(payload, cliSecret);

        var result = await sut.ProcessAsync(payload, sig, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    // ── Idempotency ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessAsync_DuplicateEventId_MarksDuplicateAndReturnsSuccess()
    {
        await using var db = DbContextFactory.Create();
        db.WebhookEvents.Add(new WebhookEvent
        {
            Provider = "stripe",
            ExternalEventId = "evt_dup",
            EventType = "checkout.session.completed",
            Payload = "{}"
        });
        await db.SaveChangesAsync();

        var sut = BuildSut(db);
        var payload = StripeWebhookHelper.SessionCompletedPayload("evt_dup", "cs_x", Guid.NewGuid().ToString());
        var sig = StripeWebhookHelper.Sign(payload, WebhookSecret);

        var result = await sut.ProcessAsync(payload, sig, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.WebhookEvents.Count(e => e.Status == WebhookEventStatus.Duplicate).Should().Be(1);
    }

    // ── checkout.session.completed ──────────────────────────────────────────

    [Fact]
    public async Task ProcessAsync_SessionCompleted_FindsOrderBySessionId_MarksAsPaid()
    {
        await using var db = DbContextFactory.Create();
        var orderId = await SeedPendingOrder(db, sessionId: "cs_found");

        var payload = StripeWebhookHelper.SessionCompletedPayload("evt_sc", "cs_found", orderId.ToString());
        var sig = StripeWebhookHelper.Sign(payload, WebhookSecret);

        await BuildSut(db).ProcessAsync(payload, sig, CancellationToken.None);

        db.Orders.Single().Status.Should().Be(OrderStatus.Paid);
        db.Orders.Single().PaidAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessAsync_SessionCompleted_FindsOrderByMetadataOrderId_MarksAsPaid()
    {
        await using var db = DbContextFactory.Create();
        var orderId = await SeedPendingOrder(db, sessionId: null);

        var payload = StripeWebhookHelper.SessionCompletedPayload("evt_meta", "cs_nomatch", orderId.ToString());
        var sig = StripeWebhookHelper.Sign(payload, WebhookSecret);

        await BuildSut(db).ProcessAsync(payload, sig, CancellationToken.None);

        db.Orders.Single().Status.Should().Be(OrderStatus.Paid);
    }

    [Fact]
    public async Task ProcessAsync_SessionCompleted_FindsOrderByPaymentIntentId_MarksAsPaid()
    {
        await using var db = DbContextFactory.Create();
        var orderId = await SeedPendingOrder(db, sessionId: null, paymentIntentId: "pi_linked");

        var payload = StripeWebhookHelper.SessionCompletedPayload("evt_pi", "cs_other", Guid.NewGuid().ToString(), paymentIntentId: "pi_linked");
        var sig = StripeWebhookHelper.Sign(payload, WebhookSecret);

        await BuildSut(db).ProcessAsync(payload, sig, CancellationToken.None);

        db.Orders.Single().Status.Should().Be(OrderStatus.Paid);
    }

    // ── checkout.session.expired ────────────────────────────────────────────

    [Fact]
    public async Task ProcessAsync_SessionExpired_MarksOrderAsExpired()
    {
        await using var db = DbContextFactory.Create();
        var orderId = await SeedPendingOrder(db, sessionId: "cs_exp");

        var payload = StripeWebhookHelper.SessionExpiredPayload("evt_exp", "cs_exp", orderId.ToString());
        var sig = StripeWebhookHelper.Sign(payload, WebhookSecret);

        await BuildSut(db).ProcessAsync(payload, sig, CancellationToken.None);

        db.Orders.Single().Status.Should().Be(OrderStatus.Expired);
        db.Orders.Single().CancelledAt.Should().NotBeNull();
    }

    // ── payment_intent.succeeded ────────────────────────────────────────────

    [Fact]
    public async Task ProcessAsync_PaymentIntentSucceeded_FindsOrderByPaymentIntentId_MarksAsPaid()
    {
        await using var db = DbContextFactory.Create();
        var orderId = await SeedPendingOrder(db, paymentIntentId: "pi_succ");

        var payload = StripeWebhookHelper.PaymentIntentSucceededPayload("evt_pis", "pi_succ", orderId.ToString());
        var sig = StripeWebhookHelper.Sign(payload, WebhookSecret);

        await BuildSut(db).ProcessAsync(payload, sig, CancellationToken.None);

        db.Orders.Single().Status.Should().Be(OrderStatus.Paid);
    }

    // ── payment_intent.payment_failed ───────────────────────────────────────

    [Fact]
    public async Task ProcessAsync_PaymentIntentFailed_MarksOrderAsFailed()
    {
        await using var db = DbContextFactory.Create();
        var orderId = await SeedPendingOrder(db, paymentIntentId: "pi_fail");

        var payload = StripeWebhookHelper.PaymentIntentFailedPayload("evt_pif", "pi_fail", orderId.ToString());
        var sig = StripeWebhookHelper.Sign(payload, WebhookSecret);

        await BuildSut(db).ProcessAsync(payload, sig, CancellationToken.None);

        db.Orders.Single().Status.Should().Be(OrderStatus.Failed);
    }

    // ── charge.refunded ─────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessAsync_ChargeRefunded_MarksOrderAsRefunded()
    {
        await using var db = DbContextFactory.Create();
        var orderId = await SeedPendingOrder(db, paymentIntentId: "pi_ref");

        var payload = StripeWebhookHelper.ChargeRefundedPayload("evt_ref", "ch_ref", "pi_ref", orderId.ToString());
        var sig = StripeWebhookHelper.Sign(payload, WebhookSecret);

        await BuildSut(db).ProcessAsync(payload, sig, CancellationToken.None);

        db.Orders.Single().Status.Should().Be(OrderStatus.Refunded);
    }

    // ── Unknown event ───────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessAsync_UnknownEventType_RecordsEventButLeavesOrderUnchanged()
    {
        await using var db = DbContextFactory.Create();
        var orderId = await SeedPendingOrder(db);

        var payload = StripeWebhookHelper.UnknownEventPayload("evt_unk");
        var sig = StripeWebhookHelper.Sign(payload, WebhookSecret);

        var result = await BuildSut(db).ProcessAsync(payload, sig, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.Orders.Single().Status.Should().Be(OrderStatus.Pending);
        db.WebhookEvents.Should().ContainSingle(e => e.EventType == "customer.created");
    }

    // ── Webhook event recording ─────────────────────────────────────────────

    [Fact]
    public async Task ProcessAsync_Success_RecordsWebhookEventAsProcessed()
    {
        await using var db = DbContextFactory.Create();
        var orderId = await SeedPendingOrder(db, sessionId: "cs_rec");

        var payload = StripeWebhookHelper.SessionCompletedPayload("evt_rec", "cs_rec", orderId.ToString());
        var sig = StripeWebhookHelper.Sign(payload, WebhookSecret);

        await BuildSut(db).ProcessAsync(payload, sig, CancellationToken.None);

        var evt = db.WebhookEvents.Single();
        evt.Status.Should().Be(WebhookEventStatus.Processed);
        evt.ProcessedAt.Should().NotBeNull();
        evt.Provider.Should().Be("stripe");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static StripeWebhookService BuildSut(AppDbContext db,
        string webhookSecret = WebhookSecret, string cliSecret = "")
    {
        var opts = Options.Create(new StripeOptions
        {
            SecretKey = "sk_test_key",
            WebhookSecret = webhookSecret,
            CliWebhookSecret = cliSecret
        });
        return new StripeWebhookService(db, opts);
    }

    private static async Task<Guid> SeedPendingOrder(AppDbContext db,
        string? sessionId = null, string? paymentIntentId = null)
    {
        var user = new User { Email = "u@example.com", Name = "U", FirstName = "U" };
        var product = new Product { Id = "p-wh", Slug = "s", Name = "P", Description = "D", Price = 10m, Currency = "BRL" };
        var order = new Order
        {
            UserId = user.Id,
            ProductId = product.Id,
            Status = OrderStatus.Pending,
            Amount = 10m,
            Currency = "BRL",
            StripeCheckoutSessionId = sessionId,
            StripePaymentIntentId = paymentIntentId
        };
        db.Users.Add(user);
        db.Products.Add(product);
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order.Id;
    }
}
