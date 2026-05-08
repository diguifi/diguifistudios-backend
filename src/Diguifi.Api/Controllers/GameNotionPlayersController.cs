using Diguifi.Application.DTOs.GameNotionPlayers;
using Diguifi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diguifi.Api.Controllers;

[ApiController]
[Route("api/game-notion-players")]
public sealed class GameNotionPlayersController(IGameNotionPlayerService service) : ControllerBase
{
    [Authorize(Roles = "admin")]
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<GameNotionPlayerResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => Ok(await service.GetAllAsync(cancellationToken));

    [Authorize(Roles = "admin")]
    [HttpGet("{playerId}")]
    [ProducesResponseType<GameNotionPlayerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string playerId, CancellationToken cancellationToken)
    {
        var player = await service.GetByIdAsync(playerId, cancellationToken);
        return player is null ? NotFound() : Ok(player);
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    [ProducesResponseType<GameNotionPlayerResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateGameNotionPlayerRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { playerId = result.Value!.PlayerId }, result.Value)
            : BadRequest(result.Error);
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{playerId}")]
    [ProducesResponseType<GameNotionPlayerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string playerId, [FromBody] UpdateGameNotionPlayerRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(playerId, request, cancellationToken);
        if (!result.IsSuccess)
            return result.Error?.Code == "player_not_found" ? NotFound(result.Error) : BadRequest(result.Error);
        return Ok(result.Value);
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{playerId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string playerId, CancellationToken cancellationToken)
    {
        var result = await service.DeleteAsync(playerId, cancellationToken);
        if (!result.IsSuccess)
            return result.Error?.Code == "player_not_found" ? NotFound(result.Error) : BadRequest(result.Error);
        return NoContent();
    }

    [Authorize]
    [HttpPut("me")]
    [ProducesResponseType<GameNotionPlayerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetMyPlayerId([FromBody] SetPlayerIdRequest request, CancellationToken cancellationToken)
    {
        var userId = Program.TryGetUserId(User);
        if (userId is null)
            return Unauthorized();

        var result = await service.SetPlayerIdAsync(userId.Value, request, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(result.Error);
        return Ok(result.Value);
    }
}
