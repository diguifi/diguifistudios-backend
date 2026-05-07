using System.Security.Cryptography;
using System.Text;

namespace Diguifi.Tests.Unit.Helpers;

internal static class StripeWebhookHelper
{
    internal static string Sign(string payload, string secret)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signed = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signed));
        var hex = Convert.ToHexString(hash).ToLowerInvariant();
        return $"t={timestamp},v1={hex}";
    }

    internal static string SessionCompletedPayload(string eventId, string sessionId, string orderId, string? paymentIntentId = null) => $$"""
        {
          "id": "{{eventId}}",
          "object": "event",
          "api_version": "2026-03-25.dahlia",
          "created": 1700000000,
          "type": "checkout.session.completed",
          "data": {
            "object": {
              "id": "{{sessionId}}",
              "object": "checkout.session",
              "payment_intent": {{(paymentIntentId is null ? "null" : $"\"{paymentIntentId}\"")}},
              "metadata": { "orderId": "{{orderId}}", "productId": "prod-1" },
              "payment_status": "paid",
              "status": "complete",
              "mode": "payment",
              "livemode": false,
              "amount_total": 1000,
              "currency": "brl"
            }
          }
        }
        """;

    internal static string SessionExpiredPayload(string eventId, string sessionId, string orderId) => $$"""
        {
          "id": "{{eventId}}",
          "object": "event",
          "api_version": "2026-03-25.dahlia",
          "created": 1700000000,
          "type": "checkout.session.expired",
          "data": {
            "object": {
              "id": "{{sessionId}}",
              "object": "checkout.session",
              "payment_intent": null,
              "metadata": { "orderId": "{{orderId}}", "productId": "prod-1" },
              "payment_status": "unpaid",
              "status": "expired",
              "mode": "payment",
              "livemode": false,
              "amount_total": 1000,
              "currency": "brl"
            }
          }
        }
        """;

    internal static string PaymentIntentSucceededPayload(string eventId, string paymentIntentId, string orderId) => $$"""
        {
          "id": "{{eventId}}",
          "object": "event",
          "api_version": "2026-03-25.dahlia",
          "created": 1700000000,
          "type": "payment_intent.succeeded",
          "data": {
            "object": {
              "id": "{{paymentIntentId}}",
              "object": "payment_intent",
              "metadata": { "orderId": "{{orderId}}", "productId": "prod-1" },
              "status": "succeeded",
              "amount": 1000,
              "currency": "brl",
              "livemode": false
            }
          }
        }
        """;

    internal static string PaymentIntentFailedPayload(string eventId, string paymentIntentId, string orderId) => $$"""
        {
          "id": "{{eventId}}",
          "object": "event",
          "api_version": "2026-03-25.dahlia",
          "created": 1700000000,
          "type": "payment_intent.payment_failed",
          "data": {
            "object": {
              "id": "{{paymentIntentId}}",
              "object": "payment_intent",
              "metadata": { "orderId": "{{orderId}}", "productId": "prod-1" },
              "status": "requires_payment_method",
              "amount": 1000,
              "currency": "brl",
              "livemode": false
            }
          }
        }
        """;

    internal static string ChargeRefundedPayload(string eventId, string chargeId, string paymentIntentId, string orderId) => $$"""
        {
          "id": "{{eventId}}",
          "object": "event",
          "api_version": "2026-03-25.dahlia",
          "created": 1700000000,
          "type": "charge.refunded",
          "data": {
            "object": {
              "id": "{{chargeId}}",
              "object": "charge",
              "payment_intent": "{{paymentIntentId}}",
              "metadata": { "orderId": "{{orderId}}", "productId": "prod-1" },
              "amount": 1000,
              "currency": "brl",
              "refunded": true,
              "livemode": false
            }
          }
        }
        """;

    internal static string SubscriptionDeletedPayload(string eventId, string subscriptionId) => $$"""
        {
          "id": "{{eventId}}",
          "object": "event",
          "api_version": "2026-03-25.dahlia",
          "created": 1700000000,
          "type": "customer.subscription.deleted",
          "data": {
            "object": {
              "id": "{{subscriptionId}}",
              "object": "subscription",
              "customer": "cus_test",
              "status": "canceled",
              "current_period_start": 1700000000,
              "current_period_end": 1703000000,
              "livemode": false,
              "items": {
                "object": "list",
                "data": [],
                "has_more": false,
                "total_count": 0,
                "url": "/v1/subscription_items"
              }
            }
          }
        }
        """;

    internal static string SessionCompletedWithSubscriptionPayload(
        string eventId, string sessionId, string orderId,
        string? subscriptionId = null, string? customerId = null) => $$"""
        {
          "id": "{{eventId}}",
          "object": "event",
          "api_version": "2026-03-25.dahlia",
          "created": 1700000000,
          "type": "checkout.session.completed",
          "data": {
            "object": {
              "id": "{{sessionId}}",
              "object": "checkout.session",
              "subscription": {{(subscriptionId is null ? "null" : $"\"{subscriptionId}\"")}},
              "customer": {{(customerId is null ? "null" : $"\"{customerId}\"")}},
              "payment_intent": null,
              "metadata": { "orderId": "{{orderId}}", "productId": "prod-1" },
              "payment_status": "paid",
              "status": "complete",
              "mode": "subscription",
              "livemode": false,
              "amount_total": 1000,
              "currency": "brl"
            }
          }
        }
        """;

    internal static string UnknownEventPayload(string eventId) => $$"""
        {
          "id": "{{eventId}}",
          "object": "event",
          "api_version": "2026-03-25.dahlia",
          "created": 1700000000,
          "type": "customer.created",
          "data": {
            "object": {
              "id": "cus_test",
              "object": "customer",
              "email": "test@example.com",
              "livemode": false
            }
          }
        }
        """;
}
