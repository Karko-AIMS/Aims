namespace Aims.Api.Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    // InternalAdmin은 Org에 소속되지 않으므로 nullable
    public Guid? OrgId { get; set; }

    public UserRole Role { get; set; } = UserRole.Viewer;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    // OrgId가 null일 수 있으므로 navigation도 nullable로 두는게 안전
    public Organization? Org { get; set; }

    public List<Vehicle> CreatedVehicles { get; set; } = new();
    public List<Vehicle> UpdatedVehicles { get; set; } = new();
}
