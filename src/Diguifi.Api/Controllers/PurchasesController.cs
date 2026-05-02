using Diguifi.Application.DTOs.Purchases;
using Diguifi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diguifi.Api.Controllers;

[ApiController]
[Route("api/purchases")]
[Authorize]
public sealed class PurchasesController(IPurchaseService purchaseService) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType<IReadOnlyCollection<PurchaseResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var userId = Program.TryGetUserId(User);
        if (userId is null)
        {
            return Unauthorized();
        }

        return Ok(await purchaseService.GetPurchasesForUserAsync(userId.Value, cancellationToken));
    }
}
