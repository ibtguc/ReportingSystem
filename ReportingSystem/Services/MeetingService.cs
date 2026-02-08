using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Services;

public class MeetingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MeetingService> _logger;

    public MeetingService(ApplicationDbContext context, ILogger<MeetingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── Meeting Queries ──

    public async Task<List<Meeting>> GetMeetingsAsync(
        int? committeeId = null,
        MeetingStatus? status = null,
        bool includePast = false,
        bool includeCancelled = false)
    {
        var query = _context.Meetings
            .Include(m => m.Committee)
            .Include(m => m.Moderator)
            .Include(m => m.Attendees)
            .AsQueryable();

        if (!includeCancelled)
            query = query.Where(m => m.Status != MeetingStatus.Cancelled);

        if (!includePast)
            query = query.Where(m => m.ScheduledAt >= DateTime.UtcNow.Date || m.Status == MeetingStatus.InProgress
                || m.Status == MeetingStatus.MinutesEntry || m.Status == MeetingStatus.MinutesReview);

        if (committeeId.HasValue)
            query = query.Where(m => m.CommitteeId == committeeId.Value);

        if (status.HasValue)
            query = query.Where(m => m.Status == status.Value);

        return await query
            .OrderBy(m => m.ScheduledAt)
            .ToListAsync();
    }

    public async Task<Meeting?> GetMeetingByIdAsync(int id)
    {
        return await _context.Meetings
            .Include(m => m.Committee)
            .Include(m => m.Moderator)
            .Include(m => m.AgendaItems.OrderBy(a => a.OrderIndex))
                .ThenInclude(a => a.Presenter)
            .Include(m => m.AgendaItems)
                .ThenInclude(a => a.LinkedReport)
            .Include(m => m.Attendees)
                .ThenInclude(a => a.User)
            .Include(m => m.Decisions.OrderBy(d => d.CreatedAt))
                .ThenInclude(d => d.AgendaItem)
            .Include(m => m.Decisions)
                .ThenInclude(d => d.ActionItems)
            .Include(m => m.ActionItems)
                .ThenInclude(ai => ai.AssignedTo)
            .Include(m => m.ActionItems)
                .ThenInclude(ai => ai.AssignedBy)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<List<Meeting>> GetMeetingsForUserAsync(int userId, bool includePast = false)
    {
        var query = _context.Meetings
            .Include(m => m.Committee)
            .Include(m => m.Moderator)
            .Include(m => m.Attendees)
            .Where(m => m.ModeratorId == userId
                || m.Attendees.Any(a => a.UserId == userId));

        if (!includePast)
            query = query.Where(m => m.ScheduledAt >= DateTime.UtcNow.Date || m.Status == MeetingStatus.InProgress
                || m.Status == MeetingStatus.MinutesEntry || m.Status == MeetingStatus.MinutesReview);

        return await query
            .OrderBy(m => m.ScheduledAt)
            .ToListAsync();
    }

    // ── Meeting CRUD ──

    public async Task<Meeting> CreateMeetingAsync(Meeting meeting, int userId)
    {
        meeting.ModeratorId = userId;
        meeting.Status = MeetingStatus.Scheduled;
        meeting.CreatedAt = DateTime.UtcNow;

        _context.Meetings.Add(meeting);
        await _context.SaveChangesAsync();

        // Auto-add moderator as attendee
        var moderatorAttendee = new MeetingAttendee
        {
            MeetingId = meeting.Id,
            UserId = userId,
            RsvpStatus = RsvpStatus.Accepted,
            RsvpAt = DateTime.UtcNow
        };
        _context.MeetingAttendees.Add(moderatorAttendee);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Meeting created: {Title} (ID: {Id})", meeting.Title, meeting.Id);
        return meeting;
    }

    public async Task UpdateMeetingAsync(Meeting meeting)
    {
        meeting.UpdatedAt = DateTime.UtcNow;
        _context.Meetings.Update(meeting);
        await _context.SaveChangesAsync();
    }

    public async Task CancelMeetingAsync(int meetingId)
    {
        var meeting = await _context.Meetings.FindAsync(meetingId);
        if (meeting == null) return;

        meeting.Status = MeetingStatus.Cancelled;
        meeting.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Meeting cancelled: {Id}", meetingId);
    }

    // ── Meeting Status Transitions ──

    public async Task StartMeetingAsync(int meetingId)
    {
        var meeting = await _context.Meetings.FindAsync(meetingId);
        if (meeting == null || meeting.Status != MeetingStatus.Scheduled) return;

        meeting.Status = MeetingStatus.InProgress;
        meeting.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task BeginMinutesEntryAsync(int meetingId)
    {
        var meeting = await _context.Meetings.FindAsync(meetingId);
        if (meeting == null || (meeting.Status != MeetingStatus.InProgress && meeting.Status != MeetingStatus.Scheduled)) return;

        meeting.Status = MeetingStatus.MinutesEntry;
        meeting.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task SubmitMinutesAsync(int meetingId, string minutesContent)
    {
        var meeting = await _context.Meetings
            .Include(m => m.Attendees)
            .FirstOrDefaultAsync(m => m.Id == meetingId);
        if (meeting == null || (meeting.Status != MeetingStatus.MinutesEntry && meeting.Status != MeetingStatus.MinutesReview)) return;

        meeting.MinutesContent = minutesContent;
        meeting.MinutesSubmittedAt = DateTime.UtcNow;
        meeting.Status = MeetingStatus.MinutesReview;
        meeting.UpdatedAt = DateTime.UtcNow;

        // Reset all attendee confirmations to Pending when (re)submitting
        foreach (var attendee in meeting.Attendees)
        {
            attendee.ConfirmationStatus = ConfirmationStatus.Pending;
            attendee.ConfirmationComment = null;
            attendee.ConfirmedAt = null;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Minutes submitted for meeting {Id}", meetingId);
    }

    public async Task<bool> TryFinalizeMinutesAsync(int meetingId)
    {
        var meeting = await _context.Meetings
            .Include(m => m.Attendees)
            .FirstOrDefaultAsync(m => m.Id == meetingId);
        if (meeting == null || meeting.Status != MeetingStatus.MinutesReview) return false;

        // Check: all attendees must have Confirmed or Abstained
        var allConfirmed = meeting.Attendees.All(a =>
            a.ConfirmationStatus == ConfirmationStatus.Confirmed
            || a.ConfirmationStatus == ConfirmationStatus.Abstained);

        if (!allConfirmed) return false;

        meeting.Status = MeetingStatus.Finalized;
        meeting.MinutesFinalizedAt = DateTime.UtcNow;
        meeting.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Minutes finalized for meeting {Id}", meetingId);
        return true;
    }

    // ── Attendee Management ──

    public async Task AddAttendeeAsync(int meetingId, int userId)
    {
        var exists = await _context.MeetingAttendees
            .AnyAsync(a => a.MeetingId == meetingId && a.UserId == userId);
        if (exists) return;

        var attendee = new MeetingAttendee
        {
            MeetingId = meetingId,
            UserId = userId,
            RsvpStatus = RsvpStatus.Pending
        };
        _context.MeetingAttendees.Add(attendee);
        await _context.SaveChangesAsync();
    }

    public async Task AddAttendeesFromCommitteeAsync(int meetingId, int committeeId)
    {
        var members = await _context.CommitteeMemberships
            .Where(m => m.CommitteeId == committeeId && m.EffectiveTo == null)
            .Select(m => m.UserId)
            .ToListAsync();

        foreach (var userId in members)
        {
            await AddAttendeeAsync(meetingId, userId);
        }
    }

    public async Task RemoveAttendeeAsync(int meetingId, int userId)
    {
        var attendee = await _context.MeetingAttendees
            .FirstOrDefaultAsync(a => a.MeetingId == meetingId && a.UserId == userId);
        if (attendee == null) return;

        _context.MeetingAttendees.Remove(attendee);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateRsvpAsync(int meetingId, int userId, RsvpStatus status, string? comment = null)
    {
        var attendee = await _context.MeetingAttendees
            .FirstOrDefaultAsync(a => a.MeetingId == meetingId && a.UserId == userId);
        if (attendee == null) return;

        attendee.RsvpStatus = status;
        attendee.RsvpComment = comment;
        attendee.RsvpAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task UpdateConfirmationAsync(int meetingId, int userId, ConfirmationStatus status, string? comment = null)
    {
        var attendee = await _context.MeetingAttendees
            .FirstOrDefaultAsync(a => a.MeetingId == meetingId && a.UserId == userId);
        if (attendee == null) return;

        attendee.ConfirmationStatus = status;
        attendee.ConfirmationComment = comment;
        attendee.ConfirmedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    // ── Agenda Items ──

    public async Task<MeetingAgendaItem> AddAgendaItemAsync(MeetingAgendaItem item)
    {
        // Set order index to next available
        var maxOrder = await _context.MeetingAgendaItems
            .Where(a => a.MeetingId == item.MeetingId)
            .MaxAsync(a => (int?)a.OrderIndex) ?? -1;
        item.OrderIndex = maxOrder + 1;

        _context.MeetingAgendaItems.Add(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task UpdateAgendaItemAsync(MeetingAgendaItem item)
    {
        _context.MeetingAgendaItems.Update(item);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveAgendaItemAsync(int agendaItemId)
    {
        var item = await _context.MeetingAgendaItems.FindAsync(agendaItemId);
        if (item == null) return;

        _context.MeetingAgendaItems.Remove(item);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAgendaDiscussionNotesAsync(int agendaItemId, string notes)
    {
        var item = await _context.MeetingAgendaItems.FindAsync(agendaItemId);
        if (item == null) return;

        item.DiscussionNotes = notes;
        await _context.SaveChangesAsync();
    }

    // ── Decisions ──

    public async Task<MeetingDecision> AddDecisionAsync(MeetingDecision decision)
    {
        decision.CreatedAt = DateTime.UtcNow;
        _context.MeetingDecisions.Add(decision);
        await _context.SaveChangesAsync();
        return decision;
    }

    public async Task RemoveDecisionAsync(int decisionId)
    {
        var decision = await _context.MeetingDecisions.FindAsync(decisionId);
        if (decision == null) return;

        _context.MeetingDecisions.Remove(decision);
        await _context.SaveChangesAsync();
    }

    // ── Action Items ──

    public async Task<ActionItem> CreateActionItemAsync(ActionItem actionItem)
    {
        actionItem.Status = ActionItemStatus.Assigned;
        actionItem.CreatedAt = DateTime.UtcNow;
        _context.ActionItems.Add(actionItem);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Action item created: {Title} (ID: {Id})", actionItem.Title, actionItem.Id);
        return actionItem;
    }

    public async Task StartActionItemAsync(int actionItemId)
    {
        var item = await _context.ActionItems.FindAsync(actionItemId);
        if (item == null || item.Status != ActionItemStatus.Assigned) return;

        item.Status = ActionItemStatus.InProgress;
        item.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task CompleteActionItemAsync(int actionItemId)
    {
        var item = await _context.ActionItems.FindAsync(actionItemId);
        if (item == null || (item.Status != ActionItemStatus.Assigned && item.Status != ActionItemStatus.InProgress)) return;

        item.Status = ActionItemStatus.Completed;
        item.CompletedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task VerifyActionItemAsync(int actionItemId)
    {
        var item = await _context.ActionItems.FindAsync(actionItemId);
        if (item == null || item.Status != ActionItemStatus.Completed) return;

        item.Status = ActionItemStatus.Verified;
        item.VerifiedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<List<ActionItem>> GetActionItemsForUserAsync(int userId)
    {
        return await _context.ActionItems
            .Include(ai => ai.Meeting)
                .ThenInclude(m => m.Committee)
            .Include(ai => ai.AssignedBy)
            .Include(ai => ai.MeetingDecision)
            .Where(ai => ai.AssignedToId == userId && ai.Status != ActionItemStatus.Verified)
            .OrderBy(ai => ai.Deadline ?? DateTime.MaxValue)
            .ThenByDescending(ai => ai.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ActionItem>> GetAllActionItemsAsync(
        int? committeeId = null,
        int? assignedToId = null,
        ActionItemStatus? status = null)
    {
        var query = _context.ActionItems
            .Include(ai => ai.Meeting)
                .ThenInclude(m => m.Committee)
            .Include(ai => ai.AssignedTo)
            .Include(ai => ai.AssignedBy)
            .Include(ai => ai.MeetingDecision)
            .AsQueryable();

        if (committeeId.HasValue)
            query = query.Where(ai => ai.Meeting.CommitteeId == committeeId.Value);

        if (assignedToId.HasValue)
            query = query.Where(ai => ai.AssignedToId == assignedToId.Value);

        if (status.HasValue)
            query = query.Where(ai => ai.Status == status.Value);

        return await query
            .OrderBy(ai => ai.Deadline ?? DateTime.MaxValue)
            .ThenByDescending(ai => ai.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ActionItem>> GetOverdueActionItemsAsync()
    {
        return await _context.ActionItems
            .Include(ai => ai.Meeting)
                .ThenInclude(m => m.Committee)
            .Include(ai => ai.AssignedTo)
            .Where(ai => ai.Deadline.HasValue
                && ai.Deadline.Value < DateTime.UtcNow
                && ai.Status != ActionItemStatus.Completed
                && ai.Status != ActionItemStatus.Verified)
            .OrderBy(ai => ai.Deadline)
            .ToListAsync();
    }

    // ── Authorization Helpers ──

    public async Task<bool> CanUserScheduleMeetingAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        if (user.SystemRole == SystemRole.SystemAdmin
            || user.SystemRole == SystemRole.Chairman
            || user.SystemRole == SystemRole.ChairmanOffice)
            return true;

        // Committee heads can schedule meetings
        return await _context.CommitteeMemberships
            .AnyAsync(m => m.UserId == userId
                && m.Role == CommitteeRole.Head
                && m.EffectiveTo == null);
    }

    public async Task<bool> IsUserModeratorAsync(int meetingId, int userId)
    {
        var meeting = await _context.Meetings.FindAsync(meetingId);
        return meeting?.ModeratorId == userId;
    }

    public async Task<bool> IsUserAttendeeAsync(int meetingId, int userId)
    {
        return await _context.MeetingAttendees
            .AnyAsync(a => a.MeetingId == meetingId && a.UserId == userId);
    }

    public async Task<List<Committee>> GetSchedulableCommitteesAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return new List<Committee>();

        if (user.SystemRole == SystemRole.SystemAdmin
            || user.SystemRole == SystemRole.Chairman
            || user.SystemRole == SystemRole.ChairmanOffice)
        {
            return await _context.Committees
                .Where(c => c.IsActive)
                .OrderBy(c => c.HierarchyLevel)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        // Return committees where user is Head
        var committeeIds = await _context.CommitteeMemberships
            .Where(m => m.UserId == userId && m.Role == CommitteeRole.Head && m.EffectiveTo == null)
            .Select(m => m.CommitteeId)
            .ToListAsync();

        return await _context.Committees
            .Where(c => committeeIds.Contains(c.Id) && c.IsActive)
            .OrderBy(c => c.HierarchyLevel)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    // ── Stats ──

    public async Task<(int total, int scheduled, int inProgress, int minutesReview, int finalized, int overdueActions)> GetMeetingStatsAsync()
    {
        var meetings = _context.Meetings.Where(m => m.Status != MeetingStatus.Cancelled);

        var total = await meetings.CountAsync();
        var scheduled = await meetings.CountAsync(m => m.Status == MeetingStatus.Scheduled);
        var inProgress = await meetings.CountAsync(m => m.Status == MeetingStatus.InProgress
            || m.Status == MeetingStatus.MinutesEntry);
        var minutesReview = await meetings.CountAsync(m => m.Status == MeetingStatus.MinutesReview);
        var finalized = await meetings.CountAsync(m => m.Status == MeetingStatus.Finalized);

        var overdueActions = await _context.ActionItems
            .CountAsync(ai => ai.Deadline.HasValue
                && ai.Deadline.Value < DateTime.UtcNow
                && ai.Status != ActionItemStatus.Completed
                && ai.Status != ActionItemStatus.Verified);

        return (total, scheduled, inProgress, minutesReview, finalized, overdueActions);
    }

    public async Task<List<Meeting>> GetUpcomingMeetingsAsync(int days = 7)
    {
        var cutoff = DateTime.UtcNow.AddDays(days);
        return await _context.Meetings
            .Include(m => m.Committee)
            .Include(m => m.Moderator)
            .Include(m => m.Attendees)
            .Where(m => m.Status == MeetingStatus.Scheduled
                && m.ScheduledAt >= DateTime.UtcNow
                && m.ScheduledAt <= cutoff)
            .OrderBy(m => m.ScheduledAt)
            .ToListAsync();
    }
}
