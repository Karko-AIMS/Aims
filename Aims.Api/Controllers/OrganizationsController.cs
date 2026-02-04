using Aims.Api.Contracts.Organizations;
using Aims.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aims.Api.Controllers;

[ApiController]
[Route("api/organizations")]
[Authorize(Roles = "InternalAdmin")]
public sealed class OrganizationsController : ControllerBase
{
    private readonly OrganizationService _svc;

    public OrganizationsController(OrganizationService svc) => _svc = svc;

    // GET /api/organizations?q=&isActive=&skip=&take=
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? q,
        [FromQuery] bool? isActive,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var items = await _svc.ListAsync(q, isActive, skip, take, ct);
        return Ok(items);
    }

    // GET /api/organizations/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var dto = await _svc.GetByIdAsync(id, ct);
        if (dto is null)
            return NotFound(new { code = "ORG_404", message = "Organization not found" });

        return Ok(dto);
    }

    // POST /api/organizations
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationRequest req, CancellationToken ct = default)
    {
        var (ok, code, message, dto) = await _svc.CreateAsync(req, ct);

        if (!ok)
        {
            if (code == "ORG_409")
                return Conflict(new { code, message });

            return BadRequest(new { code = code ?? "ORG_400", message = message ?? "Invalid request" });
        }

        return Created($"/api/organizations/{dto!.id}", dto);
    }

    // PUT /api/organizations/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOrganizationRequest req, CancellationToken ct = default)
    {
        var (ok, code, message, dto) = await _svc.UpdateAsync(id, req, ct);

        if (!ok)
        {
            return code switch
            {
                "ORG_404" => NotFound(new { code, message }),
                "ORG_409" => Conflict(new { code, message }),
                _ => BadRequest(new { code = code ?? "ORG_400", message = message ?? "Invalid request" })
            };
        }

        return Ok(dto);
    }

    // DELETE /api/organizations/{id} (soft delete)
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken ct = default)
    {
        var (ok, code, message) = await _svc.SoftDeleteAsync(id, ct);

        if (!ok)
            return NotFound(new { code = code ?? "ORG_404", message = message ?? "Organization not found" });

        return NoContent();
    }

    // POST /api/organizations/{id}/restore
    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        var (ok, code, message) = await _svc.RestoreAsync(id, ct);

        if (!ok)
            return NotFound(new { code = code ?? "ORG_404", message = message ?? "Organization not found" });

        return Ok(new { message = "restored" });
    }
}
