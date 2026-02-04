using Microsoft.EntityFrameworkCore;
using ReportingSystem.Models;

namespace ReportingSystem.Data;

/// <summary>
/// Main database context for the reporting system
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Authentication DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<MagicLink> MagicLinks { get; set; }

    // Organization DbSets
    public DbSet<OrganizationalUnit> OrganizationalUnits { get; set; }
    public DbSet<Delegation> Delegations { get; set; }

    // Notification DbSets
    public DbSet<Notification> Notifications { get; set; }

    // Backup DbSets
    public DbSet<DatabaseBackup> DatabaseBackups { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Role);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.OrganizationalUnitId);

            entity.HasOne(e => e.OrganizationalUnit)
                .WithMany(ou => ou.Users)
                .HasForeignKey(e => e.OrganizationalUnitId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure MagicLink
        modelBuilder.Entity<MagicLink>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.IsUsed);
            entity.HasOne(e => e.User)
                .WithMany(u => u.MagicLinks)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure OrganizationalUnit
        modelBuilder.Entity<OrganizationalUnit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Code).IsUnique().HasFilter("Code IS NOT NULL");
            entity.HasIndex(e => e.ParentId);
            entity.HasIndex(e => e.Level);
            entity.HasIndex(e => e.IsActive);

            entity.HasOne(e => e.Parent)
                .WithMany(e => e.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Delegation
        modelBuilder.Entity<Delegation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DelegatorId);
            entity.HasIndex(e => e.DelegateId);
            entity.HasIndex(e => new { e.StartDate, e.EndDate });
            entity.HasIndex(e => e.IsActive);

            entity.HasOne(e => e.Delegator)
                .WithMany()
                .HasForeignKey(e => e.DelegatorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Delegate)
                .WithMany()
                .HasForeignKey(e => e.DelegateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Notification
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.IsRead });
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Type);
        });

        // Configure DatabaseBackup
        modelBuilder.Entity<DatabaseBackup>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.IsAutomaticDailyBackup);
        });
    }
}
