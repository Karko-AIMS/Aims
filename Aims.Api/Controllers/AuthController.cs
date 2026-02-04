using Aims.Api.Domain.Entities;
using Aims.Api.Infrastructure.Data;
using Aims.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Aims.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly AimsDbContext _db;
    private readonly JwtTokenService _jwt;
    private readonly PasswordHasher<User> _hasher;
    private readonly IWebHostEnvironment _env;

    public AuthController(
        AimsDbContext db,
        JwtTokenService jwt,
        PasswordHasher<User> hasher,
        IWebHostEnvironment env)
    {
        _db = db;
        _jwt = jwt;
        _hasher = hasher;
        _env = env;
    }

    // ---------- DTOs ----------
    public sealed record LoginRequest(string Email, string Password);

    // OrgId는 InternalAdmin이면 무시(= null로 저장)
    public sealed record RegisterRequest(
        string Email,
        string Password,
        Guid OrgId,
        UserRole Role);

    // Seed도 OrgId는 받지만 InternalAdmin이므로 실제 저장에서는 무시(null)
    public sealed record SeedDevAdminRequest(string Email, string Password, Guid OrgId);

    // ---------- Helpers ----------
    private static string NormalizeEmail(string? email)
        => (email ?? "").Trim().ToLowerInvariant();

    private static bool IsInternalAdminRole(UserRole role)
        => role == UserRole.InternalAdmin;

    // ---------- API ----------
    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var email = NormalizeEmail(req.Email);
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { code = "AUTH_400", message = "Email and Password are required" });

        var user = await _db.Users.FirstOrDefaultAsync(x => x.IsActive && x.Email.ToLower() == email);
        if (user is null)
            return Unauthorized(new { code = "AUTH_401", message = "Invalid credentials" });

        // Org-bound 유저는 Org 활성 상태 체크
        if (user.OrgId.HasValue)
        {
            var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == user.OrgId.Value);
            if (org is null || !org.IsActive)
                return Unauthorized(new { code = "AUTH_401", message = "Invalid credentials" }); // 정보 노출 방지
        }

        var ok = _hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
        if (ok == PasswordVerificationResult.Failed)
            return Unauthorized(new { code = "AUTH_401", message = "Invalid credentials" });

        var (token, expiresIn) = _jwt.CreateToken(user);
        return Ok(new
        {
            accessToken = token,
            tokenType = "Bearer",
            expiresIn,
            role = user.Role.ToString()
        });
    }

    // POST /api/auth/register
    // 운영 관점: 관리자만 계정 생성 가능하도록 제한
    [HttpPost("register")]
    [Authorize(Roles = "InternalAdmin")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var email = NormalizeEmail(req.Email);
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { code = "AUTH_400", message = "Email and Password are required" });

        // 중복 이메일 방지
        var exists = await _db.Users.AnyAsync(x => x.Email.ToLower() == email);
        if (exists)
            return Conflict(new { code = "AUTH_409", message = "Email already exists" });

        // InternalAdmin이 아니면 OrgId 필수 + Org 존재/활성 검증
        Guid? orgId = null;

        if (!IsInternalAdminRole(req.Role))
        {
            if (req.OrgId == Guid.Empty)
                return BadRequest(new { code = "AUTH_400", message = "OrgId is required for non-admin users" });

            var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == req.OrgId);
            if (org is null)
                return BadRequest(new { code = "AUTH_400", message = "Organization not found" });

            if (!org.IsActive)
                return BadRequest(new { code = "AUTH_400", message = "Organization is inactive" });

            orgId = req.OrgId;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            OrgId = orgId,
            Role = req.Role,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        user.PasswordHash = _hasher.HashPassword(user, req.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Created($"/api/users/{user.Id}", new
        {
            user.Id,
            user.Email,
            user.OrgId,
            role = user.Role.ToString(),
            user.IsActive,
            user.CreatedAtUtc
        });
    }

    // POST /api/auth/seed-dev-admin
    // 개발 환경에서만, 그리고 users 테이블이 비어있을 때만 최초 관리자 생성
    [HttpPost("seed-dev-admin")]
    [AllowAnonymous]
    public async Task<IActionResult> SeedDevAdmin([FromBody] SeedDevAdminRequest req)
    {
        if (!_env.IsDevelopment())
            return NotFound(); // 존재 자체를 숨김

        var hasAny = await _db.Users.AnyAsync();
        if (hasAny)
            return Conflict(new { code = "SEED_409", message = "Users already exist. Seed is disabled." });

        var email = NormalizeEmail(req.Email);
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { code = "SEED_400", message = "Email and Password are required" });

        // InternalAdmin은 org에 소속되지 않는다 (OrgId = null)
        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            OrgId = null,
            Role = UserRole.InternalAdmin,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        admin.PasswordHash = _hasher.HashPassword(admin, req.Password);

        _db.Users.Add(admin);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Dev admin seeded",
            admin.Id,
            admin.Email,
            admin.OrgId,
            role = admin.Role.ToString()
        });
    }

    // GET /api/auth/me
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        var email = User.FindFirstValue(ClaimTypes.Email)
                  ?? User.FindFirstValue(JwtRegisteredClaimNames.Email)
                  ?? User.FindFirstValue("email");

        var role = User.FindFirstValue(ClaimTypes.Role);

        var orgId = User.FindFirstValue("orgId"); // InternalAdmin이면 null

        return Ok(new { userId, email, role, orgId });
    }
}
