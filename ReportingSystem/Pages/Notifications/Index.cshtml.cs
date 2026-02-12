using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Notifications;

[Authorize]
public class IndexModel : PageModel
{
    private readonly NotificationService _notificationService;

    public IndexModel(NotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public List<Notification> Notifications { get; set; } = new();
    public int UnreadCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool UnreadOnly { get; set; }

    public async Task OnGetAsync()
    {
        var userId = GetUserId();
        Notifications = await _notificationService.GetUserNotificationsAsync(userId, UnreadOnly, 100);
        UnreadCount = await _notificationService.GetUnreadCountAsync(userId);
    }

    public async Task<IActionResult> OnPostMarkReadAsync(int notificationId)
    {
        await _notificationService.MarkAsReadAsync(notificationId);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkAllReadAsync()
    {
        var userId = GetUserId();
        await _notificationService.MarkAllAsReadAsync(userId);
        TempData["SuccessMessage"] = "All notifications marked as read.";
        return RedirectToPage();
    }

    private string GetUserId() =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0";
}
