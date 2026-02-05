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

    // Reporting DbSets
    public DbSet<ReportTemplate> ReportTemplates { get; set; }
    public DbSet<ReportTemplateAssignment> ReportTemplateAssignments { get; set; }
    public DbSet<ReportField> ReportFields { get; set; }
    public DbSet<ReportPeriod> ReportPeriods { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<ReportFieldValue> ReportFieldValues { get; set; }
    public DbSet<Attachment> Attachments { get; set; }

    // Upward Flow DbSets (Phase 4)
    public DbSet<SuggestedAction> SuggestedActions { get; set; }
    public DbSet<ResourceRequest> ResourceRequests { get; set; }
    public DbSet<SupportRequest> SupportRequests { get; set; }

    // Workflow & Tagging DbSets (Phase 5)
    public DbSet<Comment> Comments { get; set; }
    public DbSet<ConfirmationTag> ConfirmationTags { get; set; }

    // Downward Flow DbSets (Phase 6)
    public DbSet<Feedback> Feedbacks { get; set; }
    public DbSet<Recommendation> Recommendations { get; set; }
    public DbSet<Decision> Decisions { get; set; }

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

        // Configure ReportTemplate
        modelBuilder.Entity<ReportTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Schedule);
            entity.HasIndex(e => e.IsActive);

            entity.HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure ReportTemplateAssignment
        modelBuilder.Entity<ReportTemplateAssignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ReportTemplateId, e.AssignmentType });
            entity.HasIndex(e => e.TargetId);

            entity.HasOne(e => e.ReportTemplate)
                .WithMany(t => t.Assignments)
                .HasForeignKey(e => e.ReportTemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ReportField
        modelBuilder.Entity<ReportField>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ReportTemplateId, e.SectionOrder, e.FieldOrder });
            entity.HasIndex(e => e.FieldKey);

            entity.HasOne(e => e.ReportTemplate)
                .WithMany(t => t.Fields)
                .HasForeignKey(e => e.ReportTemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ReportPeriod
        modelBuilder.Entity<ReportPeriod>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ReportTemplateId, e.StartDate, e.EndDate });
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.SubmissionDeadline);

            entity.HasOne(e => e.ReportTemplate)
                .WithMany(t => t.Periods)
                .HasForeignKey(e => e.ReportTemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Report
        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ReportTemplateId, e.ReportPeriodId, e.SubmittedById }).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.SubmittedById);
            entity.HasIndex(e => e.AssignedReviewerId);

            entity.HasOne(e => e.ReportTemplate)
                .WithMany(t => t.Reports)
                .HasForeignKey(e => e.ReportTemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ReportPeriod)
                .WithMany(p => p.Reports)
                .HasForeignKey(e => e.ReportPeriodId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.SubmittedBy)
                .WithMany(u => u.SubmittedReports)
                .HasForeignKey(e => e.SubmittedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AssignedReviewer)
                .WithMany(u => u.ReviewedReports)
                .HasForeignKey(e => e.AssignedReviewerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure ReportFieldValue
        modelBuilder.Entity<ReportFieldValue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ReportId, e.ReportFieldId }).IsUnique();

            entity.HasOne(e => e.Report)
                .WithMany(r => r.FieldValues)
                .HasForeignKey(e => e.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ReportField)
                .WithMany(f => f.Values)
                .HasForeignKey(e => e.ReportFieldId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Attachment
        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReportId);
            entity.HasIndex(e => e.ReportFieldId);

            entity.HasOne(e => e.Report)
                .WithMany(r => r.Attachments)
                .HasForeignKey(e => e.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ReportField)
                .WithMany()
                .HasForeignKey(e => e.ReportFieldId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.UploadedBy)
                .WithMany()
                .HasForeignKey(e => e.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure SuggestedAction (Phase 4)
        modelBuilder.Entity<SuggestedAction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReportId);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);

            entity.HasOne(e => e.Report)
                .WithMany(r => r.SuggestedActions)
                .HasForeignKey(e => e.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ReviewedBy)
                .WithMany()
                .HasForeignKey(e => e.ReviewedById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure ResourceRequest (Phase 4)
        modelBuilder.Entity<ResourceRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReportId);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Urgency);

            entity.HasOne(e => e.Report)
                .WithMany(r => r.ResourceRequests)
                .HasForeignKey(e => e.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ReviewedBy)
                .WithMany()
                .HasForeignKey(e => e.ReviewedById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure SupportRequest (Phase 4)
        modelBuilder.Entity<SupportRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReportId);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Urgency);
            entity.HasIndex(e => e.AssignedToId);

            entity.HasOne(e => e.Report)
                .WithMany(r => r.SupportRequests)
                .HasForeignKey(e => e.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.AssignedTo)
                .WithMany()
                .HasForeignKey(e => e.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.AcknowledgedBy)
                .WithMany()
                .HasForeignKey(e => e.AcknowledgedById)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.ResolvedBy)
                .WithMany()
                .HasForeignKey(e => e.ResolvedById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Comment (Phase 5)
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReportId);
            entity.HasIndex(e => e.ParentCommentId);
            entity.HasIndex(e => e.AuthorId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Report)
                .WithMany(r => r.Comments)
                .HasForeignKey(e => e.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(e => e.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Author)
                .WithMany()
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ReportField)
                .WithMany()
                .HasForeignKey(e => e.ReportFieldId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure ConfirmationTag (Phase 5)
        modelBuilder.Entity<ConfirmationTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReportId);
            entity.HasIndex(e => e.RequestedById);
            entity.HasIndex(e => e.TaggedUserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.ReportId, e.TaggedUserId });

            entity.HasOne(e => e.Report)
                .WithMany(r => r.ConfirmationTags)
                .HasForeignKey(e => e.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.RequestedBy)
                .WithMany()
                .HasForeignKey(e => e.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TaggedUser)
                .WithMany()
                .HasForeignKey(e => e.TaggedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ReportField)
                .WithMany()
                .HasForeignKey(e => e.ReportFieldId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Feedback (Phase 6)
        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReportId);
            entity.HasIndex(e => e.AuthorId);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Visibility);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ParentFeedbackId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Report)
                .WithMany(r => r.Feedbacks)
                .HasForeignKey(e => e.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Author)
                .WithMany()
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ParentFeedback)
                .WithMany(f => f.Replies)
                .HasForeignKey(e => e.ParentFeedbackId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ReportField)
                .WithMany()
                .HasForeignKey(e => e.ReportFieldId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Recommendation (Phase 6)
        modelBuilder.Entity<Recommendation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReportId);
            entity.HasIndex(e => e.IssuedById);
            entity.HasIndex(e => e.TargetOrgUnitId);
            entity.HasIndex(e => e.TargetUserId);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.TargetScope);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Report)
                .WithMany(r => r.Recommendations)
                .HasForeignKey(e => e.ReportId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.IssuedBy)
                .WithMany()
                .HasForeignKey(e => e.IssuedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TargetOrgUnit)
                .WithMany()
                .HasForeignKey(e => e.TargetOrgUnitId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.TargetUser)
                .WithMany()
                .HasForeignKey(e => e.TargetUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Decision (Phase 6)
        modelBuilder.Entity<Decision>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReportId);
            entity.HasIndex(e => e.DecidedById);
            entity.HasIndex(e => e.RequestType);
            entity.HasIndex(e => e.Outcome);
            entity.HasIndex(e => e.SuggestedActionId);
            entity.HasIndex(e => e.ResourceRequestId);
            entity.HasIndex(e => e.SupportRequestId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Report)
                .WithMany(r => r.Decisions)
                .HasForeignKey(e => e.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.DecidedBy)
                .WithMany()
                .HasForeignKey(e => e.DecidedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.SuggestedAction)
                .WithMany()
                .HasForeignKey(e => e.SuggestedActionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.ResourceRequest)
                .WithMany()
                .HasForeignKey(e => e.ResourceRequestId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.SupportRequest)
                .WithMany()
                .HasForeignKey(e => e.SupportRequestId)
                .OnDelete(DeleteBehavior.SetNull);
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
