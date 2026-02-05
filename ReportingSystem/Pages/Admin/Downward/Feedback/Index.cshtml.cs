using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.Downward.Feedback;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Models.Feedback> Feedbacks { get; set; } = new();
    public int TotalCount { get; set; }
    public int PendingAcknowledgmentCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? CategoryFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? VisibilityFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ReportId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync()
    {
        var query = _context.Feedbacks
            .Include(f => f.Report)
                .ThenInclude(r => r.SubmittedBy)
            .Include(f => f.Report.ReportTemplate)
            .Include(f => f.Author)
            .Where(f => f.ParentFeedbackId == null) // Only top-level feedback
            .AsQueryable();

        if (!string.IsNullOrEmpty(StatusFilter))
        {
            query = query.Where(f => f.Status == StatusFilter);
        }

        if (!string.IsNullOrEmpty(CategoryFilter))
        {
            query = query.Where(f => f.Category == CategoryFilter);
        }

        if (!string.IsNullOrEmpty(VisibilityFilter))
        {
            query = query.Where(f => f.Visibility == VisibilityFilter);
        }

        if (ReportId.HasValue)
        {
            query = query.Where(f => f.ReportId == ReportId.Value);
        }

        if (!string.IsNullOrEmpty(SearchTerm))
        {
            var term = SearchTerm.ToLower();
            query = query.Where(f =>
                f.Subject.ToLower().Contains(term) ||
                f.Content.ToLower().Contains(term) ||
                f.Author.Name.ToLower().Contains(term));
        }

        TotalCount = await query.CountAsync();
        PendingAcknowledgmentCount = await _context.Feedbacks
            .Where(f => !f.IsAcknowledged &&
                       (f.Category == FeedbackCategory.Concern || f.Category == FeedbackCategory.Question))
            .CountAsync();

        Feedbacks = await query
            .OrderByDescending(f => f.CreatedAt)
            .Take(100)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(int id, string newStatus)
    {
        var feedback = await _context.Feedbacks.FindAsync(id);
        if (feedback == null) return NotFound();

        feedback.Status = newStatus;
        feedback.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Status updated to {FeedbackStatus.DisplayName(newStatus)}.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var feedback = await _context.Feedbacks.FindAsync(id);
        if (feedback == null) return NotFound();

        _context.Feedbacks.Remove(feedback);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Feedback deleted.";

        return RedirectToPage();
    }
}
