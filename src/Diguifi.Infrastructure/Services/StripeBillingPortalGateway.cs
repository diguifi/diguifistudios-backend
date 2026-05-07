using Diguifi.Application.Interfaces;
using Stripe;

namespace Diguifi.Infrastructure.Services;

public sealed class StripeBillingPortalGateway(IStripeClient stripeClient) : IStripeBillingPortalGateway
{
    public async Task<string> CreatePortalSessionAsync(
        string customerId, string subscriptionId, string returnUrl, CancellationToken cancellationToken)
    {
        var service = new Stripe.BillingPortal.SessionService(stripeClient);

        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = customerId,
            ReturnUrl = returnUrl,
            FlowData = new Stripe.BillingPortal.SessionFlowDataOptions
            {
                Type = "subscription_cancel",
                SubscriptionCancel = new Stripe.BillingPortal.SessionFlowDataSubscriptionCancelOptions
                {
                    Subscription = subscriptionId
                }
            }
        };

        var session = await service.CreateAsync(options, cancellationToken: cancellationToken);
        return session.Url ?? string.Empty;
    }

    public async Task<string?> GetCustomerIdFromSubscriptionAsync(
        string subscriptionId, CancellationToken cancellationToken)
    {
        var service = new SubscriptionService(stripeClient);
        var subscription = await service.GetAsync(subscriptionId, cancellationToken: cancellationToken);
        return subscription?.CustomerId;
    }

    public async Task<(string? CustomerId, string? SubscriptionId)> GetIdsFromCheckoutSessionAsync(
        string sessionId, CancellationToken cancellationToken)
    {
        var service = new Stripe.Checkout.SessionService(stripeClient);
        var session = await service.GetAsync(sessionId, cancellationToken: cancellationToken);
        return (session?.CustomerId, session?.SubscriptionId);
    }
}
