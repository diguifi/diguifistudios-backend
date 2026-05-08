using Diguifi.Application.DTOs.Notifications;
using Diguifi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diguifi.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize(Roles = "admin")]
public sealed class NotificationsController(INotificationService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<NotificationResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? userId, CancellationToken cancellationToken)
        => Ok(await service.GetAllAsync(userId, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<NotificationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var notification = await service.GetByIdAsync(id, cancellationToken);
        return notification is null ? NotFound() : Ok(notification);
    }

    [HttpPost]
    [ProducesResponseType<NotificationResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateNotificationRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<NotificationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateNotificationRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
            return result.Error?.Code == "notification_not_found" ? NotFound(result.Error) : BadRequest(result.Error);
        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.DeleteAsync(id, cancellationToken);
        if (!result.IsSuccess)
            return result.Error?.Code == "notification_not_found" ? NotFound(result.Error) : BadRequest(result.Error);
        return NoContent();
    }
}
