using Diguifi.Application.Interfaces;
using Diguifi.Domain.Enums;
using Diguifi.Infrastructure.Options;
using Diguifi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Diguifi.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public sealed class OrdersController(
    AppDbContext dbContext,
    IOrderService orderService,
    IOptions<FrontendOptions> frontendOptions) : ControllerBase
{
    private readonly FrontendOptions _frontend = frontendOptions.Value;

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var userId = Program.TryGetUserId(User);
        if (userId is null) return Unauthorized();

        var rows = await dbContext.Orders
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                ProductName = x.Product != null ? x.Product.Name : x.ProductId,
                ProductCategory = x.Product != null ? x.Product.Category : (ProductCategory?)null,
                BundleType = dbContext.Bundles
                    .Where(b => b.ProductId == x.ProductId)
                    .Select(b => (BundleType?)b.BundleType)
                    .FirstOrDefault(),
                x.Status,
                x.Amount,
                x.Currency,
                x.CreatedAt,
                x.PaidAt,
                x.CancelAtPeriodEnd
            })
            .ToListAsync(cancellationToken);

        return Ok(rows.Select(x => new
        {
            id = x.Id,
            productName = x.ProductName,
            productCategory = x.ProductCategory?.ToString().ToLowerInvariant() ?? "unknown",
            bundleType = x.BundleType?.ToString().ToLowerInvariant(),
            status = x.Status.ToString().ToLowerInvariant(),
            amount = x.Amount,
            currency = x.Currency,
            createdAt = x.CreatedAt,
            paidAt = x.PaidAt,
            cancelAtPeriodEnd = x.CancelAtPeriodEnd
        }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var row = await dbContext.Orders
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.ProductId,
                ProductCategory = x.Product != null ? x.Product.Category : (ProductCategory?)null,
                BundleType = dbContext.Bundles
                    .Where(b => b.ProductId == x.ProductId)
                    .Select(b => (BundleType?)b.BundleType)
                    .FirstOrDefault(),
                x.Status,
                x.Amount,
                x.Currency,
                x.CreatedAt,
                x.PaidAt,
                x.CancelledAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null) return NotFound();

        return Ok(new
        {
            id = row.Id,
            userId = row.UserId,
            productId = row.ProductId,
            productCategory = row.ProductCategory?.ToString().ToLowerInvariant() ?? "unknown",
            bundleType = row.BundleType?.ToString().ToLowerInvariant(),
            status = row.Status.ToString().ToLowerInvariant(),
            amount = row.Amount,
            currency = row.Currency,
            createdAt = row.CreatedAt,
            paidAt = row.PaidAt,
            cancelledAt = row.CancelledAt
        });
    }

    [HttpPost("{orderId:guid}/cancel-subscription")]
    public async Task<IActionResult> CancelSubscription(Guid orderId, CancellationToken cancellationToken)
    {
        var userId = Program.TryGetUserId(User);
        if (userId is null) return Unauthorized();

        var returnUrl = $"{_frontend.BaseUrl}/#/orders";
        var result = await orderService.CancelSubscriptionAsync(orderId, userId.Value, returnUrl, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error!.Code == "order_not_found") return NotFound(result.Error);
            if (result.Error!.Code == "already_cancelling") return Conflict(result.Error);
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpGet("{orderId:guid}/bundle-download")]
    public async Task<IActionResult> BundleDownload(Guid orderId, CancellationToken cancellationToken)
    {
        var userId = Program.TryGetUserId(User);
        if (userId is null) return Unauthorized();

        var result = await orderService.GetBundleDownloadAsync(orderId, userId.Value, cancellationToken);

        if (!result.IsSuccess)
            return result.Error!.Code == "order_not_found"
                ? NotFound(result.Error)
                : BadRequest(result.Error);

        return Ok(result.Value);
    }
}
