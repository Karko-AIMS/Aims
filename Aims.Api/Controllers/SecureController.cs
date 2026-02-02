using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Aims.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SecureController : ControllerBase
{
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
        var role = User.FindFirstValue(ClaimTypes.Role);
        return Ok(new { message = "ok", email, role });
    }

    [HttpGet("admin-only")]
    [Authorize(Roles = "InternalAdmin")]
    public IActionResult AdminOnly()
        => Ok(new { message = "ok (admin)" });
}