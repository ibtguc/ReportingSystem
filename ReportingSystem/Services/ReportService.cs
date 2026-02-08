using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Services;

public class ReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReportService> _logger;

    public ReportService(ApplicationDbContext context, ILogger<ReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── Queries ──

    public async Task<List<Report>> GetReportsAsync(
        int? committeeId = null,
        int? authorId = null,
        ReportStatus? status = null,
        ReportType? reportType = null,
        bool includeArchived = false)
    {
        var query = _context.Reports
            .Include(r => r.Author)
            .Include(r => r.Committee)
            .Include(r => r.Attachments)
            .AsQueryable();

        if (!includeArchived)
            query = query.Where(r => r.Status != ReportStatus.Archived);

        if (committeeId.HasValue)
            query = query.Where(r => r.CommitteeId == committeeId.Value);

        if (authorId.HasValue)
            query = query.Where(r => r.AuthorId == authorId.Value);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        if (reportType.HasValue)
            query = query.Where(r => r.ReportType == reportType.Value);

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Report?> GetReportByIdAsync(int id)
    {
        return await _context.Reports
            .Include(r => r.Author)
            .Include(r => r.Committee)
            .Include(r => r.Attachments).ThenInclude(a => a.UploadedBy)
            .Include(r => r.StatusHistory).ThenInclude(h => h.ChangedBy)
            .Include(r => r.OriginalReport)
            .Include(r => r.Revisions)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<Report>> GetReportsForCommitteeTreeAsync(int committeeId)
    {
        // Get this committee and all descendant committee IDs
        var committeeIds = await GetDescendantCommitteeIdsAsync(committeeId);
        committeeIds.Add(committeeId);

        return await _context.Reports
            .Include(r => r.Author)
            .Include(r => r.Committee)
            .Where(r => committeeIds.Contains(r.CommitteeId) && r.Status != ReportStatus.Archived)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    // ── Create / Update ──

    public async Task<Report> CreateReportAsync(Report report, int userId)
    {
        report.AuthorId = userId;
        report.CreatedAt = DateTime.UtcNow;
        report.Status = ReportStatus.Draft;
        report.Version = 1;

        _context.Reports.Add(report);
        await _context.SaveChangesAsync();

        await AddStatusHistoryAsync(report.Id, ReportStatus.Draft, ReportStatus.Draft, userId, "Report created");

        _logger.LogInformation("Report '{Title}' created by user {UserId} in committee {CommitteeId}",
            report.Title, userId, report.CommitteeId);

        return report;
    }

    public async Task UpdateReportAsync(Report report, int userId)
    {
        report.UpdatedAt = DateTime.UtcNow;
        _context.Reports.Update(report);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Report '{Title}' (ID: {Id}) updated by user {UserId}",
            report.Title, report.Id, userId);
    }

    // ── Status Transitions ──

    public async Task<bool> SubmitReportAsync(int reportId, int userId)
    {
        var report = await _context.Reports.FindAsync(reportId);
        if (report == null) return false;
        if (report.Status != ReportStatus.Draft && report.Status != ReportStatus.Revised)
            return false;

        var oldStatus = report.Status;
        report.Status = ReportStatus.Submitted;
        report.SubmittedAt = DateTime.UtcNow;
        report.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await AddStatusHistoryAsync(reportId, oldStatus, ReportStatus.Submitted, userId, "Report submitted for review");

        _logger.LogInformation("Report {Id} submitted by user {UserId}", reportId, userId);
        return true;
    }

    public async Task<bool> StartReviewAsync(int reportId, int userId)
    {
        var report = await _context.Reports.FindAsync(reportId);
        if (report == null || report.Status != ReportStatus.Submitted)
            return false;

        var oldStatus = report.Status;
        report.Status = ReportStatus.UnderReview;
        report.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await AddStatusHistoryAsync(reportId, oldStatus, ReportStatus.UnderReview, userId, "Review started");
        return true;
    }

    public async Task<bool> RequestFeedbackAsync(int reportId, int userId, string comments)
    {
        var report = await _context.Reports.FindAsync(reportId);
        if (report == null || report.Status != ReportStatus.UnderReview)
            return false;

        var oldStatus = report.Status;
        report.Status = ReportStatus.FeedbackRequested;
        report.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await AddStatusHistoryAsync(reportId, oldStatus, ReportStatus.FeedbackRequested, userId, comments);

        _logger.LogInformation("Feedback requested on report {Id} by user {UserId}", reportId, userId);
        return true;
    }

    public async Task<Report?> ReviseReportAsync(int originalReportId, Report revisedReport, int userId)
    {
        var original = await _context.Reports.FindAsync(originalReportId);
        if (original == null || original.Status != ReportStatus.FeedbackRequested)
            return null;

        // Create the revision as a new report linked to original
        revisedReport.AuthorId = userId;
        revisedReport.CommitteeId = original.CommitteeId;
        revisedReport.OriginalReportId = original.OriginalReportId ?? original.Id;
        revisedReport.Version = original.Version + 1;
        revisedReport.Status = ReportStatus.Revised;
        revisedReport.CreatedAt = DateTime.UtcNow;
        revisedReport.ReportType = original.ReportType;
        revisedReport.IsConfidential = original.IsConfidential;

        _context.Reports.Add(revisedReport);

        // Mark original as Revised
        var oldStatus = original.Status;
        original.Status = ReportStatus.Revised;
        original.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await AddStatusHistoryAsync(original.Id, oldStatus, ReportStatus.Revised, userId,
            $"Revised — new version {revisedReport.Version} created (Report #{revisedReport.Id})");
        await AddStatusHistoryAsync(revisedReport.Id, ReportStatus.Draft, ReportStatus.Revised, userId,
            $"Revision of Report #{original.Id} (v{original.Version})");

        _logger.LogInformation("Report {OriginalId} revised as {NewId} (v{Version}) by user {UserId}",
            original.Id, revisedReport.Id, revisedReport.Version, userId);

        return revisedReport;
    }

    public async Task<bool> ApproveReportAsync(int reportId, int userId, string? comments = null)
    {
        var report = await _context.Reports.FindAsync(reportId);
        if (report == null || (report.Status != ReportStatus.UnderReview && report.Status != ReportStatus.Submitted))
            return false;

        var oldStatus = report.Status;
        report.Status = ReportStatus.Approved;
        report.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await AddStatusHistoryAsync(reportId, oldStatus, ReportStatus.Approved, userId,
            comments ?? "Report approved");

        _logger.LogInformation("Report {Id} approved by user {UserId}", reportId, userId);
        return true;
    }

    public async Task<bool> ArchiveReportAsync(int reportId, int userId)
    {
        var report = await _context.Reports.FindAsync(reportId);
        if (report == null || report.Status != ReportStatus.Approved)
            return false;

        var oldStatus = report.Status;
        report.Status = ReportStatus.Archived;
        report.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await AddStatusHistoryAsync(reportId, oldStatus, ReportStatus.Archived, userId, "Report archived");
        return true;
    }

    // ── Attachments ──

    public async Task<Attachment> AddAttachmentAsync(int reportId, string fileName, string storagePath,
        string? contentType, long fileSizeBytes, int uploadedById)
    {
        var attachment = new Attachment
        {
            ReportId = reportId,
            FileName = fileName,
            StoragePath = storagePath,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            UploadedById = uploadedById,
            UploadedAt = DateTime.UtcNow
        };

        _context.Attachments.Add(attachment);
        await _context.SaveChangesAsync();
        return attachment;
    }

    public async Task RemoveAttachmentAsync(int attachmentId)
    {
        var attachment = await _context.Attachments.FindAsync(attachmentId);
        if (attachment != null)
        {
            _context.Attachments.Remove(attachment);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Attachment?> GetAttachmentByIdAsync(int attachmentId)
    {
        return await _context.Attachments
            .Include(a => a.Report)
            .FirstOrDefaultAsync(a => a.Id == attachmentId);
    }

    // ── Access Checks ──

    public async Task<bool> IsUserMemberOfCommitteeAsync(int userId, int committeeId)
    {
        return await _context.CommitteeMemberships
            .AnyAsync(m => m.UserId == userId && m.CommitteeId == committeeId && m.EffectiveTo == null);
    }

    public async Task<bool> IsUserHeadOfCommitteeAsync(int userId, int committeeId)
    {
        return await _context.CommitteeMemberships
            .AnyAsync(m => m.UserId == userId && m.CommitteeId == committeeId
                        && m.Role == CommitteeRole.Head && m.EffectiveTo == null);
    }

    public async Task<bool> IsUserHeadOfParentCommitteeAsync(int userId, int committeeId)
    {
        var committee = await _context.Committees.FindAsync(committeeId);
        if (committee?.ParentCommitteeId == null) return false;

        return await IsUserHeadOfCommitteeAsync(userId, committee.ParentCommitteeId.Value);
    }

    public async Task<bool> CanUserReviewReportAsync(int userId, int reportCommitteeId)
    {
        // User can review if they are head of the report's committee or its parent
        var isHead = await IsUserHeadOfCommitteeAsync(userId, reportCommitteeId);
        var isParentHead = await IsUserHeadOfParentCommitteeAsync(userId, reportCommitteeId);

        // Also check for SystemAdmin or Chairman roles
        var user = await _context.Users.FindAsync(userId);
        var isPrivileged = user?.SystemRole == SystemRole.SystemAdmin
                        || user?.SystemRole == SystemRole.Chairman;

        return isHead || isParentHead || isPrivileged;
    }

    // ── Stats ──

    public async Task<(int total, int draft, int submitted, int underReview, int approved)> GetReportStatsAsync()
    {
        var total = await _context.Reports.CountAsync(r => r.Status != ReportStatus.Archived);
        var draft = await _context.Reports.CountAsync(r => r.Status == ReportStatus.Draft);
        var submitted = await _context.Reports.CountAsync(r => r.Status == ReportStatus.Submitted);
        var underReview = await _context.Reports.CountAsync(r => r.Status == ReportStatus.UnderReview);
        var approved = await _context.Reports.CountAsync(r => r.Status == ReportStatus.Approved);
        return (total, draft, submitted, underReview, approved);
    }

    public async Task<List<Committee>> GetUserCommitteesAsync(int userId)
    {
        return await _context.CommitteeMemberships
            .Include(m => m.Committee)
            .Where(m => m.UserId == userId && m.EffectiveTo == null)
            .Select(m => m.Committee)
            .OrderBy(c => c.HierarchyLevel)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    // ── Helpers ──

    private async Task AddStatusHistoryAsync(int reportId, ReportStatus oldStatus,
        ReportStatus newStatus, int changedById, string? comments)
    {
        var history = new ReportStatusHistory
        {
            ReportId = reportId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedById = changedById,
            ChangedAt = DateTime.UtcNow,
            Comments = comments
        };
        _context.ReportStatusHistories.Add(history);
        await _context.SaveChangesAsync();
    }

    private async Task<List<int>> GetDescendantCommitteeIdsAsync(int parentId)
    {
        var result = new List<int>();
        var children = await _context.Committees
            .Where(c => c.ParentCommitteeId == parentId && c.IsActive)
            .Select(c => c.Id)
            .ToListAsync();

        result.AddRange(children);
        foreach (var childId in children)
        {
            var descendants = await GetDescendantCommitteeIdsAsync(childId);
            result.AddRange(descendants);
        }
        return result;
    }
}
