using Aims.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aims.Api.Infrastructure.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");

        b.HasKey(x => x.Id);

        b.Property(x => x.Email)
            .HasMaxLength(320)
            .IsRequired();

        b.HasIndex(x => x.Email)
            .IsUnique();

        b.Property(x => x.PasswordHash)
            .HasMaxLength(512)
            .IsRequired();

        b.Property(x => x.OrgId)
            .IsRequired();

        b.Property(x => x.Role)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        b.Property(x => x.IsActive)
            .IsRequired();

        b.Property(x => x.CreatedAtUtc)
            .IsRequired();

        b.Property(x => x.UpdatedAtUtc);

        // FK: users.org_id -> organizations.id
        b.HasOne(x => x.Org)
            .WithMany(o => o.Users)
            .HasForeignKey(x => x.OrgId)
            .OnDelete(DeleteBehavior.Restrict);

        // audit inverse navs
        b.HasMany(x => x.CreatedVehicles)
            .WithOne(v => v.CreatedByUser)
            .HasForeignKey(v => v.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasMany(x => x.UpdatedVehicles)
            .WithOne(v => v.UpdatedByUser)
            .HasForeignKey(v => v.UpdatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
