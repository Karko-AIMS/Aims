namespace Aims.Api.Contracts.Organizations;

public sealed record OrganizationDto(
    Guid id,
    string name,
    bool isActive,
    DateTime createdAtUtc,
    DateTime? updatedAtUtc,
    DateTime? deactivatedAtUtc
);
