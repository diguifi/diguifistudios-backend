using Diguifi.Application.DTOs.Notifications;
using Diguifi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diguifi.Api.Controllers;

[ApiController]
[Route("api/my/notifications")]
[Authorize]
public sealed class MyNotificationsController(INotificationService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<NotificationResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var userId = Program.TryGetUserId(User);
        if (userId is null) return Unauthorized();
        return Ok(await service.GetAllAsync(userId.Value, cancellationToken));
    }

    [HttpGet("check")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Check(CancellationToken cancellationToken)
    {
        var userId = Program.TryGetUserId(User);
        if (userId is null) return Unauthorized();
        return Ok(await service.HasUnreadAsync(userId.Value, cancellationToken));
    }

    [HttpPut("read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead([FromBody] MarkNotificationsReadRequest request, CancellationToken cancellationToken)
    {
        var userId = Program.TryGetUserId(User);
        if (userId is null) return Unauthorized();

        await service.MarkAllAsReadAsync(request.Ids, userId.Value, cancellationToken);
        return NoContent();
    }
}
