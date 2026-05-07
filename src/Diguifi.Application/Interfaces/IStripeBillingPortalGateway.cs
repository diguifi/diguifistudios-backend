namespace Diguifi.Application.Interfaces;

public interface IStripeBillingPortalGateway
{
    Task<string> CreatePortalSessionAsync(
        string customerId, string subscriptionId, string returnUrl, CancellationToken cancellationToken);

    Task<string?> GetCustomerIdFromSubscriptionAsync(
        string subscriptionId, CancellationToken cancellationToken);

    Task<(string? CustomerId, string? SubscriptionId)> GetIdsFromCheckoutSessionAsync(
        string sessionId, CancellationToken cancellationToken);
}
