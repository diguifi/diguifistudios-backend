using System.Text.Json;
using Diguifi.Application.Common;
using Diguifi.Application.Interfaces;
using Diguifi.Domain.Entities;
using Diguifi.Domain.Enums;
using Diguifi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Diguifi.Infrastructure.Services;

public sealed class StripeWebhookService(AppDbContext dbContext) : IStripeWebhookService
{
    public async Task<Result<bool>> ProcessAsync(string payload, string signature, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(signature))
        {
            return Result<bool>.Failure("invalid_signature", "Assinatura do webhook não informada.");
        }

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var eventId = root.GetProperty("id").GetString() ?? string.Empty;
        var eventType = root.GetProperty("type").GetString() ?? string.Empty;

        var existing = await dbContext.WebhookEvents
            .FirstOrDefaultAsync(x => x.Provider == "stripe" && x.ExternalEventId == eventId, cancellationToken);
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
            ExternalEventId = eventId,
            EventType = eventType,
            Payload = payload
        };

        dbContext.WebhookEvents.Add(webhookEvent);

        var sessionId = TryReadSessionId(root);
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            var order = await dbContext.Orders.FirstOrDefaultAsync(x => x.StripeCheckoutSessionId == sessionId, cancellationToken);
            if (order is not null)
            {
                ApplyOrderTransition(order, eventType);
            }
        }

        webhookEvent.Status = WebhookEventStatus.Processed;
        webhookEvent.ProcessedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    private static string? TryReadSessionId(JsonElement root)
    {
        if (!root.TryGetProperty("data", out var data) || !data.TryGetProperty("object", out var @object))
        {
            return null;
        }

        if (@object.TryGetProperty("id", out var objectId))
        {
            return objectId.GetString();
        }

        return null;
    }

    private static void ApplyOrderTransition(Order order, string eventType)
    {
        switch (eventType)
        {
            case "checkout.session.completed":
            case "payment_intent.succeeded":
                order.Status = OrderStatus.Paid;
                order.PaidAt = DateTimeOffset.UtcNow;
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
