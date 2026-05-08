using Diguifi.Application.DTOs.Bundles;
using Diguifi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diguifi.Api.Controllers;

[ApiController]
[Route("api/bundle")]
[Authorize(Roles = "admin")]
public sealed class BundlesController(IBundleService bundleService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<BundleResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => Ok(await bundleService.GetAllAsync(cancellationToken));

    [HttpGet("{productId}")]
    [ProducesResponseType<BundleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string productId, CancellationToken cancellationToken)
    {
        var bundle = await bundleService.GetByProductIdAsync(productId, cancellationToken);
        return bundle is null ? NotFound() : Ok(bundle);
    }

    [HttpPut("{productId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Upsert(string productId, [FromBody] UpsertBundleRequest request, CancellationToken cancellationToken)
    {
        var result = await bundleService.UpsertAsync(productId, request, cancellationToken);
        if (!result.IsSuccess)
            return result.Error?.Code == "product_not_found" ? NotFound(result.Error) : BadRequest(result.Error);
        return Ok();
    }

    [HttpDelete("{productId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string productId, CancellationToken cancellationToken)
    {
        var result = await bundleService.DeleteAsync(productId, cancellationToken);
        if (!result.IsSuccess)
            return result.Error?.Code == "bundle_not_found" ? NotFound(result.Error) : BadRequest(result.Error);
        return NoContent();
    }
}
