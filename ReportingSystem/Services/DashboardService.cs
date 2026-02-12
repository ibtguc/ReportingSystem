using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Services;

public class DashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ── Chairman Dashboard (FR-4.7.2.1) ──

    public async Task<ChairmanDashboardData> GetChairmanDashboardAsync()
    {
        var data = new ChairmanDashboardData();

        // Pending executive summaries
        data.PendingExecutiveSummaries = await _context.Reports
            .Include(r => r.Author).Include(r => r.Committee)
            .Where(r => r.ReportType == ReportType.ExecutiveSummary
                && r.Status == ReportStatus.Submitted)
            .OrderByDescending(r => r.CreatedAt)
            .Take(10)
            .ToListAsync();

        // Open directives issued by Chairman/Chairman's Office
        data.OpenDirectives = await _context.Directives
            .Include(d => d.Issuer).Include(d => d.TargetCommittee)
            .Where(d => d.Status != DirectiveStatus.Closed && d.Status != DirectiveStatus.Verified)
            .OrderByDescending(d => d.Priority).ThenBy(d => d.Deadline)
            .Take(15)
            .ToListAsync();

        // Overdue items
        var now = DateTime.UtcNow;
        data.OverdueDirectives = await _context.Directives
            .Include(d => d.TargetCommittee)
            .Where(d => d.Deadline.HasValue && d.Deadline.Value < now
                && d.Status != DirectiveStatus.Closed && d.Status != DirectiveStatus.Verified)
            .OrderBy(d => d.Deadline)
            .Take(10)
            .ToListAsync();

        data.OverdueActionItems = await _context.ActionItems
            .Include(a => a.AssignedTo).Include(a => a.Meeting).ThenInclude(m => m.Committee)
            .Where(a => a.Deadline.HasValue && a.Deadline.Value < now
                && a.Status != ActionItemStatus.Completed && a.Status != ActionItemStatus.Verified)
            .OrderBy(a => a.Deadline)
            .Take(10)
            .ToListAsync();

        // Health metrics
        var thirtyDaysAgo = now.AddDays(-30);
        data.ReportsSubmittedThisMonth = await _context.Reports
            .CountAsync(r => r.CreatedAt >= thirtyDaysAgo);
        data.DirectivesIssuedThisMonth = await _context.Directives
            .CountAsync(d => d.CreatedAt >= thirtyDaysAgo);
        data.MeetingsHeldThisMonth = await _context.Meetings
            .CountAsync(m => m.ScheduledAt >= thirtyDaysAgo && m.Status != MeetingStatus.Cancelled);
        data.TotalOverdueItems = data.OverdueDirectives.Count +
            await _context.ActionItems.CountAsync(a => a.Deadline.HasValue && a.Deadline.Value < now
                && a.Status != ActionItemStatus.Completed && a.Status != ActionItemStatus.Verified);

        return data;
    }

    // ── Chairman's Office Dashboard (FR-4.7.2.4) ──

    public async Task<OfficeDashboardData> GetOfficeDashboardAsync()
    {
        var data = new OfficeDashboardData();
        var now = DateTime.UtcNow;

        // Incoming reports from Top Level Committee (L0)
        data.IncomingReports = await _context.Reports
            .Include(r => r.Author).Include(r => r.Committee)
            .Where(r => r.Committee!.HierarchyLevel == HierarchyLevel.TopLevel
                && (r.Status == ReportStatus.Submitted || r.Status == ReportStatus.Approved))
            .OrderByDescending(r => r.CreatedAt)
            .Take(10)
            .ToListAsync();

        // Outgoing executive summaries (to Chairman)
        data.OutgoingExecutiveSummaries = await _context.Reports
            .Include(r => r.Author).Include(r => r.Committee)
            .Where(r => r.ReportType == ReportType.ExecutiveSummary)
            .OrderByDescending(r => r.CreatedAt)
            .Take(10)
            .ToListAsync();

        // Pending directive relays
        data.PendingDirectiveRelays = await _context.Directives
            .Include(d => d.Issuer).Include(d => d.TargetCommittee)
            .Where(d => d.Status == DirectiveStatus.Issued || d.Status == DirectiveStatus.Delivered)
            .OrderByDescending(d => d.Priority).ThenByDescending(d => d.CreatedAt)
            .Take(10)
            .ToListAsync();

        // Cross-stream status
        data.ReportsAwaitingReview = await _context.Reports
            .CountAsync(r => r.Status == ReportStatus.Submitted);
        data.ActiveDirectives = await _context.Directives
            .CountAsync(d => d.Status != DirectiveStatus.Closed);
        data.UpcomingMeetings = await _context.Meetings
            .CountAsync(m => m.ScheduledAt >= now && m.Status == MeetingStatus.Scheduled);

        return data;
    }

    // ── Committee Head Dashboard (FR-4.7.2.2) ──

    public async Task<CommitteeHeadDashboardData> GetCommitteeHeadDashboardAsync(int userId)
    {
        var data = new CommitteeHeadDashboardData();
        var now = DateTime.UtcNow;

        // Get committees where user is head
        var headCommitteeIds = await _context.CommitteeMemberships
            .Where(m => m.UserId == userId && m.Role == CommitteeRole.Head && m.EffectiveTo == null)
            .Select(m => m.CommitteeId)
            .ToListAsync();

        data.ManagedCommittees = await _context.Committees
            .Where(c => headCommitteeIds.Contains(c.Id))
            .ToListAsync();

        // Reports awaiting collective approval in my committees (exclude SkipApprovals)
        data.PendingReports = await _context.Reports
            .Include(r => r.Author).Include(r => r.Committee).Include(r => r.Approvals)
            .Where(r => headCommitteeIds.Contains(r.CommitteeId)
                && r.Status == ReportStatus.Submitted
                && !r.SkipApprovals)
            .OrderByDescending(r => r.CreatedAt)
            .Take(15)
            .ToListAsync();

        // Reports past 3-day deadline that head can finalize (exclude SkipApprovals)
        var threeDaysAgo = now.AddDays(-3);
        data.FinalizableReports = await _context.Reports
            .Include(r => r.Author).Include(r => r.Committee).Include(r => r.Approvals)
            .Where(r => headCommitteeIds.Contains(r.CommitteeId)
                && r.Status == ReportStatus.Submitted
                && !r.SkipApprovals
                && r.SubmittedAt.HasValue && r.SubmittedAt.Value <= threeDaysAgo)
            .OrderBy(r => r.SubmittedAt)
            .Take(10)
            .ToListAsync();

        // Open directives targeting my committees
        data.OpenDirectives = await _context.Directives
            .Include(d => d.Issuer).Include(d => d.TargetCommittee)
            .Where(d => headCommitteeIds.Contains(d.TargetCommitteeId)
                && d.Status != DirectiveStatus.Closed)
            .OrderByDescending(d => d.Priority).ThenBy(d => d.Deadline)
            .Take(10)
            .ToListAsync();

        // Upcoming meetings for my committees
        data.UpcomingMeetings = await _context.Meetings
            .Include(m => m.Committee).Include(m => m.Moderator)
            .Where(m => headCommitteeIds.Contains(m.CommitteeId)
                && m.ScheduledAt >= now && m.Status != MeetingStatus.Cancelled)
            .OrderBy(m => m.ScheduledAt)
            .Take(10)
            .ToListAsync();

        // Overdue action items in my committees
        data.OverdueActionItems = await _context.ActionItems
            .Include(a => a.AssignedTo).Include(a => a.Meeting).ThenInclude(m => m.Committee)
            .Where(a => a.Meeting.CommitteeId != 0
                && headCommitteeIds.Contains(a.Meeting.CommitteeId)
                && a.Deadline.HasValue && a.Deadline.Value < now
                && a.Status != ActionItemStatus.Completed && a.Status != ActionItemStatus.Verified)
            .OrderBy(a => a.Deadline)
            .Take(10)
            .ToListAsync();

        return data;
    }

    // ── Committee Activities Dashboard ──

    public async Task<CommitteeActivitiesData> GetCommitteeActivitiesAsync(int userId)
    {
        var data = new CommitteeActivitiesData();
        var now = DateTime.UtcNow;

        // 1. Get committees the user is a direct member of
        var directCommitteeIds = await _context.CommitteeMemberships
            .Where(m => m.UserId == userId && m.EffectiveTo == null)
            .Select(m => m.CommitteeId)
            .Distinct()
            .ToListAsync();

        if (!directCommitteeIds.Any())
            return data;

        // 2. Load all committees to compute hierarchy in memory
        var allCommittees = await _context.Committees.ToListAsync();
        var lookup = allCommittees.ToDictionary(c => c.Id);
        var directSet = new HashSet<int>(directCommitteeIds);

        // 3. Compute all descendant committee IDs (excluding direct memberships)
        var descendantIds = new HashSet<int>();
        foreach (var cid in directCommitteeIds)
        {
            CollectDescendants(cid, allCommittees, directSet, descendantIds);
        }

        // 4. Load activities for direct committees
        var allTargetIds = directCommitteeIds.Union(descendantIds).ToList();

        var reports = await _context.Reports
            .Include(r => r.Author).Include(r => r.Committee).Include(r => r.Approvals)
            .Where(r => allTargetIds.Contains(r.CommitteeId)
                && (r.Status == ReportStatus.Submitted || r.Status == ReportStatus.FeedbackRequested))
            .OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt)
            .ToListAsync();

        var directives = await _context.Directives
            .Include(d => d.Issuer).Include(d => d.TargetCommittee)
            .Where(d => allTargetIds.Contains(d.TargetCommitteeId)
                && d.Status != DirectiveStatus.Closed && d.Status != DirectiveStatus.Verified)
            .OrderByDescending(d => d.Priority).ThenBy(d => d.Deadline)
            .ToListAsync();

        var meetings = await _context.Meetings
            .Include(m => m.Committee).Include(m => m.Moderator)
            .Where(m => allTargetIds.Contains(m.CommitteeId)
                && m.ScheduledAt >= now && m.Status != MeetingStatus.Cancelled)
            .OrderBy(m => m.ScheduledAt)
            .ToListAsync();

        var actionItems = await _context.ActionItems
            .Include(a => a.AssignedTo).Include(a => a.Meeting).ThenInclude(m => m.Committee)
            .Where(a => allTargetIds.Contains(a.Meeting.CommitteeId)
                && a.Status != ActionItemStatus.Completed && a.Status != ActionItemStatus.Verified)
            .OrderBy(a => a.Deadline)
            .ToListAsync();

        // 5. Group into direct committees
        data.MyCommitteeActivities = BuildActivityGroups(
            directCommitteeIds, lookup, reports, directives, meetings, actionItems);

        // 6. Group into descendant committees
        if (descendantIds.Any())
        {
            data.SubCommitteeActivities = BuildActivityGroups(
                descendantIds.ToList(), lookup, reports, directives, meetings, actionItems);
        }

        return data;
    }

    private static void CollectDescendants(int parentId, List<Committee> all, HashSet<int> exclude, HashSet<int> result)
    {
        var children = all.Where(c => c.ParentCommitteeId == parentId).ToList();
        foreach (var child in children)
        {
            // Only add if not a direct membership (avoid duplication)
            if (!exclude.Contains(child.Id))
                result.Add(child.Id);
            // Always recurse to get deeper descendants
            CollectDescendants(child.Id, all, exclude, result);
        }
    }

    private static List<CommitteeActivityGroup> BuildActivityGroups(
        List<int> committeeIds, Dictionary<int, Committee> lookup,
        List<Report> reports, List<Directive> directives,
        List<Meeting> meetings, List<ActionItem> actionItems)
    {
        var groups = new List<CommitteeActivityGroup>();
        foreach (var cid in committeeIds)
        {
            if (!lookup.TryGetValue(cid, out var committee))
                continue;

            var group = new CommitteeActivityGroup
            {
                Committee = committee,
                PendingReports = reports.Where(r => r.CommitteeId == cid).Take(5).ToList(),
                OpenDirectives = directives.Where(d => d.TargetCommitteeId == cid).Take(5).ToList(),
                UpcomingMeetings = meetings.Where(m => m.CommitteeId == cid).Take(5).ToList(),
                PendingActionItems = actionItems.Where(a => a.Meeting.CommitteeId == cid).Take(5).ToList()
            };

            if (group.TotalCount > 0)
                groups.Add(group);
        }
        return groups.OrderBy(g => g.Committee.HierarchyLevel).ThenBy(g => g.Committee.Name).ToList();
    }

    // ── Personal Dashboard (FR-4.7.2.3) ──

    public async Task<PersonalDashboardData> GetPersonalDashboardAsync(int userId)
    {
        var data = new PersonalDashboardData();
        var now = DateTime.UtcNow;

        // My draft reports
        data.DraftReports = await _context.Reports
            .Include(r => r.Committee)
            .Where(r => r.AuthorId == userId && r.Status == ReportStatus.Draft)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .ToListAsync();

        // Reports where feedback was requested
        data.FeedbackRequested = await _context.Reports
            .Include(r => r.Committee)
            .Where(r => r.AuthorId == userId && r.Status == ReportStatus.FeedbackRequested)
            .OrderByDescending(r => r.UpdatedAt)
            .Take(5)
            .ToListAsync();

        // Reports awaiting my approval (submitted in my committees, not authored by me, not yet approved by me)
        var myCommitteeIds = await _context.CommitteeMemberships
            .Where(m => m.UserId == userId && m.EffectiveTo == null)
            .Select(m => m.CommitteeId)
            .ToListAsync();

        data.ReportsAwaitingMyApproval = await _context.Reports
            .Include(r => r.Author).Include(r => r.Committee).Include(r => r.Approvals)
            .Where(r => myCommitteeIds.Contains(r.CommitteeId)
                && r.Status == ReportStatus.Submitted
                && !r.SkipApprovals
                && r.AuthorId != userId
                && !r.Approvals.Any(a => a.UserId == userId))
            .OrderByDescending(r => r.CreatedAt)
            .Take(10)
            .ToListAsync();

        // Directives I need to act on

        data.PendingDirectives = await _context.Directives
            .Include(d => d.Issuer).Include(d => d.TargetCommittee)
            .Where(d => (d.TargetUserId == userId || myCommitteeIds.Contains(d.TargetCommitteeId))
                && (d.Status == DirectiveStatus.Delivered || d.Status == DirectiveStatus.Acknowledged || d.Status == DirectiveStatus.InProgress))
            .OrderByDescending(d => d.Priority).ThenBy(d => d.Deadline)
            .Take(10)
            .ToListAsync();

        // Meetings I need to RSVP or confirm minutes
        data.PendingMeetings = await _context.Meetings
            .Include(m => m.Committee).Include(m => m.Attendees)
            .Where(m => m.Attendees.Any(a => a.UserId == userId
                && ((m.Status == MeetingStatus.Scheduled && a.RsvpStatus == RsvpStatus.Pending)
                    || (m.Status == MeetingStatus.MinutesReview && a.ConfirmationStatus == ConfirmationStatus.Pending))))
            .OrderBy(m => m.ScheduledAt)
            .Take(10)
            .ToListAsync();

        // Action items assigned to me
        data.MyActionItems = await _context.ActionItems
            .Include(a => a.Meeting).ThenInclude(m => m.Committee)
            .Where(a => a.AssignedToId == userId
                && a.Status != ActionItemStatus.Completed && a.Status != ActionItemStatus.Verified)
            .OrderBy(a => a.Deadline)
            .Take(10)
            .ToListAsync();

        // My committees
        data.MyCommittees = await _context.Committees
            .Where(c => c.Memberships.Any(m => m.UserId == userId && m.EffectiveTo == null))
            .OrderBy(c => c.HierarchyLevel).ThenBy(c => c.Name)
            .ToListAsync();

        // Recent activity (notifications)
        data.RecentNotifications = await _context.Notifications
            .Where(n => n.UserId == userId.ToString())
            .OrderByDescending(n => n.CreatedAt)
            .Take(10)
            .ToListAsync();

        return data;
    }
}

// ── DTOs ──

public class ChairmanDashboardData
{
    public List<Report> PendingExecutiveSummaries { get; set; } = new();
    public List<Directive> OpenDirectives { get; set; } = new();
    public List<Directive> OverdueDirectives { get; set; } = new();
    public List<ActionItem> OverdueActionItems { get; set; } = new();
    public int ReportsSubmittedThisMonth { get; set; }
    public int DirectivesIssuedThisMonth { get; set; }
    public int MeetingsHeldThisMonth { get; set; }
    public int TotalOverdueItems { get; set; }
}

public class OfficeDashboardData
{
    public List<Report> IncomingReports { get; set; } = new();
    public List<Report> OutgoingExecutiveSummaries { get; set; } = new();
    public List<Directive> PendingDirectiveRelays { get; set; } = new();
    public int ReportsAwaitingReview { get; set; }
    public int ActiveDirectives { get; set; }
    public int UpcomingMeetings { get; set; }
}

public class CommitteeHeadDashboardData
{
    public List<Committee> ManagedCommittees { get; set; } = new();
    public List<Report> PendingReports { get; set; } = new();
    public List<Report> FinalizableReports { get; set; } = new();
    public List<Directive> OpenDirectives { get; set; } = new();
    public List<Meeting> UpcomingMeetings { get; set; } = new();
    public List<ActionItem> OverdueActionItems { get; set; } = new();
}

public class PersonalDashboardData
{
    public List<Report> DraftReports { get; set; } = new();
    public List<Report> FeedbackRequested { get; set; } = new();
    public List<Report> ReportsAwaitingMyApproval { get; set; } = new();
    public List<Directive> PendingDirectives { get; set; } = new();
    public List<Meeting> PendingMeetings { get; set; } = new();
    public List<ActionItem> MyActionItems { get; set; } = new();
    public List<Committee> MyCommittees { get; set; } = new();
    public List<Notification> RecentNotifications { get; set; } = new();
}

public class CommitteeActivitiesData
{
    public List<CommitteeActivityGroup> MyCommitteeActivities { get; set; } = new();
    public List<CommitteeActivityGroup> SubCommitteeActivities { get; set; } = new();
}

public class CommitteeActivityGroup
{
    public Committee Committee { get; set; } = null!;
    public List<Report> PendingReports { get; set; } = new();
    public List<Directive> OpenDirectives { get; set; } = new();
    public List<Meeting> UpcomingMeetings { get; set; } = new();
    public List<ActionItem> PendingActionItems { get; set; } = new();
    public int TotalCount => PendingReports.Count + OpenDirectives.Count + UpcomingMeetings.Count + PendingActionItems.Count;
}
