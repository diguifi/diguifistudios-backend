using Microsoft.AspNetCore.Mvc;

namespace Diguifi.Api.Controllers;

[ApiController]
public sealed class PingController : ControllerBase
{
    [HttpGet("/ping")]
    public IActionResult Ping() => Ok("pong");
}
