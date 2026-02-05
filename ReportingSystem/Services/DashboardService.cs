using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Services;

/// <summary>
/// Service for retrieving dashboard KPIs and statistics for different roles.
/// </summary>
public class DashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        ApplicationDbContext context,
        ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Executive Dashboard

    /// <summary>
    /// Get organization-wide KPIs for executive dashboard.
    /// </summary>
    public async Task<ExecutiveDashboardData> GetExecutiveDashboardAsync()
    {
        var data = new ExecutiveDashboardData();

        // Get current open periods
        var openPeriods = await _context.ReportPeriods
            .Where(p => p.Status == PeriodStatus.Open && p.IsActive)
            .Select(p => p.Id)
            .ToListAsync();

        // Report Statistics
        data.TotalReports = await _context.Reports.CountAsync();
        data.ReportsThisPeriod = await _context.Reports
            .Where(r => openPeriods.Contains(r.ReportPeriodId))
            .CountAsync();

        data.ReportsByStatus = await _context.Reports
            .GroupBy(r => r.Status)
            .Select(g => new StatusCount { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        data.PendingReviews = await _context.Reports
            .Where(r => r.Status == ReportStatus.Submitted || r.Status == ReportStatus.UnderReview)
            .CountAsync();

        // Calculate approval rate (last 30 days)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var recentReviewed = await _context.Reports
            .Where(r => r.ReviewedAt >= thirtyDaysAgo)
            .ToListAsync();

        if (recentReviewed.Count > 0)
        {
            var approved = recentReviewed.Count(r => r.Status == ReportStatus.Approved);
            data.ApprovalRate = Math.Round((double)approved / recentReviewed.Count * 100, 1);
        }

        // Upward Flow Statistics
        data.OpenSuggestedActions = await _context.SuggestedActions
            .Where(a => a.Status == ActionStatus.Submitted || a.Status == ActionStatus.UnderReview)
            .CountAsync();

        data.OpenResourceRequests = await _context.ResourceRequests
            .Where(r => r.Status == ResourceStatus.Submitted || r.Status == ResourceStatus.UnderReview)
            .CountAsync();

        data.OpenSupportRequests = await _context.SupportRequests
            .Where(s => s.Status != SupportStatus.Resolved && s.Status != SupportStatus.Closed)
            .CountAsync();

        // Pending resource request total cost
        data.PendingResourceCost = await _context.ResourceRequests
            .Where(r => r.Status == ResourceStatus.Submitted || r.Status == ResourceStatus.UnderReview)
            .SumAsync(r => r.EstimatedCost ?? 0);

        // Downward Flow Statistics
        data.PendingFeedbackAcknowledgments = await _context.Feedbacks
            .Where(f => (f.Category == FeedbackCategory.Concern || f.Category == FeedbackCategory.Question)
                        && !f.IsAcknowledged && f.Status == FeedbackStatus.Active)
            .CountAsync();

        data.ActiveRecommendations = await _context.Recommendations
            .Where(r => r.Status == RecommendationStatus.Issued || r.Status == RecommendationStatus.Acknowledged
                        || r.Status == RecommendationStatus.InProgress)
            .CountAsync();

        // Organization Statistics
        data.TotalUsers = await _context.Users.Where(u => u.IsActive).CountAsync();
        data.TotalOrgUnits = await _context.OrganizationalUnits.Where(ou => ou.IsActive).CountAsync();

        // User breakdown by role
        data.UsersByRole = await _context.Users
            .Where(u => u.IsActive)
            .GroupBy(u => u.Role)
            .Select(g => new StatusCount { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        // Organization coverage (units with at least one report this period)
        if (openPeriods.Count > 0)
        {
            var unitsWithReports = await _context.Reports
                .Where(r => openPeriods.Contains(r.ReportPeriodId))
                .Select(r => r.SubmittedBy.OrganizationalUnitId)
                .Distinct()
                .CountAsync();

            var totalActiveUnits = await _context.OrganizationalUnits
                .Where(ou => ou.IsActive && ou.Level >= OrgUnitLevel.Department)
                .CountAsync();

            if (totalActiveUnits > 0)
            {
                data.OrganizationCoverage = Math.Round((double)unitsWithReports / totalActiveUnits * 100, 1);
            }
        }

        // Recent Activity (last 7 days)
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        data.RecentReportSubmissions = await _context.Reports
            .Where(r => r.SubmittedAt >= sevenDaysAgo)
            .CountAsync();

        data.RecentApprovals = await _context.Reports
            .Where(r => r.ReviewedAt >= sevenDaysAgo && r.Status == ReportStatus.Approved)
            .CountAsync();

        // Upcoming deadlines (next 7 days)
        var sevenDaysFromNow = DateTime.UtcNow.AddDays(7);
        data.UpcomingDeadlines = await _context.ReportPeriods
            .Where(p => p.Status == PeriodStatus.Open && p.SubmissionDeadline <= sevenDaysFromNow
                        && p.SubmissionDeadline >= DateTime.UtcNow)
            .Select(p => new DeadlineInfo
            {
                PeriodId = p.Id,
                PeriodName = p.Name,
                TemplateName = p.ReportTemplate.Name,
                Deadline = p.SubmissionDeadline,
                DaysRemaining = (int)(p.SubmissionDeadline - DateTime.UtcNow).TotalDays
            })
            .OrderBy(d => d.Deadline)
            .ToListAsync();

        _logger.LogInformation("Retrieved executive dashboard data");
        return data;
    }

    #endregion

    #region Manager Dashboard

    /// <summary>
    /// Get team-focused KPIs for manager dashboard.
    /// </summary>
    public async Task<ManagerDashboardData> GetManagerDashboardAsync(int userId, int? orgUnitId)
    {
        var data = new ManagerDashboardData();

        // Get team members (users in same org unit or subordinate units)
        var teamMemberIds = new List<int>();
        if (orgUnitId.HasValue)
        {
            teamMemberIds = await GetSubordinateUserIdsAsync(orgUnitId.Value);
        }

        // Get current open periods
        var openPeriods = await _context.ReportPeriods
            .Where(p => p.Status == PeriodStatus.Open && p.IsActive)
            .Select(p => p.Id)
            .ToListAsync();

        // Team Reports
        if (teamMemberIds.Count > 0)
        {
            data.TeamReportsTotal = await _context.Reports
                .Where(r => teamMemberIds.Contains(r.SubmittedById))
                .CountAsync();

            data.TeamReportsThisPeriod = await _context.Reports
                .Where(r => teamMemberIds.Contains(r.SubmittedById) && openPeriods.Contains(r.ReportPeriodId))
                .CountAsync();

            data.TeamReportsByStatus = await _context.Reports
                .Where(r => teamMemberIds.Contains(r.SubmittedById) && openPeriods.Contains(r.ReportPeriodId))
                .GroupBy(r => r.Status)
                .Select(g => new StatusCount { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            data.TeamMemberCount = teamMemberIds.Count;
        }

        // Pending Approvals (reports I need to review)
        data.PendingApprovals = await _context.Reports
            .Where(r => r.AssignedReviewerId == userId &&
                        (r.Status == ReportStatus.Submitted || r.Status == ReportStatus.UnderReview))
            .Select(r => new PendingApprovalInfo
            {
                ReportId = r.Id,
                TemplateName = r.ReportTemplate.Name,
                PeriodName = r.ReportPeriod.Name,
                SubmittedBy = r.SubmittedBy.Name,
                SubmittedAt = r.SubmittedAt,
                Status = r.Status,
                DaysPending = r.SubmittedAt.HasValue
                    ? (int)(DateTime.UtcNow - r.SubmittedAt.Value).TotalDays
                    : 0
            })
            .OrderBy(r => r.SubmittedAt)
            .Take(10)
            .ToListAsync();

        data.PendingApprovalCount = await _context.Reports
            .Where(r => r.AssignedReviewerId == userId &&
                        (r.Status == ReportStatus.Submitted || r.Status == ReportStatus.UnderReview))
            .CountAsync();

        // Team Upward Flow Items
        if (teamMemberIds.Count > 0)
        {
            var teamReportIds = await _context.Reports
                .Where(r => teamMemberIds.Contains(r.SubmittedById))
                .Select(r => r.Id)
                .ToListAsync();

            data.TeamSuggestedActions = await _context.SuggestedActions
                .Where(a => teamReportIds.Contains(a.ReportId) &&
                            (a.Status == ActionStatus.Submitted || a.Status == ActionStatus.UnderReview))
                .CountAsync();

            data.TeamResourceRequests = await _context.ResourceRequests
                .Where(r => teamReportIds.Contains(r.ReportId) &&
                            (r.Status == ResourceStatus.Submitted || r.Status == ResourceStatus.UnderReview))
                .CountAsync();

            data.TeamSupportRequests = await _context.SupportRequests
                .Where(s => teamReportIds.Contains(s.ReportId) && s.IsOpen)
                .CountAsync();

            data.TeamPendingResourceCost = await _context.ResourceRequests
                .Where(r => teamReportIds.Contains(r.ReportId) &&
                            (r.Status == ResourceStatus.Submitted || r.Status == ResourceStatus.UnderReview))
                .SumAsync(r => r.EstimatedCost ?? 0);
        }

        // Confirmation Tags I need to respond to
        data.PendingConfirmationTags = await _context.ConfirmationTags
            .Where(c => c.TaggedUserId == userId && c.Status == ConfirmationStatus.Pending)
            .CountAsync();

        // Feedback I've given that's pending acknowledgment
        data.FeedbackPendingAcknowledgment = await _context.Feedbacks
            .Where(f => f.AuthorId == userId && f.IsPendingAcknowledgment)
            .CountAsync();

        // Upcoming deadlines for my team
        if (openPeriods.Count > 0)
        {
            var sevenDaysFromNow = DateTime.UtcNow.AddDays(7);
            data.UpcomingDeadlines = await _context.ReportPeriods
                .Where(p => p.Status == PeriodStatus.Open && p.SubmissionDeadline <= sevenDaysFromNow
                            && p.SubmissionDeadline >= DateTime.UtcNow)
                .Select(p => new DeadlineInfo
                {
                    PeriodId = p.Id,
                    PeriodName = p.Name,
                    TemplateName = p.ReportTemplate.Name,
                    Deadline = p.SubmissionDeadline,
                    DaysRemaining = (int)(p.SubmissionDeadline - DateTime.UtcNow).TotalDays
                })
                .OrderBy(d => d.Deadline)
                .ToListAsync();
        }

        _logger.LogInformation("Retrieved manager dashboard data for user {UserId}", userId);
        return data;
    }

    #endregion

    #region Originator Dashboard

    /// <summary>
    /// Get personal report KPIs for originator dashboard.
    /// </summary>
    public async Task<OriginatorDashboardData> GetOriginatorDashboardAsync(int userId)
    {
        var data = new OriginatorDashboardData();

        // My Reports
        data.MyReports = await _context.Reports
            .Where(r => r.SubmittedById == userId)
            .Select(r => new MyReportInfo
            {
                ReportId = r.Id,
                TemplateName = r.ReportTemplate.Name,
                PeriodName = r.ReportPeriod.Name,
                Status = r.Status,
                SubmittedAt = r.SubmittedAt,
                ReviewedAt = r.ReviewedAt,
                IsEditable = r.IsEditable
            })
            .OrderByDescending(r => r.SubmittedAt ?? DateTime.MinValue)
            .Take(10)
            .ToListAsync();

        data.MyReportsByStatus = await _context.Reports
            .Where(r => r.SubmittedById == userId)
            .GroupBy(r => r.Status)
            .Select(g => new StatusCount { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        data.TotalMyReports = await _context.Reports
            .Where(r => r.SubmittedById == userId)
            .CountAsync();

        data.DraftReports = await _context.Reports
            .Where(r => r.SubmittedById == userId && r.Status == ReportStatus.Draft)
            .CountAsync();

        // Get my report IDs for upward flow stats
        var myReportIds = await _context.Reports
            .Where(r => r.SubmittedById == userId)
            .Select(r => r.Id)
            .ToListAsync();

        // My Upward Flow Items
        data.MySuggestedActionsByStatus = await _context.SuggestedActions
            .Where(a => myReportIds.Contains(a.ReportId))
            .GroupBy(a => a.Status)
            .Select(g => new StatusCount { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        data.MyResourceRequestsByStatus = await _context.ResourceRequests
            .Where(r => myReportIds.Contains(r.ReportId))
            .GroupBy(r => r.Status)
            .Select(g => new StatusCount { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        data.MySupportRequestsByStatus = await _context.SupportRequests
            .Where(s => myReportIds.Contains(s.ReportId))
            .GroupBy(s => s.Status)
            .Select(g => new StatusCount { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        // Feedback received on my reports
        data.FeedbackReceived = await _context.Feedbacks
            .Where(f => myReportIds.Contains(f.ReportId) && !f.ParentFeedbackId.HasValue)
            .CountAsync();

        data.FeedbackPendingMyAcknowledgment = await _context.Feedbacks
            .Where(f => myReportIds.Contains(f.ReportId) && f.IsPendingAcknowledgment)
            .CountAsync();

        // Confirmation tags I've requested
        data.MyConfirmationTagsPending = await _context.ConfirmationTags
            .Where(c => c.RequestedById == userId && c.Status == ConfirmationStatus.Pending)
            .CountAsync();

        data.MyConfirmationTagsConfirmed = await _context.ConfirmationTags
            .Where(c => c.RequestedById == userId && c.Status == ConfirmationStatus.Confirmed)
            .CountAsync();

        // Confirmation tags I need to respond to
        data.ConfirmationTagsToRespond = await _context.ConfirmationTags
            .Where(c => c.TaggedUserId == userId && c.Status == ConfirmationStatus.Pending)
            .CountAsync();

        // Decisions on my requests
        data.DecisionsReceived = await _context.Decisions
            .Where(d => myReportIds.Contains(d.ReportId ?? 0))
            .CountAsync();

        data.DecisionsPendingAcknowledgment = await _context.Decisions
            .Where(d => myReportIds.Contains(d.ReportId ?? 0) && !d.IsAcknowledged)
            .CountAsync();

        // Upcoming deadlines
        var sevenDaysFromNow = DateTime.UtcNow.AddDays(7);
        data.UpcomingDeadlines = await _context.ReportPeriods
            .Where(p => p.Status == PeriodStatus.Open && p.SubmissionDeadline <= sevenDaysFromNow
                        && p.SubmissionDeadline >= DateTime.UtcNow)
            .Select(p => new DeadlineInfo
            {
                PeriodId = p.Id,
                PeriodName = p.Name,
                TemplateName = p.ReportTemplate.Name,
                Deadline = p.SubmissionDeadline,
                DaysRemaining = (int)(p.SubmissionDeadline - DateTime.UtcNow).TotalDays
            })
            .OrderBy(d => d.Deadline)
            .ToListAsync();

        // Recent recommendations targeting me
        var user = await _context.Users
            .Include(u => u.OrganizationalUnit)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user != null)
        {
            data.ActiveRecommendationsForMe = await _context.Recommendations
                .Where(r => (r.TargetUserId == userId ||
                             (r.TargetOrgUnitId == user.OrganizationalUnitId && r.TargetScope == RecommendationScope.Team))
                            && (r.Status == RecommendationStatus.Issued || r.Status == RecommendationStatus.Acknowledged
                                || r.Status == RecommendationStatus.InProgress))
                .CountAsync();
        }

        _logger.LogInformation("Retrieved originator dashboard data for user {UserId}", userId);
        return data;
    }

    #endregion

    #region Reviewer Dashboard

    /// <summary>
    /// Get review workload KPIs for reviewer dashboard.
    /// </summary>
    public async Task<ReviewerDashboardData> GetReviewerDashboardAsync(int userId)
    {
        var data = new ReviewerDashboardData();

        // Reports assigned to me for review
        data.AssignedReports = await _context.Reports
            .Where(r => r.AssignedReviewerId == userId)
            .CountAsync();

        data.PendingReviews = await _context.Reports
            .Where(r => r.AssignedReviewerId == userId &&
                        (r.Status == ReportStatus.Submitted || r.Status == ReportStatus.UnderReview))
            .Select(r => new PendingReviewInfo
            {
                ReportId = r.Id,
                TemplateName = r.ReportTemplate.Name,
                PeriodName = r.ReportPeriod.Name,
                SubmittedBy = r.SubmittedBy.Name,
                OrgUnitName = r.SubmittedBy.OrganizationalUnit != null ? r.SubmittedBy.OrganizationalUnit.Name : "N/A",
                SubmittedAt = r.SubmittedAt,
                Status = r.Status,
                DaysPending = r.SubmittedAt.HasValue
                    ? (int)(DateTime.UtcNow - r.SubmittedAt.Value).TotalDays
                    : 0
            })
            .OrderBy(r => r.SubmittedAt)
            .ToListAsync();

        data.PendingReviewCount = data.PendingReviews.Count;

        // My review statistics
        data.ReviewsByOutcome = await _context.Reports
            .Where(r => r.AssignedReviewerId == userId && r.ReviewedAt.HasValue)
            .GroupBy(r => r.Status)
            .Select(g => new StatusCount { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        data.TotalReviewsCompleted = await _context.Reports
            .Where(r => r.AssignedReviewerId == userId && r.ReviewedAt.HasValue)
            .CountAsync();

        // Reviews completed in last 30 days
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        data.ReviewsLast30Days = await _context.Reports
            .Where(r => r.AssignedReviewerId == userId && r.ReviewedAt >= thirtyDaysAgo)
            .CountAsync();

        // Calculate approval rate
        var myReviewed = await _context.Reports
            .Where(r => r.AssignedReviewerId == userId && r.ReviewedAt.HasValue)
            .ToListAsync();

        if (myReviewed.Count > 0)
        {
            var approved = myReviewed.Count(r => r.Status == ReportStatus.Approved);
            data.MyApprovalRate = Math.Round((double)approved / myReviewed.Count * 100, 1);
        }

        // Confirmation tags I need to respond to
        data.PendingConfirmationTags = await _context.ConfirmationTags
            .Where(c => c.TaggedUserId == userId && c.Status == ConfirmationStatus.Pending)
            .CountAsync();

        // Comments I've made
        data.MyCommentsCount = await _context.Comments
            .Where(c => c.AuthorId == userId && c.Status == CommentStatus.Active)
            .CountAsync();

        // Feedback I've given
        data.MyFeedbackCount = await _context.Feedbacks
            .Where(f => f.AuthorId == userId)
            .CountAsync();

        // Workload distribution by org unit
        data.WorkloadByOrgUnit = await _context.Reports
            .Where(r => r.AssignedReviewerId == userId &&
                        (r.Status == ReportStatus.Submitted || r.Status == ReportStatus.UnderReview))
            .GroupBy(r => r.SubmittedBy.OrganizationalUnit != null ? r.SubmittedBy.OrganizationalUnit.Name : "Unassigned")
            .Select(g => new StatusCount { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        _logger.LogInformation("Retrieved reviewer dashboard data for user {UserId}", userId);
        return data;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Get all user IDs in an org unit and its subordinate units.
    /// </summary>
    private async Task<List<int>> GetSubordinateUserIdsAsync(int orgUnitId)
    {
        // Get all subordinate org unit IDs recursively
        var allOrgUnitIds = new List<int> { orgUnitId };
        var queue = new Queue<int>();
        queue.Enqueue(orgUnitId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            var childIds = await _context.OrganizationalUnits
                .Where(ou => ou.ParentId == currentId && ou.IsActive)
                .Select(ou => ou.Id)
                .ToListAsync();

            foreach (var childId in childIds)
            {
                allOrgUnitIds.Add(childId);
                queue.Enqueue(childId);
            }
        }

        // Get all users in these org units
        return await _context.Users
            .Where(u => u.IsActive && u.OrganizationalUnitId.HasValue && allOrgUnitIds.Contains(u.OrganizationalUnitId.Value))
            .Select(u => u.Id)
            .ToListAsync();
    }

    /// <summary>
    /// Get user by ID with org unit.
    /// </summary>
    public async Task<User?> GetUserWithOrgUnitAsync(int userId)
    {
        return await _context.Users
            .Include(u => u.OrganizationalUnit)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    #endregion

    #region Chart Data Methods

    /// <summary>
    /// Get report activity trend for the last N days.
    /// </summary>
    public async Task<ReportActivityTrend> GetReportActivityTrendAsync(int days = 30)
    {
        var trend = new ReportActivityTrend();
        var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);

        // Get all reports created/submitted/reviewed in the period
        var reports = await _context.Reports
            .Where(r => r.CreatedAt >= startDate ||
                        r.SubmittedAt >= startDate ||
                        r.ReviewedAt >= startDate)
            .Select(r => new
            {
                r.CreatedAt,
                r.SubmittedAt,
                r.ReviewedAt,
                r.Status
            })
            .ToListAsync();

        // Build daily data
        for (int i = 0; i < days; i++)
        {
            var date = startDate.AddDays(i);
            var nextDate = date.AddDays(1);

            trend.Labels.Add(date.ToString("MMM dd"));

            trend.Created.Add(reports.Count(r =>
                r.CreatedAt >= date && r.CreatedAt < nextDate));

            trend.Submitted.Add(reports.Count(r =>
                r.SubmittedAt.HasValue && r.SubmittedAt >= date && r.SubmittedAt < nextDate));

            trend.Approved.Add(reports.Count(r =>
                r.ReviewedAt.HasValue && r.ReviewedAt >= date && r.ReviewedAt < nextDate &&
                r.Status == ReportStatus.Approved));

            trend.Rejected.Add(reports.Count(r =>
                r.ReviewedAt.HasValue && r.ReviewedAt >= date && r.ReviewedAt < nextDate &&
                r.Status == ReportStatus.Rejected));
        }

        return trend;
    }

    /// <summary>
    /// Get upward flow distribution by type.
    /// </summary>
    public async Task<UpwardFlowDistribution> GetUpwardFlowDistributionAsync()
    {
        var distribution = new UpwardFlowDistribution();

        // Suggested Actions by status
        var actions = await _context.SuggestedActions
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        distribution.SuggestedActionsByStatus = actions.ToDictionary(a => a.Status, a => a.Count);
        distribution.TotalSuggestedActions = actions.Sum(a => a.Count);

        // Resource Requests by status
        var resources = await _context.ResourceRequests
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        distribution.ResourceRequestsByStatus = resources.ToDictionary(r => r.Status, r => r.Count);
        distribution.TotalResourceRequests = resources.Sum(r => r.Count);

        // Support Requests by status
        var support = await _context.SupportRequests
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        distribution.SupportRequestsByStatus = support.ToDictionary(s => s.Status, s => s.Count);
        distribution.TotalSupportRequests = support.Sum(s => s.Count);

        // Summary totals
        distribution.Labels = new List<string> { "Suggested Actions", "Resource Requests", "Support Requests" };
        distribution.Totals = new List<int>
        {
            distribution.TotalSuggestedActions,
            distribution.TotalResourceRequests,
            distribution.TotalSupportRequests
        };

        return distribution;
    }

    /// <summary>
    /// Get reviewer performance data for charts.
    /// </summary>
    public async Task<ReviewerPerformanceData> GetReviewerPerformanceAsync(int userId, int days = 30)
    {
        var data = new ReviewerPerformanceData();
        var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);

        // Get reviews in period
        var reviews = await _context.Reports
            .Where(r => r.AssignedReviewerId == userId && r.ReviewedAt >= startDate)
            .Select(r => new
            {
                r.ReviewedAt,
                r.Status
            })
            .ToListAsync();

        // Weekly breakdown
        var weeks = (days + 6) / 7;
        for (int i = 0; i < weeks; i++)
        {
            var weekStart = startDate.AddDays(i * 7);
            var weekEnd = weekStart.AddDays(7);

            data.WeekLabels.Add($"Week {i + 1}");

            var weekReviews = reviews.Where(r =>
                r.ReviewedAt >= weekStart && r.ReviewedAt < weekEnd).ToList();

            data.ReviewsPerWeek.Add(weekReviews.Count);
            data.ApprovalsPerWeek.Add(weekReviews.Count(r => r.Status == ReportStatus.Approved));
            data.RejectionsPerWeek.Add(weekReviews.Count(r => r.Status == ReportStatus.Rejected));
        }

        return data;
    }

    #endregion
}

#region Dashboard Data Classes

/// <summary>
/// Executive dashboard data container.
/// </summary>
public class ExecutiveDashboardData
{
    // Report Statistics
    public int TotalReports { get; set; }
    public int ReportsThisPeriod { get; set; }
    public List<StatusCount> ReportsByStatus { get; set; } = new();
    public int PendingReviews { get; set; }
    public double ApprovalRate { get; set; }

    // Upward Flow Statistics
    public int OpenSuggestedActions { get; set; }
    public int OpenResourceRequests { get; set; }
    public int OpenSupportRequests { get; set; }
    public decimal PendingResourceCost { get; set; }

    // Downward Flow Statistics
    public int PendingFeedbackAcknowledgments { get; set; }
    public int ActiveRecommendations { get; set; }

    // Organization Statistics
    public int TotalUsers { get; set; }
    public int TotalOrgUnits { get; set; }
    public List<StatusCount> UsersByRole { get; set; } = new();
    public double OrganizationCoverage { get; set; }

    // Recent Activity
    public int RecentReportSubmissions { get; set; }
    public int RecentApprovals { get; set; }
    public List<DeadlineInfo> UpcomingDeadlines { get; set; } = new();
}

/// <summary>
/// Manager dashboard data container.
/// </summary>
public class ManagerDashboardData
{
    // Team Statistics
    public int TeamMemberCount { get; set; }
    public int TeamReportsTotal { get; set; }
    public int TeamReportsThisPeriod { get; set; }
    public List<StatusCount> TeamReportsByStatus { get; set; } = new();

    // Pending Approvals
    public List<PendingApprovalInfo> PendingApprovals { get; set; } = new();
    public int PendingApprovalCount { get; set; }

    // Team Upward Flow
    public int TeamSuggestedActions { get; set; }
    public int TeamResourceRequests { get; set; }
    public int TeamSupportRequests { get; set; }
    public decimal TeamPendingResourceCost { get; set; }

    // Workflow
    public int PendingConfirmationTags { get; set; }
    public int FeedbackPendingAcknowledgment { get; set; }

    // Deadlines
    public List<DeadlineInfo> UpcomingDeadlines { get; set; } = new();
}

/// <summary>
/// Originator dashboard data container.
/// </summary>
public class OriginatorDashboardData
{
    // My Reports
    public List<MyReportInfo> MyReports { get; set; } = new();
    public List<StatusCount> MyReportsByStatus { get; set; } = new();
    public int TotalMyReports { get; set; }
    public int DraftReports { get; set; }

    // My Upward Flow
    public List<StatusCount> MySuggestedActionsByStatus { get; set; } = new();
    public List<StatusCount> MyResourceRequestsByStatus { get; set; } = new();
    public List<StatusCount> MySupportRequestsByStatus { get; set; } = new();

    // Feedback & Decisions
    public int FeedbackReceived { get; set; }
    public int FeedbackPendingMyAcknowledgment { get; set; }
    public int DecisionsReceived { get; set; }
    public int DecisionsPendingAcknowledgment { get; set; }

    // Confirmation Tags
    public int MyConfirmationTagsPending { get; set; }
    public int MyConfirmationTagsConfirmed { get; set; }
    public int ConfirmationTagsToRespond { get; set; }

    // Recommendations
    public int ActiveRecommendationsForMe { get; set; }

    // Deadlines
    public List<DeadlineInfo> UpcomingDeadlines { get; set; } = new();
}

/// <summary>
/// Reviewer dashboard data container.
/// </summary>
public class ReviewerDashboardData
{
    // Review Workload
    public int AssignedReports { get; set; }
    public List<PendingReviewInfo> PendingReviews { get; set; } = new();
    public int PendingReviewCount { get; set; }

    // My Statistics
    public List<StatusCount> ReviewsByOutcome { get; set; } = new();
    public int TotalReviewsCompleted { get; set; }
    public int ReviewsLast30Days { get; set; }
    public double MyApprovalRate { get; set; }

    // Workflow
    public int PendingConfirmationTags { get; set; }
    public int MyCommentsCount { get; set; }
    public int MyFeedbackCount { get; set; }

    // Workload Distribution
    public List<StatusCount> WorkloadByOrgUnit { get; set; } = new();
}

/// <summary>
/// Generic status count for grouping.
/// </summary>
public class StatusCount
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
/// Deadline information.
/// </summary>
public class DeadlineInfo
{
    public int PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public DateTime Deadline { get; set; }
    public int DaysRemaining { get; set; }
}

/// <summary>
/// Pending approval information for managers.
/// </summary>
public class PendingApprovalInfo
{
    public int ReportId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string PeriodName { get; set; } = string.Empty;
    public string SubmittedBy { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public int DaysPending { get; set; }
}

/// <summary>
/// Pending review information for reviewers.
/// </summary>
public class PendingReviewInfo
{
    public int ReportId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string PeriodName { get; set; } = string.Empty;
    public string SubmittedBy { get; set; } = string.Empty;
    public string OrgUnitName { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public int DaysPending { get; set; }
}

/// <summary>
/// My report information for originators.
/// </summary>
public class MyReportInfo
{
    public int ReportId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string PeriodName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public bool IsEditable { get; set; }
}

/// <summary>
/// Report activity trend data for line charts.
/// </summary>
public class ReportActivityTrend
{
    public List<string> Labels { get; set; } = new();
    public List<int> Created { get; set; } = new();
    public List<int> Submitted { get; set; } = new();
    public List<int> Approved { get; set; } = new();
    public List<int> Rejected { get; set; } = new();
}

/// <summary>
/// Upward flow distribution data for pie/bar charts.
/// </summary>
public class UpwardFlowDistribution
{
    public List<string> Labels { get; set; } = new();
    public List<int> Totals { get; set; } = new();

    public Dictionary<string, int> SuggestedActionsByStatus { get; set; } = new();
    public int TotalSuggestedActions { get; set; }

    public Dictionary<string, int> ResourceRequestsByStatus { get; set; } = new();
    public int TotalResourceRequests { get; set; }

    public Dictionary<string, int> SupportRequestsByStatus { get; set; } = new();
    public int TotalSupportRequests { get; set; }
}

/// <summary>
/// Reviewer performance data for weekly charts.
/// </summary>
public class ReviewerPerformanceData
{
    public List<string> WeekLabels { get; set; } = new();
    public List<int> ReviewsPerWeek { get; set; } = new();
    public List<int> ApprovalsPerWeek { get; set; } = new();
    public List<int> RejectionsPerWeek { get; set; } = new();
}

#endregion
