using Aims.Api.Contracts.Vehicles;
using Aims.Api.Infrastructure.Auth;
using Aims.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aims.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
public sealed class VehiclesController : ControllerBase
{
    private readonly VehicleService _svc;

    public VehiclesController(VehicleService svc) => _svc = svc;

    // GET /api/vehicles?q=&isActive=&skip=&take=
    // Viewer, Operator만 조회 가능 (InternalAdmin 제외)
    [HttpGet]
    [Authorize(Roles = "Operator,Viewer")]
    public async Task<IActionResult> List(
        [FromQuery] string? q,
        [FromQuery] bool? isActive,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var orgId = User.GetOrgIdRequired();
        var result = await _svc.ListAsync(orgId, q, isActive, skip, take, ct);
        return Ok(result);
    }

    // GET /api/vehicles/{id}
    // Viewer, Operator만 조회 가능
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Operator,Viewer")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var orgId = User.GetOrgIdRequired();
        var v = await _svc.GetByIdAsync(orgId, id, ct);

        if (v is null)
            return NotFound(new { code = "VEH_404", message = "Vehicle not found" });

        return Ok(v);
    }

    // POST /api/vehicles
    // Operator만 생성 가능
    [HttpPost]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> Create(
        [FromBody] CreateVehicleRequest req,
        CancellationToken ct = default)
    {
        var orgId = User.GetOrgIdRequired();
        var userId = User.GetUserId();

        var (ok, code, message, dto) = await _svc.CreateAsync(orgId, userId, req, ct);

        if (!ok)
        {
            if (code == "VEH_409")
                return Conflict(new { code, message });

            return BadRequest(new
            {
                code = code ?? "VEH_400",
                message = message ?? "Invalid request"
            });
        }

        return Created($"/api/vehicles/{dto!.id}", dto);
    }

    // PUT /api/vehicles/{id}
    // Operator만 수정 가능
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateVehicleRequest req,
        CancellationToken ct = default)
    {
        var orgId = User.GetOrgIdRequired();
        var userId = User.GetUserId();

        var (ok, code, message, dto) =
            await _svc.UpdateAsync(orgId, userId, id, req, ct);

        if (!ok)
        {
            return code switch
            {
                "VEH_404" => NotFound(new { code, message }),
                "VEH_409" => Conflict(new { code, message }),
                _ => BadRequest(new
                {
                    code = code ?? "VEH_400",
                    message = message ?? "Invalid request"
                })
            };
        }

        return Ok(dto);
    }

    // DELETE /api/vehicles/{id} (soft delete)
    // Operator만 삭제 가능
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken ct = default)
    {
        var orgId = User.GetOrgIdRequired();
        var userId = User.GetUserId();

        var (ok, code, message) =
            await _svc.SoftDeleteAsync(orgId, userId, id, ct);

        if (!ok)
        {
            return NotFound(new
            {
                code = code ?? "VEH_404",
                message = message ?? "Vehicle not found"
            });
        }

        return NoContent();
    }

    // POST /api/vehicles/{id}/restore
    // Operator만 복구 가능
    [HttpPost("{id:guid}/restore")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        var orgId = User.GetOrgIdRequired();
        var userId = User.GetUserId();

        var (ok, code, message) =
            await _svc.RestoreAsync(orgId, userId, id, ct);

        if (!ok)
        {
            return NotFound(new
            {
                code = code ?? "VEH_404",
                message = message ?? "Vehicle not found"
            });
        }

        return Ok(new { message = "restored" });
    }
}
