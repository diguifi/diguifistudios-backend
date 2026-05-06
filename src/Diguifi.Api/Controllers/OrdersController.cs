using Diguifi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Diguifi.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public sealed class OrdersController(AppDbContext dbContext) : ControllerBase
{
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
}
