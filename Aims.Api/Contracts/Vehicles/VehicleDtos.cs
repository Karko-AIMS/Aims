using System.ComponentModel.DataAnnotations;

namespace Aims.Api.Contracts.Vehicles;

public sealed record VehicleDto(
    Guid id,
    Guid orgId,
    string vehicleCode,
    string displayName,
    string? vin,
    string? plateNumber,
    string? manufacturer,
    string? model,
    int? modelYear,
    bool isActive,
    DateTime createdAtUtc,
    DateTime? updatedAtUtc
);

public sealed record CreateVehicleRequest(
    [Required, MaxLength(64)] string VehicleCode,
    [Required, MaxLength(120)] string DisplayName,
    [MaxLength(32)] string? Vin,
    [MaxLength(32)] string? PlateNumber,
    [MaxLength(120)] string? Manufacturer,
    [MaxLength(120)] string? Model,
    int? ModelYear
);

public sealed record UpdateVehicleRequest(
    [Required, MaxLength(120)] string DisplayName,
    [MaxLength(32)] string? Vin,
    [MaxLength(32)] string? PlateNumber,
    [MaxLength(120)] string? Manufacturer,
    [MaxLength(120)] string? Model,
    int? ModelYear,
    bool? IsActive
);

public sealed record PagedResult<T>(
    IReadOnlyList<T> items,
    int total
);
