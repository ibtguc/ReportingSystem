using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.Workflow.Confirmations;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<ConfirmationTag> Confirmations { get; set; } = new();
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ReportId { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool OnlyOverdue { get; set; }

    public async Task OnGetAsync()
    {
        var query = _context.ConfirmationTags
            .Include(c => c.Report)
                .ThenInclude(r => r.SubmittedBy)
            .Include(c => c.Report.ReportTemplate)
            .Include(c => c.RequestedBy)
            .Include(c => c.TaggedUser)
            .AsQueryable();

        if (!string.IsNullOrEmpty(StatusFilter))
        {
            query = query.Where(c => c.Status == StatusFilter);
        }

        if (ReportId.HasValue)
        {
            query = query.Where(c => c.ReportId == ReportId.Value);
        }

        if (OnlyOverdue)
        {
            var overdueThreshold = DateTime.UtcNow.AddDays(-3);
            query = query.Where(c =>
                c.Status == ConfirmationStatus.Pending &&
                c.CreatedAt < overdueThreshold);
        }

        if (!string.IsNullOrEmpty(SearchTerm))
        {
            var term = SearchTerm.ToLower();
            query = query.Where(c =>
                (c.Message != null && c.Message.ToLower().Contains(term)) ||
                c.RequestedBy.Name.ToLower().Contains(term) ||
                c.TaggedUser.Name.ToLower().Contains(term));
        }

        TotalCount = await query.CountAsync();
        PendingCount = await _context.ConfirmationTags
            .CountAsync(c => c.Status == ConfirmationStatus.Pending);

        Confirmations = await query
            .OrderByDescending(c => c.CreatedAt)
            .Take(100)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(int id, string newStatus)
    {
        var confirmation = await _context.ConfirmationTags.FindAsync(id);
        if (confirmation == null) return NotFound();

        confirmation.Status = newStatus;

        if (newStatus != ConfirmationStatus.Pending)
        {
            confirmation.RespondedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Confirmation status updated to {ConfirmationStatus.DisplayName(newStatus)}.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSendReminderAsync(int id)
    {
        var confirmation = await _context.ConfirmationTags
            .Include(c => c.TaggedUser)
            .Include(c => c.Report)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (confirmation == null) return NotFound();

        if (confirmation.Status != ConfirmationStatus.Pending)
        {
            TempData["ErrorMessage"] = "Can only send reminders for pending confirmations.";
            return RedirectToPage();
        }

        // Create a notification for the tagged user
        var notification = new Notification
        {
            UserId = confirmation.TaggedUserId.ToString(),
            Type = NotificationType.ConfirmationRequested,
            Title = "Confirmation Reminder",
            Message = $"You have a pending confirmation request for report #{confirmation.ReportId}.",
            ActionUrl = $"/Admin/Reports/View?id={confirmation.ReportId}",
            RelatedEntityId = confirmation.ReportId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);

        confirmation.ReminderSentAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Reminder sent to {confirmation.TaggedUser.Name}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var confirmation = await _context.ConfirmationTags.FindAsync(id);
        if (confirmation == null) return NotFound();

        confirmation.Status = ConfirmationStatus.Cancelled;
        confirmation.RespondedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Confirmation request cancelled.";

        return RedirectToPage();
    }
}
