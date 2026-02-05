using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.Downward.Recommendations;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Recommendation> Recommendations { get; set; } = new();
    public int TotalCount { get; set; }
    public int OverdueCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? CategoryFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? PriorityFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ScopeFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool OnlyOverdue { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync()
    {
        var query = _context.Recommendations
            .Include(r => r.Report)
                .ThenInclude(r => r!.SubmittedBy)
            .Include(r => r.Report!.ReportTemplate)
            .Include(r => r.IssuedBy)
            .Include(r => r.TargetOrgUnit)
            .Include(r => r.TargetUser)
            .AsQueryable();

        if (!string.IsNullOrEmpty(StatusFilter))
        {
            query = query.Where(r => r.Status == StatusFilter);
        }

        if (!string.IsNullOrEmpty(CategoryFilter))
        {
            query = query.Where(r => r.Category == CategoryFilter);
        }

        if (!string.IsNullOrEmpty(PriorityFilter))
        {
            query = query.Where(r => r.Priority == PriorityFilter);
        }

        if (!string.IsNullOrEmpty(ScopeFilter))
        {
            query = query.Where(r => r.TargetScope == ScopeFilter);
        }

        if (OnlyOverdue)
        {
            query = query.Where(r => r.DueDate != null &&
                                     r.DueDate < DateTime.UtcNow &&
                                     r.Status != RecommendationStatus.Completed &&
                                     r.Status != RecommendationStatus.Cancelled);
        }

        if (!string.IsNullOrEmpty(SearchTerm))
        {
            var term = SearchTerm.ToLower();
            query = query.Where(r =>
                r.Title.ToLower().Contains(term) ||
                r.Description.ToLower().Contains(term) ||
                r.IssuedBy.Name.ToLower().Contains(term));
        }

        TotalCount = await query.CountAsync();
        OverdueCount = await _context.Recommendations
            .Where(r => r.DueDate != null &&
                       r.DueDate < DateTime.UtcNow &&
                       r.Status != RecommendationStatus.Completed &&
                       r.Status != RecommendationStatus.Cancelled)
            .CountAsync();

        Recommendations = await query
            .OrderByDescending(r => r.CreatedAt)
            .Take(100)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(int id, string newStatus)
    {
        var recommendation = await _context.Recommendations.FindAsync(id);
        if (recommendation == null) return NotFound();

        recommendation.Status = newStatus;
        recommendation.UpdatedAt = DateTime.UtcNow;

        if (newStatus == RecommendationStatus.Completed)
        {
            recommendation.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Status updated to {RecommendationStatus.DisplayName(newStatus)}.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var recommendation = await _context.Recommendations.FindAsync(id);
        if (recommendation == null) return NotFound();

        _context.Recommendations.Remove(recommendation);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Recommendation deleted.";

        return RedirectToPage();
    }
}
