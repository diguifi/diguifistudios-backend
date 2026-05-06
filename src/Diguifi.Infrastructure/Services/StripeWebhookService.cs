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

        if (string.IsNullOrWhiteSpace(_stripeOptions.WebhookSecret))
        {
            return Result<bool>.Failure("missing_webhook_secret", "Stripe:WebhookSecret nao configurado.");
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload, signature, _stripeOptions.WebhookSecret);
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
        }
    }
}
