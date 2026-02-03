using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Aims.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SecureController : ControllerBase
{
    [HttpGet("admin-only")]
    [Authorize(Roles = "InternalAdmin")]
    public IActionResult AdminOnly()
        => Ok(new { message = "ok (admin)" });
}