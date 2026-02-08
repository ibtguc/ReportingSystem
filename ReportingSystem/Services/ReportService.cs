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

    // ── Summarization ──

    public async Task<Report> CreateSummaryAsync(Report summaryReport, List<int> sourceReportIds,
        Dictionary<int, string?>? annotations, int userId)
    {
        summaryReport.AuthorId = userId;
        summaryReport.CreatedAt = DateTime.UtcNow;
        summaryReport.Status = ReportStatus.Draft;
        summaryReport.Version = 1;

        _context.Reports.Add(summaryReport);
        await _context.SaveChangesAsync();

        // Create source links
        foreach (var sourceId in sourceReportIds)
        {
            var link = new ReportSourceLink
            {
                SummaryReportId = summaryReport.Id,
                SourceReportId = sourceId,
                Annotation = annotations?.GetValueOrDefault(sourceId),
                CreatedAt = DateTime.UtcNow
            };
            _context.ReportSourceLinks.Add(link);
        }
        await _context.SaveChangesAsync();

        // Mark source reports as Summarized
        foreach (var sourceId in sourceReportIds)
        {
            var source = await _context.Reports.FindAsync(sourceId);
            if (source != null && (source.Status == ReportStatus.Approved || source.Status == ReportStatus.Submitted))
            {
                var oldStatus = source.Status;
                source.Status = ReportStatus.Summarized;
                source.UpdatedAt = DateTime.UtcNow;
                await AddStatusHistoryAsync(sourceId, oldStatus, ReportStatus.Summarized, userId,
                    $"Summarized in Report #{summaryReport.Id}");
            }
        }

        await AddStatusHistoryAsync(summaryReport.Id, ReportStatus.Draft, ReportStatus.Draft, userId,
            $"Summary created from {sourceReportIds.Count} source report(s)");

        _logger.LogInformation("Summary report '{Title}' (ID: {Id}) created from {Count} sources by user {UserId}",
            summaryReport.Title, summaryReport.Id, sourceReportIds.Count, userId);

        return summaryReport;
    }

    /// <summary>
    /// Gets reports available to be summarized: approved/submitted reports from
    /// the specified committee and its sub-committees.
    /// </summary>
    public async Task<List<Report>> GetSummarizableReportsAsync(int committeeId)
    {
        var committeeIds = await GetDescendantCommitteeIdsAsync(committeeId);
        committeeIds.Add(committeeId);

        return await _context.Reports
            .Include(r => r.Author)
            .Include(r => r.Committee)
            .Where(r => committeeIds.Contains(r.CommitteeId)
                     && (r.Status == ReportStatus.Approved || r.Status == ReportStatus.Submitted)
                     && r.ReportType == ReportType.Detailed)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Gets the full drill-down chain: starting from a summary, walks down
    /// through all source links to build an expandable tree.
    /// </summary>
    public async Task<DrillDownNode> GetDrillDownTreeAsync(int reportId)
    {
        var report = await _context.Reports
            .Include(r => r.Author)
            .Include(r => r.Committee)
            .Include(r => r.SourceLinks).ThenInclude(sl => sl.SourceReport).ThenInclude(r => r.Author)
            .Include(r => r.SourceLinks).ThenInclude(sl => sl.SourceReport).ThenInclude(r => r.Committee)
            .FirstOrDefaultAsync(r => r.Id == reportId);

        if (report == null)
            return new DrillDownNode { Report = new Report { Title = "Not Found" } };

        return await BuildDrillDownNodeAsync(report, 0);
    }

    private async Task<DrillDownNode> BuildDrillDownNodeAsync(Report report, int depth)
    {
        var node = new DrillDownNode
        {
            Report = report,
            Depth = depth
        };

        // Load source links if not already loaded
        if (!report.SourceLinks.Any())
        {
            await _context.Entry(report).Collection(r => r.SourceLinks).LoadAsync();
            foreach (var link in report.SourceLinks)
            {
                await _context.Entry(link).Reference(l => l.SourceReport).LoadAsync();
                await _context.Entry(link.SourceReport).Reference(r => r.Author).LoadAsync();
                await _context.Entry(link.SourceReport).Reference(r => r.Committee).LoadAsync();
            }
        }

        foreach (var link in report.SourceLinks)
        {
            var childNode = await BuildDrillDownNodeAsync(link.SourceReport, depth + 1);
            childNode.Annotation = link.Annotation;
            node.Children.Add(childNode);
        }

        return node;
    }

    /// <summary>
    /// Gets all summaries that reference this report (upward chain).
    /// </summary>
    public async Task<List<Report>> GetSummariesOfReportAsync(int reportId)
    {
        return await _context.ReportSourceLinks
            .Include(l => l.SummaryReport).ThenInclude(r => r.Author)
            .Include(l => l.SummaryReport).ThenInclude(r => r.Committee)
            .Where(l => l.SourceReportId == reportId)
            .Select(l => l.SummaryReport)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Gets source reports linked to a summary.
    /// </summary>
    public async Task<List<ReportSourceLink>> GetSourceLinksAsync(int summaryReportId)
    {
        return await _context.ReportSourceLinks
            .Include(l => l.SourceReport).ThenInclude(r => r.Author)
            .Include(l => l.SourceReport).ThenInclude(r => r.Committee)
            .Where(l => l.SummaryReportId == summaryReportId)
            .OrderBy(l => l.SourceReport.Committee.HierarchyLevel)
            .ThenBy(l => l.SourceReport.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Computes how many levels deep this report is in the summarization chain.
    /// 0 = raw report, 1 = summary of raw reports, 2 = summary of summaries, etc.
    /// </summary>
    public async Task<int> GetSummarizationDepthAsync(int reportId)
    {
        var hasSourceLinks = await _context.ReportSourceLinks
            .AnyAsync(l => l.SummaryReportId == reportId);

        if (!hasSourceLinks) return 0;

        var sourceIds = await _context.ReportSourceLinks
            .Where(l => l.SummaryReportId == reportId)
            .Select(l => l.SourceReportId)
            .ToListAsync();

        int maxChildDepth = 0;
        foreach (var sourceId in sourceIds)
        {
            var childDepth = await GetSummarizationDepthAsync(sourceId);
            if (childDepth > maxChildDepth) maxChildDepth = childDepth;
        }

        return maxChildDepth + 1;
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

/// <summary>
/// Tree node for drill-down visualization.
/// </summary>
public class DrillDownNode
{
    public Report Report { get; set; } = null!;
    public int Depth { get; set; }
    public string? Annotation { get; set; }
    public List<DrillDownNode> Children { get; set; } = new();
}
