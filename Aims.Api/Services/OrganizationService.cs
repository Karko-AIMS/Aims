using Aims.Api.Contracts.Organizations;
using Aims.Api.Domain.Entities;
using Aims.Api.Infrastructure.Data.Repositories;

namespace Aims.Api.Services;

public sealed class OrganizationService
{
    private readonly IOrganizationRepository _repo;

    public OrganizationService(IOrganizationRepository repo) => _repo = repo;

    private static string NormalizeName(string? name)
        => (name ?? "").Trim();

    private static string NormalizeNameKey(string name)
        => name.Trim().ToLowerInvariant();

    private static OrganizationDto ToDto(Organization o)
        => new(
            id: o.Id,
            name: o.Name,
            isActive: o.IsActive,
            createdAtUtc: o.CreatedAtUtc,
            updatedAtUtc: o.UpdatedAtUtc,
            deactivatedAtUtc: o.DeactivatedAtUtc
        );

    public async Task<List<OrganizationDto>> ListAsync(string? q, bool? isActive, int skip, int take, CancellationToken ct)
    {
        var items = await _repo.ListAsync(q, isActive, skip, take, ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<OrganizationDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var org = await _repo.GetByIdAsync(id, ct);
        return org is null ? null : ToDto(org);
    }

    public async Task<(bool ok, string? code, string? message, OrganizationDto? dto)> CreateAsync(CreateOrganizationRequest req, CancellationToken ct)
    {
        var name = NormalizeName(req.Name);
        if (string.IsNullOrWhiteSpace(name))
            return (false, "ORG_400", "Name is required", null);

        if (name.Length > 120)
            return (false, "ORG_400", "Name is too long", null);

        var key = NormalizeNameKey(name);
        if (await _repo.ExistsByNameAsync(key, ct))
            return (false, "ORG_409", "Organization name already exists", null);

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null,
            DeactivatedAtUtc = null
        };

        await _repo.AddAsync(org, ct);
        await _repo.SaveChangesAsync(ct);

        return (true, null, null, ToDto(org));
    }

    public async Task<(bool ok, string? code, string? message, OrganizationDto? dto)> UpdateAsync(Guid id, UpdateOrganizationRequest req, CancellationToken ct)
    {
        var org = await _repo.GetByIdAsync(id, ct);
        if (org is null)
            return (false, "ORG_404", "Organization not found", null);

        var name = NormalizeName(req.Name);
        if (string.IsNullOrWhiteSpace(name))
            return (false, "ORG_400", "Name is required", null);

        if (name.Length > 120)
            return (false, "ORG_400", "Name is too long", null);

        var key = NormalizeNameKey(name);
        if (await _repo.ExistsByNameExceptIdAsync(id, key, ct))
            return (false, "ORG_409", "Organization name already exists", null);

        org.Name = name;
        org.UpdatedAtUtc = DateTime.UtcNow;

        await _repo.SaveChangesAsync(ct);

        return (true, null, null, ToDto(org));
    }

    public async Task<(bool ok, string? code, string? message)> SoftDeleteAsync(Guid id, CancellationToken ct)
    {
        var org = await _repo.GetByIdAsync(id, ct);
        if (org is null)
            return (false, "ORG_404", "Organization not found");

        if (!org.IsActive)
            return (true, null, null);

        org.IsActive = false;
        org.DeactivatedAtUtc = DateTime.UtcNow;
        org.UpdatedAtUtc = DateTime.UtcNow;

        await _repo.SaveChangesAsync(ct);

        return (true, null, null);
    }

    public async Task<(bool ok, string? code, string? message)> RestoreAsync(Guid id, CancellationToken ct)
    {
        var org = await _repo.GetByIdAsync(id, ct);
        if (org is null)
            return (false, "ORG_404", "Organization not found");

        if (org.IsActive)
            return (true, null, null);

        org.IsActive = true;
        org.DeactivatedAtUtc = null;
        org.UpdatedAtUtc = DateTime.UtcNow;

        await _repo.SaveChangesAsync(ct);

        return (true, null, null);
    }
}
