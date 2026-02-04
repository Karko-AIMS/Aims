namespace Aims.Api.Domain.Entities;

public sealed class Organization
{
    public Guid Id { get; set; } // tenant key (orgId)

    public string Name { get; set; } = "Default";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public DateTime? DeactivatedAtUtc { get; set; }

    public List<User> Users { get; set; } = new();
    public List<Vehicle> Vehicles { get; set; } = new();
}
