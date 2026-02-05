using ReportingSystem.Models;

namespace ReportingSystem.Data;

/// <summary>
/// Seeds delegations and notifications.
/// </summary>
public static class SeedDelegationsAndNotifications
{
    public static async Task SeedDelegationsAsync(ApplicationDbContext context)
    {
        var now = DateTime.UtcNow;
        var today = DateTime.UtcNow.Date;

        var delegations = new List<Delegation>
        {
            // Active delegation: Head of CSE delegates to a reviewer during conference travel
            new()
            {
                DelegatorId = 9, // Prof. Nadia Kamel (Head of CSE)
                DelegateId = 31, // Dr. Hany Mourad (Reviewer)
                StartDate = today.AddDays(-2),
                EndDate = today.AddDays(12),
                Reason = "International conference attendance - IEEE 2026",
                Scope = DelegationScope.Full,
                IsActive = true,
                CreatedAt = now.AddDays(-3)
            },
            // Active delegation: Head of Software Dev delegates approval to Web Section Lead
            new()
            {
                DelegatorId = 18, // Eng. Mahmoud Adel (Head of SDEV)
                DelegateId = 22, // Eng. Ali Kamal (Web Section Lead)
                StartDate = today.AddDays(-1),
                EndDate = today.AddDays(6),
                Reason = "Annual leave",
                Scope = DelegationScope.ApprovalOnly,
                IsActive = true,
                CreatedAt = now.AddDays(-2)
            },
            // Upcoming delegation: Head of Infrastructure will delegate next month
            new()
            {
                DelegatorId = 19, // Eng. Heba Mostafa (Head of Infrastructure)
                DelegateId = 24, // Eng. Hassan Tawfik (Network Ops Lead)
                StartDate = today.AddDays(14),
                EndDate = today.AddDays(28),
                Reason = "Training program abroad",
                Scope = DelegationScope.Full,
                IsActive = true,
                CreatedAt = now
            },
            // Past delegation: VP Admin delegated to Dean Main Campus (already completed)
            new()
            {
                DelegatorId = 3, // Dr. Khaled Ibrahim (VP Admin)
                DelegateId = 4, // Prof. Sara Mahmoud (Dean Main Campus)
                StartDate = today.AddDays(-60),
                EndDate = today.AddDays(-45),
                Reason = "Medical leave recovery",
                Scope = DelegationScope.Full,
                IsActive = true,
                CreatedAt = now.AddDays(-65)
            },
            // Revoked delegation: Head of HR revoked early
            new()
            {
                DelegatorId = 20, // Dr. Amira Youssef (Head of HR)
                DelegateId = 55, // Marwa Elsayed (HR Specialist)
                StartDate = today.AddDays(-10),
                EndDate = today.AddDays(5),
                Reason = "Business travel - cancelled",
                Scope = DelegationScope.ReportingOnly,
                IsActive = false,
                CreatedAt = now.AddDays(-12),
                RevokedAt = now.AddDays(-8)
            },
            // Active reporting-only delegation in MET faculty
            new()
            {
                DelegatorId = 13, // Prof. Fatma Zaki (Head of CS)
                DelegateId = 33, // Dr. Tamer Hosny (Reviewer)
                StartDate = today,
                EndDate = today.AddDays(20),
                Reason = "Sabbatical research period",
                Scope = DelegationScope.ReportingOnly,
                IsActive = true,
                CreatedAt = now.AddDays(-1)
            }
        };

        context.Delegations.AddRange(delegations);
        await context.SaveChangesAsync();
    }

    public static async Task SeedNotificationsAsync(ApplicationDbContext context)
    {
        var now = DateTime.UtcNow;

        var notifications = new List<Notification>
        {
            new()
            {
                UserId = "6", // System Administrator
                Type = NotificationType.General,
                Title = "Welcome to HORS",
                Message = "Welcome to the Hierarchical Organizational Reporting System. Your administrator account has been configured.",
                ActionUrl = "/Admin/Dashboard",
                IsRead = false,
                CreatedAt = now,
                Priority = NotificationPriority.Normal
            },
            new()
            {
                UserId = "1", // University President
                Type = NotificationType.General,
                Title = "System Ready",
                Message = "The reporting system is now configured with organizational units and user accounts. You may begin reviewing reports.",
                ActionUrl = "/Admin/Dashboard",
                IsRead = false,
                CreatedAt = now,
                Priority = NotificationPriority.Normal
            },
            new()
            {
                UserId = "9", // Head of CSE
                Type = NotificationType.DeadlineApproaching,
                Title = "Delegation Active",
                Message = "Your authority has been delegated to Dr. Hany Mourad while you attend the IEEE conference.",
                ActionUrl = "/Admin/Delegations",
                IsRead = true,
                CreatedAt = now.AddDays(-2),
                ReadAt = now.AddDays(-2),
                Priority = NotificationPriority.High,
                RelatedEntityId = 1
            },
            new()
            {
                UserId = "31", // Dr. Hany Mourad
                Type = NotificationType.DeadlineApproaching,
                Title = "Delegation Received",
                Message = "You have received delegated authority from Prof. Nadia Kamel (Head of CSE). This delegation is active until the end of the conference period.",
                ActionUrl = "/Admin/Delegations",
                IsRead = true,
                CreatedAt = now.AddDays(-2),
                ReadAt = now.AddDays(-1),
                Priority = NotificationPriority.High,
                RelatedEntityId = 1
            },
            new()
            {
                UserId = "18", // Head of Software Dev
                Type = NotificationType.DeadlineApproaching,
                Title = "Delegation Active",
                Message = "Your approval authority has been delegated to Eng. Ali Kamal during your annual leave.",
                ActionUrl = "/Admin/Delegations",
                IsRead = false,
                CreatedAt = now.AddDays(-1),
                Priority = NotificationPriority.Normal,
                RelatedEntityId = 2
            }
        };

        context.Notifications.AddRange(notifications);
        await context.SaveChangesAsync();
    }
}
