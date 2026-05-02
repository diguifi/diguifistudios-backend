using Diguifi.Application.DTOs.Auth;
using Diguifi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diguifi.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    private const string RefreshCookieName = "diguifi_rt";

    [HttpPost("google")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Google([FromBody] GoogleAuthRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.AuthenticateWithGoogleAsync(
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers["User-Agent"].ToString(),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return BadRequest(result.Error);
        }

        SetRefreshCookie(result.Value.RefreshToken);
        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    [ProducesResponseType<RefreshResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshCookieName] ?? string.Empty;
        var result = await authService.RefreshAsync(
            refreshToken,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers["User-Agent"].ToString(),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return Unauthorized(result.Error);
        }

        SetRefreshCookie(result.Value.RefreshToken);
        return Ok(result.Value);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshCookieName] ?? string.Empty;
        await authService.LogoutAsync(refreshToken, cancellationToken);
        Response.Cookies.Delete(RefreshCookieName);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userId = Program.TryGetUserId(User);
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await authService.GetCurrentUserAsync(userId.Value, cancellationToken);
        return !result.IsSuccess || result.Value is null
            ? NotFound(result.Error)
            : Ok(result.Value);
    }

    private void SetRefreshCookie(string refreshToken)
    {
        Response.Cookies.Append(RefreshCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });
    }
}
