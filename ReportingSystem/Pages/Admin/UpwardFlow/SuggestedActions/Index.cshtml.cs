using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.UpwardFlow.SuggestedActions;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<SuggestedAction> SuggestedActions { get; set; } = new();
    public int TotalCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? CategoryFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? PriorityFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync()
    {
        var query = _context.SuggestedActions
            .Include(a => a.Report)
                .ThenInclude(r => r.SubmittedBy)
            .Include(a => a.Report.ReportTemplate)
            .Include(a => a.ReviewedBy)
            .AsQueryable();

        if (!string.IsNullOrEmpty(StatusFilter))
        {
            query = query.Where(a => a.Status == StatusFilter);
        }

        if (!string.IsNullOrEmpty(CategoryFilter))
        {
            query = query.Where(a => a.Category == CategoryFilter);
        }

        if (!string.IsNullOrEmpty(PriorityFilter))
        {
            query = query.Where(a => a.Priority == PriorityFilter);
        }

        if (!string.IsNullOrEmpty(SearchTerm))
        {
            var term = SearchTerm.ToLower();
            query = query.Where(a =>
                a.Title.ToLower().Contains(term) ||
                (a.Description != null && a.Description.ToLower().Contains(term)));
        }

        TotalCount = await query.CountAsync();
        SuggestedActions = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(100)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(int id, string newStatus)
    {
        var action = await _context.SuggestedActions.FindAsync(id);
        if (action == null) return NotFound();

        action.Status = newStatus;
        action.UpdatedAt = DateTime.UtcNow;

        if (newStatus == ActionStatus.Approved ||
            newStatus == ActionStatus.Rejected ||
            newStatus == ActionStatus.Implemented)
        {
            action.ReviewedAt = DateTime.UtcNow;
            var reviewerEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (reviewerEmail != null)
            {
                var reviewer = await _context.Users.FirstOrDefaultAsync(u => u.Email == reviewerEmail);
                if (reviewer != null)
                {
                    action.ReviewedById = reviewer.Id;
                }
            }
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Status updated to {ActionStatus.DisplayName(newStatus)}.";

        return RedirectToPage();
    }
}
