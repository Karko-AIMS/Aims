using Aims.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aims.Api.Infrastructure.Configurations;

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> b)
    {
        b.ToTable("vehicles");

        b.HasKey(x => x.Id);

        b.Property(x => x.OrgId).IsRequired();

        b.HasOne(x => x.Org)
            .WithMany(o => o.Vehicles)
            .HasForeignKey(x => x.OrgId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Property(x => x.VehicleCode)
            .HasMaxLength(64)
            .IsRequired();

        b.Property(x => x.DisplayName)
            .HasMaxLength(120)
            .IsRequired();

        b.Property(x => x.Vin).HasMaxLength(32);
        b.Property(x => x.PlateNumber).HasMaxLength(32);

        b.Property(x => x.Manufacturer).HasMaxLength(120);
        b.Property(x => x.Model).HasMaxLength(120);

        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.CreatedAtUtc).IsRequired();
        b.Property(x => x.UpdatedAtUtc);

        // multi-tenant unique
        b.HasIndex(x => new { x.OrgId, x.VehicleCode }).IsUnique();
        b.HasIndex(x => new { x.OrgId, x.Vin }).IsUnique();
        b.HasIndex(x => new { x.OrgId, x.PlateNumber }).IsUnique();
    }
}
