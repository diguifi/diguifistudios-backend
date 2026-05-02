namespace Diguifi.Application.Interfaces;

public interface IStripeCheckoutGateway
{
    Task<StripeCheckoutSessionResult> CreateCheckoutSessionAsync(StripeCheckoutSessionRequest request, CancellationToken cancellationToken);
}
