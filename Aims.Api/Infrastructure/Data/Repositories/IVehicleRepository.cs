using Aims.Api.Domain.Entities;

namespace Aims.Api.Infrastructure.Data.Repositories;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid orgId, Guid id, CancellationToken ct);
    Task<Vehicle?> GetByIdForUpdateAsync(Guid orgId, Guid id, CancellationToken ct);

    Task<(IReadOnlyList<Vehicle> items, int total)> ListAsync(
        Guid orgId,
        string? q,
        bool? isActive,
        int skip,
        int take,
        CancellationToken ct);

    Task<bool> ExistsByVehicleCodeAsync(Guid orgId, string vehicleCode, CancellationToken ct);
    Task<bool> ExistsByVinAsync(Guid orgId, string vin, CancellationToken ct);
    Task<bool> ExistsByPlateAsync(Guid orgId, string plateNumber, CancellationToken ct);

    Task AddAsync(Vehicle vehicle, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
