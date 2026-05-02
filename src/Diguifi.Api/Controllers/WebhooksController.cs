using Diguifi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Diguifi.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public sealed class WebhooksController(IStripeWebhookService stripeWebhookService) : ControllerBase
{
    [HttpPost("stripe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Stripe(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].ToString();

        var result = await stripeWebhookService.ProcessAsync(payload, signature, cancellationToken);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }
}
