using LicenseServer.Models;
using Microsoft.EntityFrameworkCore;

namespace LicenseServer.Data;

public class LicenseDbContext : DbContext
{
    public LicenseDbContext(DbContextOptions<LicenseDbContext> options) : base(options) { }

    public DbSet<LicenseKey> LicenseKeys => Set<LicenseKey>();
    public DbSet<Activation> Activations => Set<Activation>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<AnnouncementDismissal> AnnouncementDismissals => Set<AnnouncementDismissal>();
    public DbSet<DeviceControl> DeviceControls => Set<DeviceControl>();

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

        modelBuilder.Entity<Announcement>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.ProductScope).HasMaxLength(32);
            e.Property(x => x.ImageUrl).HasMaxLength(1024);
            e.Property(x => x.LinkUrl).HasMaxLength(1024);
        });

        modelBuilder.Entity<AnnouncementDismissal>(e =>
        {
            e.HasIndex(x => new { x.AnnouncementId, x.MachineId, x.Product }).IsUnique();
            e.Property(x => x.MachineId).HasMaxLength(128);
            e.Property(x => x.Product).HasMaxLength(32);
            e.HasOne(x => x.Announcement)
                .WithMany(x => x.Dismissals)
                .HasForeignKey(x => x.AnnouncementId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeviceControl>(e =>
        {
            e.HasIndex(x => new { x.MachineId, x.Product }).IsUnique();
            e.Property(x => x.MachineId).HasMaxLength(128);
            e.Property(x => x.Product).HasMaxLength(32);
            e.Property(x => x.PauseReason).HasMaxLength(500);
        });
    }

    /// <summary>EnsureCreated does not add tables to existing DBs — create new ones if missing.</summary>
    public async Task EnsureExtendedSchemaAsync()
    {
        await Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "Announcements" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Announcements" PRIMARY KEY AUTOINCREMENT,
                "Title" TEXT NOT NULL,
                "Body" TEXT NOT NULL,
                "ImageUrl" TEXT NULL,
                "LinkUrl" TEXT NULL,
                "ProductScope" TEXT NOT NULL,
                "IsActive" INTEGER NOT NULL,
                "StartsAt" TEXT NULL,
                "EndsAt" TEXT NULL,
                "SortOrder" INTEGER NOT NULL,
                "CreatedAt" TEXT NOT NULL
            );
            """);

        await Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "AnnouncementDismissals" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_AnnouncementDismissals" PRIMARY KEY AUTOINCREMENT,
                "AnnouncementId" INTEGER NOT NULL,
                "MachineId" TEXT NOT NULL,
                "Product" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                CONSTRAINT "FK_AnnouncementDismissals_Announcements_AnnouncementId"
                    FOREIGN KEY ("AnnouncementId") REFERENCES "Announcements" ("Id") ON DELETE CASCADE
            );
            """);

        await Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_AnnouncementDismissals_AnnouncementId_MachineId_Product"
            ON "AnnouncementDismissals" ("AnnouncementId", "MachineId", "Product");
            """);

        await Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "DeviceControls" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_DeviceControls" PRIMARY KEY AUTOINCREMENT,
                "MachineId" TEXT NOT NULL,
                "Product" TEXT NOT NULL,
                "IsPaused" INTEGER NOT NULL,
                "PauseReason" TEXT NULL,
                "UpdatedAt" TEXT NOT NULL
            );
            """);

        await Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_DeviceControls_MachineId_Product"
            ON "DeviceControls" ("MachineId", "Product");
            """);
    }
}
