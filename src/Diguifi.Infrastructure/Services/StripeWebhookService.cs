using Diguifi.Application.Common;
using Diguifi.Application.Interfaces;
using Diguifi.Domain.Entities;
using Diguifi.Domain.Enums;
using Diguifi.Infrastructure.Options;
using Diguifi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Diguifi.Infrastructure.Services;

public sealed class StripeWebhookService(
    AppDbContext dbContext,
    IOptions<StripeOptions> stripeOptions) : IStripeWebhookService
{
    private readonly StripeOptions _stripeOptions = stripeOptions.Value;

    public async Task<Result<bool>> ProcessAsync(string payload, string signature, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(signature))
        {
            return Result<bool>.Failure("invalid_signature", "Assinatura do webhook nao informada.");
        }

        var webhookSecrets = GetWebhookSecrets();
        if (webhookSecrets.Count == 0)
        {
            return Result<bool>.Failure("missing_webhook_secret", "Stripe:WebhookSecret ou Stripe:CliWebhookSecret nao configurado.");
        }

        Event stripeEvent;
        try
        {
            stripeEvent = ConstructEvent(payload, signature, webhookSecrets);
        }
        catch (StripeException)
        {
            return Result<bool>.Failure("invalid_signature", "Assinatura do webhook da Stripe invalida.");
        }

        var existing = await dbContext.WebhookEvents
            .FirstOrDefaultAsync(x => x.Provider == "stripe" && x.ExternalEventId == stripeEvent.Id, cancellationToken);
        if (existing is not null)
        {
            existing.Status = WebhookEventStatus.Duplicate;
            existing.ProcessedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<bool>.Success(true);
        }

        var webhookEvent = new WebhookEvent
        {
            Provider = "stripe",
            ExternalEventId = stripeEvent.Id,
            EventType = stripeEvent.Type,
            Payload = payload
        };

        dbContext.WebhookEvents.Add(webhookEvent);

        var sessionId = TryReadSessionId(stripeEvent);
        var paymentIntentId = TryReadPaymentIntentId(stripeEvent);

        Order? order = null;
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            order = await dbContext.Orders.FirstOrDefaultAsync(x => x.StripeCheckoutSessionId == sessionId, cancellationToken);
        }

        if (order is null && !string.IsNullOrWhiteSpace(paymentIntentId))
        {
            order = await dbContext.Orders.FirstOrDefaultAsync(x => x.StripePaymentIntentId == paymentIntentId, cancellationToken);
        }

        if (order is null && TryReadOrderId(stripeEvent) is { } orderId)
        {
            order = await dbContext.Orders.FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);
        }

        if (order is null && stripeEvent.Data.Object is Subscription stripeSubscription)
        {
            order = await dbContext.Orders.FirstOrDefaultAsync(x => x.StripeSubscriptionId == stripeSubscription.Id, cancellationToken);
        }

        if (order is not null)
        {
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                order.StripeCheckoutSessionId = sessionId;
            }

            if (!string.IsNullOrWhiteSpace(paymentIntentId))
            {
                order.StripePaymentIntentId = paymentIntentId;
            }

            if (stripeEvent.Data.Object is Session completedSession)
            {
                if (!string.IsNullOrWhiteSpace(completedSession.SubscriptionId))
                    order.StripeSubscriptionId = completedSession.SubscriptionId;

                if (!string.IsNullOrWhiteSpace(completedSession.CustomerId))
                {
                    var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == order.UserId, cancellationToken);
                    if (user is not null && string.IsNullOrWhiteSpace(user.StripeCustomerId))
                        user.StripeCustomerId = completedSession.CustomerId;
                }
            }

            ApplyOrderTransition(order, stripeEvent.Type);
        }

        webhookEvent.Status = WebhookEventStatus.Processed;
        webhookEvent.ProcessedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    private static string? TryReadSessionId(Event stripeEvent)
    {
        return stripeEvent.Data.Object switch
        {
            Session session => session.Id,
            _ => null
        };
    }

    private static string? TryReadPaymentIntentId(Event stripeEvent)
    {
        return stripeEvent.Data.Object switch
        {
            Session session => session.PaymentIntentId,
            PaymentIntent paymentIntent => paymentIntent.Id,
            Charge charge => charge.PaymentIntentId,
            _ => null
        };
    }

    private IReadOnlyList<string> GetWebhookSecrets()
    {
        var secrets = new List<string>(2);

        if (!string.IsNullOrWhiteSpace(_stripeOptions.WebhookSecret))
        {
            secrets.Add(_stripeOptions.WebhookSecret);
        }

        if (!string.IsNullOrWhiteSpace(_stripeOptions.CliWebhookSecret))
        {
            secrets.Add(_stripeOptions.CliWebhookSecret);
        }

        return secrets;
    }

    private static Event ConstructEvent(string payload, string signature, IReadOnlyList<string> webhookSecrets)
    {
        StripeException? lastException = null;

        foreach (var webhookSecret in webhookSecrets)
        {
            try
            {
                // Stripe CLI sends events with the account's API version (2026-03-25.dahlia),
                // which may differ from Stripe.net's current version (2026-04-22.dahlia).
                // HMAC signature is still fully validated regardless.
                return EventUtility.ConstructEvent(payload, signature, webhookSecret, throwOnApiVersionMismatch: false);
            }
            catch (StripeException ex)
            {
                lastException = ex;
            }
        }

        throw lastException ?? new StripeException("Unable to validate Stripe webhook signature.");
    }

    private static Guid? TryReadOrderId(Event stripeEvent)
    {
        var orderId = stripeEvent.Data.Object switch
        {
            Session session => session.Metadata?.GetValueOrDefault("orderId"),
            PaymentIntent paymentIntent => paymentIntent.Metadata?.GetValueOrDefault("orderId"),
            Charge charge => charge.Metadata?.GetValueOrDefault("orderId"),
            _ => null
        };

        return Guid.TryParse(orderId, out var parsedOrderId) ? parsedOrderId : null;
    }

    private static void ApplyOrderTransition(Order order, string eventType)
    {
        switch (eventType)
        {
            case "checkout.session.completed":
            case "payment_intent.succeeded":
                order.Status = OrderStatus.Paid;
                order.PaidAt = DateTimeOffset.UtcNow;
                order.CancelledAt = null;
                break;
            case "checkout.session.expired":
                order.Status = OrderStatus.Expired;
                order.CancelledAt = DateTimeOffset.UtcNow;
                break;
            case "payment_intent.payment_failed":
                order.Status = OrderStatus.Failed;
                break;
            case "charge.refunded":
                order.Status = OrderStatus.Refunded;
                break;
            case "customer.subscription.deleted":
                order.Status = OrderStatus.Cancelled;
                order.CancelledAt = DateTimeOffset.UtcNow;
                break;
        }
    }
}
