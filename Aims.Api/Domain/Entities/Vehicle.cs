namespace Aims.Api.Domain.Entities;

public sealed class Vehicle
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Tenant boundary
    public Guid OrgId { get; set; }
    public Organization Org { get; set; } = null!;

    // Business identifiers
    public string VehicleCode { get; set; } = string.Empty; // required, unique per org
    public string DisplayName { get; set; } = string.Empty; // required

    public string? Vin { get; set; }
    public string? PlateNumber { get; set; }

    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public int? ModelYear { get; set; }

    public bool IsActive { get; set; } = true;

    // Audit
    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public Guid? UpdatedByUserId { get; set; }
    public User? UpdatedByUser { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
