using Diguifi.Application.Interfaces;

namespace Diguifi.Infrastructure.Services;

public sealed class StripeCheckoutGatewayStub : IStripeCheckoutGateway
{
    public Task<StripeCheckoutSessionResult> CreateCheckoutSessionAsync(StripeCheckoutSessionRequest request, CancellationToken cancellationToken)
        => Task.FromResult(new StripeCheckoutSessionResult
        {
            CheckoutUrl = $"{request.ReturnUrl}&stubSession={request.OrderId}",
            CheckoutSessionId = $"cs_test_{request.OrderId:N}",
            PaymentIntentId = $"pi_test_{request.OrderId:N}"
        });
}
