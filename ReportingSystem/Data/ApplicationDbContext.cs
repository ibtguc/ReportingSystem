using Microsoft.EntityFrameworkCore;
using ReportingSystem.Models;

namespace ReportingSystem.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Authentication
    public DbSet<User> Users { get; set; }
    public DbSet<MagicLink> MagicLinks { get; set; }

    // Organization
    public DbSet<Committee> Committees { get; set; }
    public DbSet<CommitteeMembership> CommitteeMemberships { get; set; }
    public DbSet<ShadowAssignment> ShadowAssignments { get; set; }

    // Notifications
    public DbSet<Notification> Notifications { get; set; }

    // Backup
    public DbSet<DatabaseBackup> DatabaseBackups { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.SystemRole);
            entity.HasIndex(e => e.IsActive);
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

        // Configure Committee
        modelBuilder.Entity<Committee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.HierarchyLevel);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.Sector);
            entity.HasOne(e => e.ParentCommittee)
                .WithMany(e => e.SubCommittees)
                .HasForeignKey(e => e.ParentCommitteeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure CommitteeMembership
        modelBuilder.Entity<CommitteeMembership>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.CommitteeId });
            entity.HasIndex(e => e.CommitteeId);
            entity.HasOne(e => e.User)
                .WithMany(u => u.CommitteeMemberships)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Committee)
                .WithMany(c => c.Memberships)
                .HasForeignKey(e => e.CommitteeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure ShadowAssignment
        modelBuilder.Entity<ShadowAssignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PrincipalUserId, e.CommitteeId });
            entity.HasIndex(e => e.ShadowUserId);
            entity.HasOne(e => e.PrincipalUser)
                .WithMany()
                .HasForeignKey(e => e.PrincipalUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ShadowUser)
                .WithMany()
                .HasForeignKey(e => e.ShadowUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Committee)
                .WithMany(c => c.ShadowAssignments)
                .HasForeignKey(e => e.CommitteeId)
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
