using Aims.Api.Contracts.Vehicles;
using Aims.Api.Domain.Entities;
using Aims.Api.Infrastructure.Data.Repositories;

namespace Aims.Api.Services;

public sealed class VehicleService
{
    private readonly IVehicleRepository _repo;

    public VehicleService(IVehicleRepository repo) => _repo = repo;

    // ---- Helpers ----
    private static string Norm(string? s) => (s ?? "").Trim();
    private static string? NullIfBlank(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    public static VehicleDto ToDto(Vehicle v) => new(
        v.Id,
        v.OrgId,
        v.VehicleCode,
        v.DisplayName,
        v.Vin,
        v.PlateNumber,
        v.Manufacturer,
        v.Model,
        v.ModelYear,
        v.IsActive,
        v.CreatedAtUtc,
        v.UpdatedAtUtc
    );

    // ---- API ----

    public async Task<PagedResult<VehicleDto>> ListAsync(
        Guid orgId, string? q, bool? isActive, int skip, int take, CancellationToken ct)
    {
        var (items, total) = await _repo.ListAsync(orgId, q, isActive, skip, take, ct);
        return new PagedResult<VehicleDto>(items.Select(ToDto).ToList(), total);
    }

    public async Task<VehicleDto?> GetByIdAsync(Guid orgId, Guid id, CancellationToken ct)
    {
        var v = await _repo.GetByIdAsync(orgId, id, ct);
        return v is null ? null : ToDto(v);
    }

    /// <summary>
    /// returns: (ok, code, message, dto)
    /// </summary>
    public async Task<(bool ok, string? code, string? message, VehicleDto? dto)> CreateAsync(
        Guid orgId, Guid actorUserId, CreateVehicleRequest req, CancellationToken ct)
    {
        var vehicleCode = Norm(req.VehicleCode);
        var displayName = Norm(req.DisplayName);

        if (string.IsNullOrWhiteSpace(vehicleCode) || string.IsNullOrWhiteSpace(displayName))
            return (false, "VEH_400", "VehicleCode and DisplayName are required", null);

        var vin = NullIfBlank(req.Vin);
        var plate = NullIfBlank(req.PlateNumber);

        // 중복 체크 (org 범위)
        if (await _repo.ExistsByVehicleCodeAsync(orgId, vehicleCode, ct))
            return (false, "VEH_409", "VehicleCode already exists", null);

        if (vin is not null && await _repo.ExistsByVinAsync(orgId, vin, ct))
            return (false, "VEH_409", "VIN already exists", null);

        if (plate is not null && await _repo.ExistsByPlateAsync(orgId, plate, ct))
            return (false, "VEH_409", "PlateNumber already exists", null);

        var v = new Vehicle
        {
            Id = Guid.NewGuid(),
            OrgId = orgId,
            VehicleCode = vehicleCode,
            DisplayName = displayName,
            Vin = vin,
            PlateNumber = plate,
            Manufacturer = NullIfBlank(req.Manufacturer),
            Model = NullIfBlank(req.Model),
            ModelYear = req.ModelYear,
            IsActive = true,
            CreatedByUserId = actorUserId,
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _repo.AddAsync(v, ct);
        await _repo.SaveChangesAsync(ct);

        return (true, null, null, ToDto(v));
    }

    public async Task<(bool ok, string? code, string? message, VehicleDto? dto)> UpdateAsync(
        Guid orgId, Guid actorUserId, Guid id, UpdateVehicleRequest req, CancellationToken ct)
    {
        var displayName = Norm(req.DisplayName);
        if (string.IsNullOrWhiteSpace(displayName))
            return (false, "VEH_400", "DisplayName is required", null);

        var v = await _repo.GetByIdForUpdateAsync(orgId, id, ct);
        if (v is null)
            return (false, "VEH_404", "Vehicle not found", null);

        var vin = NullIfBlank(req.Vin);
        var plate = NullIfBlank(req.PlateNumber);

        // 중복 체크: 본인 제외가 필요 → 간단히 값이 바뀌는 경우만 체크
        if (vin is not null && !string.Equals(v.Vin, vin, StringComparison.OrdinalIgnoreCase))
        {
            if (await _repo.ExistsByVinAsync(orgId, vin, ct))
                return (false, "VEH_409", "VIN already exists", null);
        }

        if (plate is not null && !string.Equals(v.PlateNumber, plate, StringComparison.OrdinalIgnoreCase))
        {
            if (await _repo.ExistsByPlateAsync(orgId, plate, ct))
                return (false, "VEH_409", "PlateNumber already exists", null);
        }

        v.DisplayName = displayName;
        v.Vin = vin;
        v.PlateNumber = plate;
        v.Manufacturer = NullIfBlank(req.Manufacturer);
        v.Model = NullIfBlank(req.Model);
        v.ModelYear = req.ModelYear;

        if (req.IsActive.HasValue)
            v.IsActive = req.IsActive.Value;

        v.UpdatedByUserId = actorUserId;
        v.UpdatedAtUtc = DateTime.UtcNow;

        await _repo.SaveChangesAsync(ct);
        return (true, null, null, ToDto(v));
    }

    public async Task<(bool ok, string? code, string? message)> SoftDeleteAsync(
        Guid orgId, Guid actorUserId, Guid id, CancellationToken ct)
    {
        var v = await _repo.GetByIdForUpdateAsync(orgId, id, ct);
        if (v is null)
            return (false, "VEH_404", "Vehicle not found");

        v.IsActive = false;
        v.UpdatedByUserId = actorUserId;
        v.UpdatedAtUtc = DateTime.UtcNow;

        await _repo.SaveChangesAsync(ct);
        return (true, null, null);
    }

    public async Task<(bool ok, string? code, string? message)> RestoreAsync(
        Guid orgId, Guid actorUserId, Guid id, CancellationToken ct)
    {
        var v = await _repo.GetByIdForUpdateAsync(orgId, id, ct);
        if (v is null)
            return (false, "VEH_404", "Vehicle not found");

        v.IsActive = true;
        v.UpdatedByUserId = actorUserId;
        v.UpdatedAtUtc = DateTime.UtcNow;

        await _repo.SaveChangesAsync(ct);
        return (true, null, null);
    }
}
