using Aims.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aims.Api.Infrastructure.Data;

public sealed class AimsDbContext : DbContext
{
    public AimsDbContext(DbContextOptions<AimsDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AimsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
