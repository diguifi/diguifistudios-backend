using System.Globalization;
using Diguifi.Application.Interfaces;
using Stripe;
using Stripe.Checkout;

namespace Diguifi.Infrastructure.Services;

public sealed class StripeCheckoutGateway(IStripeClient stripeClient) : IStripeCheckoutGateway
{
    public async Task<StripeCheckoutSessionResult> CreateCheckoutSessionAsync(StripeCheckoutSessionRequest request, CancellationToken cancellationToken)
    {
        var sessionService = new SessionService(stripeClient);

        var mode = "payment";
        if (!string.IsNullOrWhiteSpace(request.StripePriceId))
        {
            var priceService = new PriceService(stripeClient);
            var price = await priceService.GetAsync(request.StripePriceId, cancellationToken: cancellationToken);
            mode = price.Type == "recurring" ? "subscription" : "payment";
        }

        var options = new SessionCreateOptions
        {
            Mode = mode,
            SuccessUrl = request.ReturnUrl,
            CancelUrl = request.CancelUrl,
            CustomerEmail = request.CustomerEmail,
            ClientReferenceId = request.OrderId.ToString("N"),
            Metadata = new Dictionary<string, string>
            {
                ["orderId"] = request.OrderId.ToString(),
                ["productId"] = request.ProductId
            }
        };

        if (!string.IsNullOrWhiteSpace(request.StripePriceId))
        {
            options.LineItems =
            [
                new SessionLineItemOptions
                {
                    Price = request.StripePriceId,
                    Quantity = 1
                }
            ];
        }
        else
        {
            options.LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = request.Currency.ToLowerInvariant(),
                        UnitAmount = ToMinorUnit(request.Amount),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = request.ProductName
                        }
                    }
                }
            ];
        }

        if (mode == "payment")
        {
            options.PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    ["orderId"] = request.OrderId.ToString(),
                    ["productId"] = request.ProductId
                }
            };
        }
        else
        {
            options.SubscriptionData = new SessionSubscriptionDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    ["orderId"] = request.OrderId.ToString(),
                    ["productId"] = request.ProductId
                }
            };
        }

        var session = await sessionService.CreateAsync(options, cancellationToken: cancellationToken);

        return new StripeCheckoutSessionResult
        {
            CheckoutUrl = session.Url ?? string.Empty,
            CheckoutSessionId = session.Id,
            PaymentIntentId = session.PaymentIntentId
        };
    }

    private static long ToMinorUnit(decimal amount)
        => long.Parse(decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
}
