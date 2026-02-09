using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Services;

public class AnalyticsService
{
    private readonly ApplicationDbContext _context;

    public AnalyticsService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get organization-wide analytics overview.
    /// </summary>
    public async Task<OrganizationAnalytics> GetOrganizationAnalyticsAsync()
    {
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);
        var sixtyDaysAgo = now.AddDays(-60);

        return new OrganizationAnalytics
        {
            // Totals
            TotalReports = await _context.Reports.CountAsync(),
            TotalDirectives = await _context.Directives.CountAsync(),
            TotalMeetings = await _context.Meetings.CountAsync(),
            TotalUsers = await _context.Users.CountAsync(u => u.IsActive),
            TotalCommittees = await _context.Committees.CountAsync(c => c.IsActive),

            // Last 30 days
            ReportsLast30Days = await _context.Reports.CountAsync(r => r.CreatedAt >= thirtyDaysAgo),
            DirectivesLast30Days = await _context.Directives.CountAsync(d => d.CreatedAt >= thirtyDaysAgo),
            MeetingsLast30Days = await _context.Meetings.CountAsync(m => m.CreatedAt >= thirtyDaysAgo),

            // Previous 30 days (for trend comparison)
            ReportsPrev30Days = await _context.Reports.CountAsync(r => r.CreatedAt >= sixtyDaysAgo && r.CreatedAt < thirtyDaysAgo),
            DirectivesPrev30Days = await _context.Directives.CountAsync(d => d.CreatedAt >= sixtyDaysAgo && d.CreatedAt < thirtyDaysAgo),
            MeetingsPrev30Days = await _context.Meetings.CountAsync(m => m.CreatedAt >= sixtyDaysAgo && m.CreatedAt < thirtyDaysAgo),

            // Status distributions
            ReportsByStatus = await _context.Reports
                .GroupBy(r => r.Status)
                .Select(g => new StatusCount { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync(),
            DirectivesByStatus = await _context.Directives
                .GroupBy(d => d.Status)
                .Select(g => new StatusCount { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync(),
            MeetingsByStatus = await _context.Meetings
                .GroupBy(m => m.Status)
                .Select(g => new StatusCount { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync(),

            // Overdue items
            OverdueDirectives = await _context.Directives
                .CountAsync(d => d.Deadline.HasValue && d.Deadline.Value < now &&
                    d.Status != DirectiveStatus.Closed && d.Status != DirectiveStatus.Verified),
            OverdueActionItems = await _context.ActionItems
                .CountAsync(a => a.Deadline.HasValue && a.Deadline.Value < now &&
                    a.Status != ActionItemStatus.Completed && a.Status != ActionItemStatus.Verified),

            // Compliance: reports approved vs total submitted
            ReportsApproved = await _context.Reports.CountAsync(r => r.Status == ReportStatus.Approved || r.Status == ReportStatus.Archived),
            ReportsSubmitted = await _context.Reports.CountAsync(r => r.Status != ReportStatus.Draft),

            // Knowledge base
            KnowledgeArticles = await _context.KnowledgeArticles.CountAsync(a => a.IsPublished)
        };
    }

    /// <summary>
    /// Get monthly activity trend data for the last 12 months.
    /// </summary>
    public async Task<List<MonthlyTrend>> GetMonthlyTrendsAsync()
    {
        var trends = new List<MonthlyTrend>();
        var now = DateTime.UtcNow;

        for (int i = 11; i >= 0; i--)
        {
            var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);

            trends.Add(new MonthlyTrend
            {
                Month = monthStart.ToString("MMM yyyy"),
                Reports = await _context.Reports.CountAsync(r => r.CreatedAt >= monthStart && r.CreatedAt < monthEnd),
                Directives = await _context.Directives.CountAsync(d => d.CreatedAt >= monthStart && d.CreatedAt < monthEnd),
                Meetings = await _context.Meetings.CountAsync(m => m.CreatedAt >= monthStart && m.CreatedAt < monthEnd)
            });
        }

        return trends;
    }

    /// <summary>
    /// Get per-committee activity metrics.
    /// </summary>
    public async Task<List<CommitteeMetrics>> GetCommitteeMetricsAsync()
    {
        var committees = await _context.Committees
            .Where(c => c.IsActive)
            .OrderBy(c => c.HierarchyLevel)
            .ThenBy(c => c.Name)
            .ToListAsync();

        var metrics = new List<CommitteeMetrics>();

        foreach (var committee in committees)
        {
            var memberCount = await _context.CommitteeMemberships
                .CountAsync(cm => cm.CommitteeId == committee.Id && cm.EffectiveTo == null);

            metrics.Add(new CommitteeMetrics
            {
                CommitteeId = committee.Id,
                CommitteeName = committee.Name,
                HierarchyLevel = committee.HierarchyLevel,
                MemberCount = memberCount,
                ReportCount = await _context.Reports.CountAsync(r => r.CommitteeId == committee.Id),
                DirectiveCount = await _context.Directives.CountAsync(d => d.TargetCommitteeId == committee.Id),
                MeetingCount = await _context.Meetings.CountAsync(m => m.CommitteeId == committee.Id),
                OpenDirectives = await _context.Directives.CountAsync(d =>
                    d.TargetCommitteeId == committee.Id &&
                    d.Status != DirectiveStatus.Closed && d.Status != DirectiveStatus.Verified),
                PendingReports = await _context.Reports.CountAsync(r =>
                    r.CommitteeId == committee.Id &&
                    (r.Status == ReportStatus.Submitted || r.Status == ReportStatus.UnderReview))
            });
        }

        return metrics;
    }

    /// <summary>
    /// Get directive compliance metrics (on-time vs overdue).
    /// </summary>
    public async Task<ComplianceMetrics> GetComplianceMetricsAsync()
    {
        var closedDirectives = await _context.Directives
            .Where(d => d.Status == DirectiveStatus.Closed || d.Status == DirectiveStatus.Verified)
            .ToListAsync();

        var onTime = closedDirectives.Count(d =>
            !d.Deadline.HasValue || d.UpdatedAt <= d.Deadline.Value);
        var overdue = closedDirectives.Count(d =>
            d.Deadline.HasValue && d.UpdatedAt > d.Deadline.Value);

        var completedActions = await _context.ActionItems
            .Where(a => a.Status == ActionItemStatus.Completed || a.Status == ActionItemStatus.Verified)
            .ToListAsync();

        var actionsOnTime = completedActions.Count(a =>
            !a.Deadline.HasValue || (a.CompletedAt.HasValue && a.CompletedAt.Value <= a.Deadline.Value));
        var actionsOverdue = completedActions.Count(a =>
            a.Deadline.HasValue && a.CompletedAt.HasValue && a.CompletedAt.Value > a.Deadline.Value);

        return new ComplianceMetrics
        {
            DirectivesClosedOnTime = onTime,
            DirectivesClosedOverdue = overdue,
            DirectivesStillOpen = await _context.Directives.CountAsync(d =>
                d.Status != DirectiveStatus.Closed && d.Status != DirectiveStatus.Verified),
            DirectivesCurrentlyOverdue = await _context.Directives.CountAsync(d =>
                d.Deadline.HasValue && d.Deadline.Value < DateTime.UtcNow &&
                d.Status != DirectiveStatus.Closed && d.Status != DirectiveStatus.Verified),
            ActionItemsCompletedOnTime = actionsOnTime,
            ActionItemsCompletedOverdue = actionsOverdue,
            ActionItemsStillOpen = await _context.ActionItems.CountAsync(a =>
                a.Status != ActionItemStatus.Completed && a.Status != ActionItemStatus.Verified),
            MeetingsFinalized = await _context.Meetings.CountAsync(m => m.Status == MeetingStatus.Finalized),
            MeetingsAwaitingConfirmation = await _context.Meetings.CountAsync(m => m.Status == MeetingStatus.MinutesReview)
        };
    }
}

// ── DTOs ──

public class OrganizationAnalytics
{
    public int TotalReports { get; set; }
    public int TotalDirectives { get; set; }
    public int TotalMeetings { get; set; }
    public int TotalUsers { get; set; }
    public int TotalCommittees { get; set; }
    public int ReportsLast30Days { get; set; }
    public int DirectivesLast30Days { get; set; }
    public int MeetingsLast30Days { get; set; }
    public int ReportsPrev30Days { get; set; }
    public int DirectivesPrev30Days { get; set; }
    public int MeetingsPrev30Days { get; set; }
    public List<StatusCount> ReportsByStatus { get; set; } = new();
    public List<StatusCount> DirectivesByStatus { get; set; } = new();
    public List<StatusCount> MeetingsByStatus { get; set; } = new();
    public int OverdueDirectives { get; set; }
    public int OverdueActionItems { get; set; }
    public int ReportsApproved { get; set; }
    public int ReportsSubmitted { get; set; }
    public int KnowledgeArticles { get; set; }
}

public class StatusCount
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class MonthlyTrend
{
    public string Month { get; set; } = string.Empty;
    public int Reports { get; set; }
    public int Directives { get; set; }
    public int Meetings { get; set; }
}

public class CommitteeMetrics
{
    public int CommitteeId { get; set; }
    public string CommitteeName { get; set; } = string.Empty;
    public HierarchyLevel HierarchyLevel { get; set; }
    public int MemberCount { get; set; }
    public int ReportCount { get; set; }
    public int DirectiveCount { get; set; }
    public int MeetingCount { get; set; }
    public int OpenDirectives { get; set; }
    public int PendingReports { get; set; }
}

public class ComplianceMetrics
{
    public int DirectivesClosedOnTime { get; set; }
    public int DirectivesClosedOverdue { get; set; }
    public int DirectivesStillOpen { get; set; }
    public int DirectivesCurrentlyOverdue { get; set; }
    public int ActionItemsCompletedOnTime { get; set; }
    public int ActionItemsCompletedOverdue { get; set; }
    public int ActionItemsStillOpen { get; set; }
    public int MeetingsFinalized { get; set; }
    public int MeetingsAwaitingConfirmation { get; set; }
}
