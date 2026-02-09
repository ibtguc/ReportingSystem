using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Services;

/// <summary>
/// Service for managing in-app notifications
/// </summary>
public class NotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ApplicationDbContext context,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Create a new notification for a user
    /// </summary>
    public async Task<Notification> CreateNotificationAsync(
        string userId,
        NotificationType type,
        string title,
        string message,
        string? actionUrl = null,
        NotificationPriority priority = NotificationPriority.Normal,
        int? relatedEntityId = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            ActionUrl = actionUrl,
            Priority = priority,
            RelatedEntityId = relatedEntityId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created notification {NotificationId} for user {UserId}: {Title}",
            notification.Id, userId, title);

        return notification;
    }

    /// <summary>
    /// Get all notifications for a user
    /// </summary>
    public async Task<List<Notification>> GetUserNotificationsAsync(
        string userId,
        bool unreadOnly = false,
        int limit = 50)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return notifications;
    }

    /// <summary>
    /// Get unread notification count for a user
    /// </summary>
    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync();
    }

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    public async Task<bool> MarkAsReadAsync(int notificationId)
    {
        var notification = await _context.Notifications
            .FindAsync(notificationId);

        if (notification == null)
            return false;

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Mark all notifications as read for a user
    /// </summary>
    public async Task<int> MarkAllAsReadAsync(string userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        var count = notifications.Count;

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Marked {Count} notifications as read for user {UserId}", count, userId);

        return count;
    }

    // ── Event-driven notification helpers (FR-4.7.1.1) ──

    public async Task NotifyReportSubmittedAsync(int reportId, string reportTitle, int authorId, int committeeId)
    {
        // Notify heads of the report's own committee
        var recipientIds = await _context.CommitteeMemberships
            .Where(m => m.CommitteeId == committeeId && m.Role == CommitteeRole.Head && m.EffectiveTo == null)
            .Select(m => m.UserId)
            .ToListAsync();

        // Also notify ALL members of the parent committee
        var parentCommitteeId = await _context.Committees
            .Where(c => c.Id == committeeId)
            .Select(c => c.ParentCommitteeId)
            .FirstOrDefaultAsync();

        if (parentCommitteeId.HasValue)
        {
            var parentMembers = await _context.CommitteeMemberships
                .Where(m => m.CommitteeId == parentCommitteeId.Value && m.EffectiveTo == null)
                .Select(m => m.UserId)
                .ToListAsync();
            recipientIds.AddRange(parentMembers);
        }

        // Deduplicate and exclude the author
        recipientIds = recipientIds.Distinct().Where(id => id != authorId).ToList();

        foreach (var userId in recipientIds)
        {
            await CreateNotificationAsync(userId.ToString(), NotificationType.ReportSubmitted,
                "Report Submitted", $"Report \"{reportTitle}\" has been submitted for review.",
                $"/Reports/Details/{reportId}", NotificationPriority.Normal, reportId);
        }
    }

    public async Task NotifyReportStatusChangedAsync(int reportId, string reportTitle, int authorId, string newStatus)
    {
        await CreateNotificationAsync(authorId.ToString(), NotificationType.ReportStatusChanged,
            $"Report {newStatus}", $"Your report \"{reportTitle}\" status changed to {newStatus}.",
            $"/Reports/Details/{reportId}", NotificationPriority.Normal, reportId);
    }

    /// <summary>
    /// Returns the list of users who would be notified when a report in the given committee is submitted.
    /// Used to display "will be notified" info before submission.
    /// </summary>
    public async Task<List<User>> GetReportSubmissionRecipientsAsync(int committeeId, int authorId)
    {
        // Same-committee heads
        var ids = await _context.CommitteeMemberships
            .Where(m => m.CommitteeId == committeeId && m.Role == CommitteeRole.Head && m.EffectiveTo == null)
            .Select(m => m.UserId)
            .ToListAsync();

        // All parent committee members
        var parentCommitteeId = await _context.Committees
            .Where(c => c.Id == committeeId)
            .Select(c => c.ParentCommitteeId)
            .FirstOrDefaultAsync();

        if (parentCommitteeId.HasValue)
        {
            var parentMembers = await _context.CommitteeMemberships
                .Where(m => m.CommitteeId == parentCommitteeId.Value && m.EffectiveTo == null)
                .Select(m => m.UserId)
                .ToListAsync();
            ids.AddRange(parentMembers);
        }

        var recipientIds = ids.Distinct().Where(id => id != authorId).ToList();
        return await _context.Users.Where(u => recipientIds.Contains(u.Id)).OrderBy(u => u.Name).ToListAsync();
    }

    /// <summary>
    /// Notify the issuer when a directive's status changes (target acknowledges, starts, implements, etc.)
    /// </summary>
    public async Task NotifyDirectiveStatusChangedAsync(int directiveId, string directiveTitle, int issuerId, string newStatus)
    {
        await CreateNotificationAsync(issuerId.ToString(), NotificationType.DirectiveIssued,
            $"Directive {newStatus}", $"Directive \"{directiveTitle}\" has been {newStatus.ToLower()}.",
            $"/Directives/Details/{directiveId}", NotificationPriority.Normal, directiveId);
    }

    public async Task NotifyDirectiveIssuedAsync(int directiveId, string directiveTitle, int targetCommitteeId, int? targetUserId)
    {
        if (targetUserId.HasValue)
        {
            await CreateNotificationAsync(targetUserId.Value.ToString(), NotificationType.DirectiveIssued,
                "New Directive", $"You have received directive: \"{directiveTitle}\".",
                $"/Directives/Details/{directiveId}", NotificationPriority.High, directiveId);
        }
        else
        {
            var members = await _context.CommitteeMemberships
                .Where(m => m.CommitteeId == targetCommitteeId && m.EffectiveTo == null)
                .Select(m => m.UserId)
                .ToListAsync();

            foreach (var memberId in members)
            {
                await CreateNotificationAsync(memberId.ToString(), NotificationType.DirectiveIssued,
                    "New Directive", $"Your committee has received directive: \"{directiveTitle}\".",
                    $"/Directives/Details/{directiveId}", NotificationPriority.High, directiveId);
            }
        }
    }

    public async Task NotifyMeetingInvitationAsync(int meetingId, string meetingTitle, int userId)
    {
        await CreateNotificationAsync(userId.ToString(), NotificationType.MeetingInvitation,
            "Meeting Invitation", $"You have been invited to meeting: \"{meetingTitle}\".",
            $"/Meetings/Details/{meetingId}", NotificationPriority.Normal, meetingId);
    }

    public async Task NotifyMinutesSubmittedAsync(int meetingId, string meetingTitle, List<int> attendeeIds)
    {
        foreach (var attendeeId in attendeeIds)
        {
            await CreateNotificationAsync(attendeeId.ToString(), NotificationType.MinutesSubmitted,
                "Minutes for Confirmation", $"Minutes for \"{meetingTitle}\" are ready for your confirmation.",
                $"/Meetings/Details/{meetingId}", NotificationPriority.Normal, meetingId);
        }
    }

    public async Task NotifyActionItemAssignedAsync(int actionItemId, string actionItemTitle, int assigneeId, int meetingId)
    {
        await CreateNotificationAsync(assigneeId.ToString(), NotificationType.ActionItemAssigned,
            "Action Item Assigned", $"You have been assigned action item: \"{actionItemTitle}\".",
            "/Meetings/ActionItems", NotificationPriority.Normal, actionItemId);
    }

    public async Task NotifyActionItemOverdueAsync(int actionItemId, string actionItemTitle, int assigneeId)
    {
        await CreateNotificationAsync(assigneeId.ToString(), NotificationType.ActionItemOverdue,
            "Action Item Overdue", $"Action item \"{actionItemTitle}\" is overdue.",
            "/Meetings/ActionItems", NotificationPriority.High, actionItemId);
    }

    /// <summary>
    /// Delete old read notifications (cleanup)
    /// </summary>
    public async Task<int> DeleteOldNotificationsAsync(int daysToKeep = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);

        var oldNotifications = await _context.Notifications
            .Where(n => n.IsRead && n.ReadAt < cutoffDate)
            .ToListAsync();

        var count = oldNotifications.Count;

        _context.Notifications.RemoveRange(oldNotifications);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted {Count} old notifications", count);

        return count;
    }
}
