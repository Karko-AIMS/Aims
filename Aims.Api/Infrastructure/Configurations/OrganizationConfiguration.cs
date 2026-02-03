using Aims.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aims.Api.Infrastructure.Configurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> b)
    {
        b.ToTable("organizations");

        b.HasKey(x => x.Id);

        b.Property(x => x.Name)
            .HasMaxLength(120)
            .IsRequired();

        b.Property(x => x.CreatedAtUtc)
            .IsRequired();

        // WPF에서 쓰는 orgId
        var defaultOrgId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        b.HasData(new Organization
        {
            Id = defaultOrgId,
            Name = "Default Org",
            CreatedAtUtc = new DateTime(2026, 2, 4, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
