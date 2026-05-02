using Diguifi.Application.Common;
using Diguifi.Application.DTOs.Checkout;

namespace Diguifi.Application.Interfaces;

public interface ICheckoutService
{
    Task<Result<CheckoutSessionResponse>> CreateSessionAsync(Guid userId, CreateCheckoutSessionRequest request, CancellationToken cancellationToken);
}
