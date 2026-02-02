using Microsoft.AspNetCore.Mvc;

namespace Aims.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PingController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { message = "pong", utc = DateTime.UtcNow });
    }
}
