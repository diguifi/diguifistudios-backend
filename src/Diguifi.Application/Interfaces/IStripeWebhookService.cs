using Diguifi.Application.Common;

namespace Diguifi.Application.Interfaces;

public interface IStripeWebhookService
{
    Task<Result<bool>> ProcessAsync(string payload, string signature, CancellationToken cancellationToken);
}
