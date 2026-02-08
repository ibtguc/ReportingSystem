using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Services;

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

    // Helper methods for specific notification types

    /// <summary>
    /// Notify user about absence being reported
    /// </summary>
    public async Task NotifyAbsenceReportedAsync(string userId, int absenceId, string teacherName, DateTime date)
    {
        await CreateNotificationAsync(
            userId,
            NotificationType.AbsenceReported,
            "Absence Reported",
            $"Absence for {teacherName} on {date:MMM dd, yyyy} has been recorded.",
            $"/Admin/Absences/{absenceId}",
            NotificationPriority.Normal,
            absenceId);
    }

    /// <summary>
    /// Notify substitute teacher of assignment
    /// </summary>
    public async Task NotifySubstituteAssignedAsync(
        string userId,
        int substitutionId,
        string className,
        string subjectName,
        DateTime date,
        string periodTime)
    {
        await CreateNotificationAsync(
            userId,
            NotificationType.SubstituteAssigned,
            "Substitute Assignment",
            $"You are assigned to teach {subjectName} for {className} on {date:MMM dd} at {periodTime}.",
            $"/Admin/Substitutions/Daily?date={date:yyyy-MM-dd}",
            NotificationPriority.High,
            substitutionId);
    }

    /// <summary>
    /// Notify about approaching substitution (1 hour before)
    /// </summary>
    public async Task NotifySubstitutionApproachingAsync(
        string userId,
        string className,
        string subjectName,
        string roomName,
        int minutesUntil)
    {
        await CreateNotificationAsync(
            userId,
            NotificationType.SubstitutionApproaching,
            "Substitution Starting Soon",
            $"Reminder: {subjectName} for {className} in {roomName} starts in {minutesUntil} minutes.",
            null,
            NotificationPriority.Urgent);
    }

    /// <summary>
    /// Notify admin about coverage status
    /// </summary>
    public async Task NotifyCoverageStatusAsync(
        string userId,
        int absenceId,
        string teacherName,
        int coveredCount,
        int totalCount)
    {
        var type = coveredCount == totalCount
            ? NotificationType.CoverageComplete
            : NotificationType.CoverageIncomplete;

        var title = coveredCount == totalCount
            ? "All Lessons Covered"
            : "Coverage Incomplete";

        var message = coveredCount == totalCount
            ? $"All {totalCount} lessons for {teacherName} have been covered."
            : $"{coveredCount} of {totalCount} lessons for {teacherName} are covered. {totalCount - coveredCount} still need substitutes.";

        var priority = coveredCount == totalCount
            ? NotificationPriority.Normal
            : NotificationPriority.High;

        await CreateNotificationAsync(
            userId,
            type,
            title,
            message,
            $"/Admin/Absences/{absenceId}/FindSubstitutes",
            priority,
            absenceId);
    }
}
