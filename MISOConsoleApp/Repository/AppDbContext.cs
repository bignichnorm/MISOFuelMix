using Microsoft.EntityFrameworkCore;
using MISOQueryingApp.Repository.Models;

namespace MISOQueryingApp.Repository;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<FuelMixSnapshot> FuelMixSnapshots => Set<FuelMixSnapshot>();
    public DbSet<FuelMixElement> FuelMixElements => Set<FuelMixElement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FuelMixSnapshot>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.IntervalEst).IsRequired();
            entity.HasIndex(x => x.IntervalEst).IsUnique();
        });

        modelBuilder.Entity<FuelMixElement>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasOne<FuelMixSnapshot>().WithMany(x => x.FuelMixElements).HasForeignKey(x => x.SnapshotId);

            entity.Property(x => x.Category).IsRequired();
            entity.HasIndex(x => x.Category);
        });
    }
}