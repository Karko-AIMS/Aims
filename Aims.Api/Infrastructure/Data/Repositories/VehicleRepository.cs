using Aims.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aims.Api.Infrastructure.Data.Repositories;

public sealed class VehicleRepository : IVehicleRepository
{
    private readonly AimsDbContext _db;

    public VehicleRepository(AimsDbContext db) => _db = db;

    public Task<Vehicle?> GetByIdAsync(Guid orgId, Guid id, CancellationToken ct)
        => _db.Vehicles.AsNoTracking()
            .FirstOrDefaultAsync(v => v.OrgId == orgId && v.Id == id, ct);

    public Task<Vehicle?> GetByIdForUpdateAsync(Guid orgId, Guid id, CancellationToken ct)
        => _db.Vehicles
            .FirstOrDefaultAsync(v => v.OrgId == orgId && v.Id == id, ct);

    public async Task<(IReadOnlyList<Vehicle> items, int total)> ListAsync(
        Guid orgId, string? q, bool? isActive, int skip, int take, CancellationToken ct)
    {
        if (take <= 0) take = 50;
        if (take > 200) take = 200;
        if (skip < 0) skip = 0;

        var query = _db.Vehicles.AsNoTracking().Where(v => v.OrgId == orgId);

        if (isActive.HasValue)
            query = query.Where(v => v.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(v =>
                v.VehicleCode.Contains(q) ||
                v.DisplayName.Contains(q) ||
                (v.Vin != null && v.Vin.Contains(q)) ||
                (v.PlateNumber != null && v.PlateNumber.Contains(q)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(v => v.VehicleCode)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<bool> ExistsByVehicleCodeAsync(Guid orgId, string vehicleCode, CancellationToken ct)
        => _db.Vehicles.AsNoTracking()
            .AnyAsync(v => v.OrgId == orgId && v.VehicleCode == vehicleCode, ct);

    public Task<bool> ExistsByVinAsync(Guid orgId, string vin, CancellationToken ct)
        => _db.Vehicles.AsNoTracking()
            .AnyAsync(v => v.OrgId == orgId && v.Vin == vin, ct);

    public Task<bool> ExistsByPlateAsync(Guid orgId, string plateNumber, CancellationToken ct)
        => _db.Vehicles.AsNoTracking()
            .AnyAsync(v => v.OrgId == orgId && v.PlateNumber == plateNumber, ct);

    public async Task AddAsync(Vehicle vehicle, CancellationToken ct)
        => await _db.Vehicles.AddAsync(vehicle, ct);

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}
