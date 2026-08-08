using LicenseServer.Models;
using Microsoft.EntityFrameworkCore;

namespace LicenseServer.Data;

public class LicenseDbContext : DbContext
{
    public LicenseDbContext(DbContextOptions<LicenseDbContext> options) : base(options) { }

    public DbSet<LicenseKey> LicenseKeys => Set<LicenseKey>();
    public DbSet<Activation> Activations => Set<Activation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LicenseKey>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(64);
            e.Property(x => x.Product).HasMaxLength(32);
            e.Property(x => x.DurationType).HasMaxLength(32);
        });

        modelBuilder.Entity<Activation>(e =>
        {
            e.HasIndex(x => new { x.LicenseKeyId, x.MachineId, x.Product }).IsUnique();
            e.Property(x => x.MachineId).HasMaxLength(128);
            e.Property(x => x.Product).HasMaxLength(32);
            e.HasOne(x => x.LicenseKey)
                .WithMany(x => x.Activations)
                .HasForeignKey(x => x.LicenseKeyId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
