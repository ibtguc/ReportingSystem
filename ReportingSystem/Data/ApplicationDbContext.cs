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

    // Reporting
    public DbSet<Report> Reports { get; set; }
    public DbSet<Attachment> Attachments { get; set; }
    public DbSet<ReportStatusHistory> ReportStatusHistories { get; set; }
    public DbSet<ReportSourceLink> ReportSourceLinks { get; set; }

    // Directives
    public DbSet<Directive> Directives { get; set; }
    public DbSet<DirectiveStatusHistory> DirectiveStatusHistories { get; set; }

    // Meetings
    public DbSet<Meeting> Meetings { get; set; }
    public DbSet<MeetingAgendaItem> MeetingAgendaItems { get; set; }
    public DbSet<MeetingAttendee> MeetingAttendees { get; set; }
    public DbSet<MeetingDecision> MeetingDecisions { get; set; }
    public DbSet<ActionItem> ActionItems { get; set; }

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

        // Configure Report
        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.CommitteeId, e.Status });
            entity.HasIndex(e => new { e.AuthorId, e.Status });
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IsConfidential);
            entity.HasOne(e => e.Author)
                .WithMany()
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Committee)
                .WithMany()
                .HasForeignKey(e => e.CommitteeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.OriginalReport)
                .WithMany(r => r.Revisions)
                .HasForeignKey(e => e.OriginalReportId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Attachment
        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReportId);
            entity.HasOne(e => e.Report)
                .WithMany(r => r.Attachments)
                .HasForeignKey(e => e.ReportId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.UploadedBy)
                .WithMany()
                .HasForeignKey(e => e.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure ReportStatusHistory
        modelBuilder.Entity<ReportStatusHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReportId);
            entity.HasIndex(e => e.ChangedAt);
            entity.HasOne(e => e.Report)
                .WithMany(r => r.StatusHistory)
                .HasForeignKey(e => e.ReportId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ChangedBy)
                .WithMany()
                .HasForeignKey(e => e.ChangedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure ReportSourceLink
        modelBuilder.Entity<ReportSourceLink>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SummaryReportId, e.SourceReportId }).IsUnique();
            entity.HasIndex(e => e.SourceReportId);
            entity.HasOne(e => e.SummaryReport)
                .WithMany(r => r.SourceLinks)
                .HasForeignKey(e => e.SummaryReportId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.SourceReport)
                .WithMany(r => r.SummaryLinks)
                .HasForeignKey(e => e.SourceReportId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Directive
        modelBuilder.Entity<Directive>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TargetCommitteeId, e.Status });
            entity.HasIndex(e => new { e.IssuerId, e.Status });
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.Deadline);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasOne(e => e.Issuer)
                .WithMany()
                .HasForeignKey(e => e.IssuerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.TargetCommittee)
                .WithMany()
                .HasForeignKey(e => e.TargetCommitteeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.TargetUser)
                .WithMany()
                .HasForeignKey(e => e.TargetUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.RelatedReport)
                .WithMany()
                .HasForeignKey(e => e.RelatedReportId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ParentDirective)
                .WithMany(d => d.ChildDirectives)
                .HasForeignKey(e => e.ParentDirectiveId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure DirectiveStatusHistory
        modelBuilder.Entity<DirectiveStatusHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DirectiveId);
            entity.HasIndex(e => e.ChangedAt);
            entity.HasOne(e => e.Directive)
                .WithMany(d => d.StatusHistory)
                .HasForeignKey(e => e.DirectiveId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ChangedBy)
                .WithMany()
                .HasForeignKey(e => e.ChangedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Meeting
        modelBuilder.Entity<Meeting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.CommitteeId, e.Status });
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ScheduledAt);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasOne(e => e.Committee)
                .WithMany()
                .HasForeignKey(e => e.CommitteeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Moderator)
                .WithMany()
                .HasForeignKey(e => e.ModeratorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure MeetingAgendaItem
        modelBuilder.Entity<MeetingAgendaItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MeetingId);
            entity.HasOne(e => e.Meeting)
                .WithMany(m => m.AgendaItems)
                .HasForeignKey(e => e.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Presenter)
                .WithMany()
                .HasForeignKey(e => e.PresenterId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.LinkedReport)
                .WithMany()
                .HasForeignKey(e => e.LinkedReportId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure MeetingAttendee
        modelBuilder.Entity<MeetingAttendee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.MeetingId, e.UserId }).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasOne(e => e.Meeting)
                .WithMany(m => m.Attendees)
                .HasForeignKey(e => e.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure MeetingDecision
        modelBuilder.Entity<MeetingDecision>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MeetingId);
            entity.HasOne(e => e.Meeting)
                .WithMany(m => m.Decisions)
                .HasForeignKey(e => e.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.AgendaItem)
                .WithMany()
                .HasForeignKey(e => e.AgendaItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure ActionItem
        modelBuilder.Entity<ActionItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.AssignedToId, e.Status });
            entity.HasIndex(e => e.MeetingId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Deadline);
            entity.HasOne(e => e.Meeting)
                .WithMany(m => m.ActionItems)
                .HasForeignKey(e => e.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.MeetingDecision)
                .WithMany(d => d.ActionItems)
                .HasForeignKey(e => e.MeetingDecisionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AssignedTo)
                .WithMany()
                .HasForeignKey(e => e.AssignedToId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AssignedBy)
                .WithMany()
                .HasForeignKey(e => e.AssignedById)
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
