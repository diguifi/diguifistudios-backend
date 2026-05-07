using Diguifi.Application.Interfaces;
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

        var orders = await dbContext.Orders
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                id = x.Id,
                productName = x.Product != null ? x.Product.Name : x.ProductId,
                productCategory = x.Product != null ? x.Product.Category.ToString().ToLowerInvariant() : "unknown",
                status = x.Status.ToString().ToLowerInvariant(),
                amount = x.Amount,
                currency = x.Currency,
                createdAt = x.CreatedAt,
                paidAt = x.PaidAt
            })
            .ToListAsync(cancellationToken);

        return Ok(orders);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                id = x.Id,
                userId = x.UserId,
                productId = x.ProductId,
                productCategory = x.Product != null ? x.Product.Category.ToString().ToLowerInvariant() : "unknown",
                status = x.Status.ToString().ToLowerInvariant(),
                amount = x.Amount,
                currency = x.Currency,
                createdAt = x.CreatedAt,
                paidAt = x.PaidAt,
                cancelledAt = x.CancelledAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost("{orderId:guid}/cancel-subscription")]
    public async Task<IActionResult> CancelSubscription(Guid orderId, CancellationToken cancellationToken)
    {
        var userId = Program.TryGetUserId(User);
        if (userId is null) return Unauthorized();

        var returnUrl = $"{_frontend.BaseUrl}/#/orders";
        var result = await orderService.CancelSubscriptionAsync(orderId, userId.Value, returnUrl, cancellationToken);

        if (!result.IsSuccess)
            return result.Error!.Code == "order_not_found"
                ? NotFound(result.Error)
                : BadRequest(result.Error);

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
