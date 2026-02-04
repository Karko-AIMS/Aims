using Aims.Api.Domain.Entities;

namespace Aims.Api.Infrastructure.Data.Repositories;

public interface IOrganizationRepository
{
    Task<List<Organization>> ListAsync(string? q, bool? isActive, int skip, int take, CancellationToken ct);
    Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<bool> ExistsByNameAsync(string nameNormalized, CancellationToken ct);
    Task<bool> ExistsByNameExceptIdAsync(Guid id, string nameNormalized, CancellationToken ct);

    Task AddAsync(Organization org, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
