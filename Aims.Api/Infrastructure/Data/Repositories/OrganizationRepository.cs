using Aims.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aims.Api.Infrastructure.Data.Repositories;

public sealed class OrganizationRepository : IOrganizationRepository
{
    private readonly AimsDbContext _db;

    public OrganizationRepository(AimsDbContext db) => _db = db;

    public async Task<List<Organization>> ListAsync(string? q, bool? isActive, int skip, int take, CancellationToken ct)
    {
        var query = _db.Organizations.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim().ToLower();
            query = query.Where(o => o.Name.ToLower().Contains(s));
        }

        if (isActive.HasValue)
            query = query.Where(o => o.IsActive == isActive.Value);

        return await query
            .OrderBy(o => o.Name)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync(ct);
    }

    public Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct)
        => _db.Organizations.FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<bool> ExistsByNameAsync(string nameNormalized, CancellationToken ct)
        => _db.Organizations.AnyAsync(o => o.Name.ToLower() == nameNormalized, ct);

    public Task<bool> ExistsByNameExceptIdAsync(Guid id, string nameNormalized, CancellationToken ct)
        => _db.Organizations.AnyAsync(o => o.Id != id && o.Name.ToLower() == nameNormalized, ct);

    public async Task AddAsync(Organization org, CancellationToken ct)
        => await _db.Organizations.AddAsync(org, ct);

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}
