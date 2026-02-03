namespace Aims.Api.Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public Guid OrgId { get; set; }

    public UserRole Role { get; set; } = UserRole.Viewer;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public Organization Org { get; set; } = null!;

    public List<Vehicle> CreatedVehicles { get; set; } = new();
    public List<Vehicle> UpdatedVehicles { get; set; } = new();

}
