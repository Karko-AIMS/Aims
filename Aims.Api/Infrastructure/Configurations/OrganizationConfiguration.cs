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

        b.HasIndex(x => x.Name)
            .IsUnique();

        b.Property(x => x.IsActive)
            .IsRequired();

        b.Property(x => x.CreatedAtUtc)
            .IsRequired();

        b.Property(x => x.UpdatedAtUtc);
        b.Property(x => x.DeactivatedAtUtc);
    }
}
