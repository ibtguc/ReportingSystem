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

        // Pending reports awaiting review in my committees
        data.PendingReports = await _context.Reports
            .Include(r => r.Author).Include(r => r.Committee)
            .Where(r => headCommitteeIds.Contains(r.CommitteeId)
                && r.Status == ReportStatus.Submitted)
            .OrderByDescending(r => r.CreatedAt)
            .Take(15)
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

        // Directives I need to act on
        var myCommitteeIds = await _context.CommitteeMemberships
            .Where(m => m.UserId == userId && m.EffectiveTo == null)
            .Select(m => m.CommitteeId)
            .ToListAsync();

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
    public List<Directive> OpenDirectives { get; set; } = new();
    public List<Meeting> UpcomingMeetings { get; set; } = new();
    public List<ActionItem> OverdueActionItems { get; set; } = new();
}

public class PersonalDashboardData
{
    public List<Report> DraftReports { get; set; } = new();
    public List<Report> FeedbackRequested { get; set; } = new();
    public List<Directive> PendingDirectives { get; set; } = new();
    public List<Meeting> PendingMeetings { get; set; } = new();
    public List<ActionItem> MyActionItems { get; set; } = new();
    public List<Committee> MyCommittees { get; set; } = new();
    public List<Notification> RecentNotifications { get; set; } = new();
}
