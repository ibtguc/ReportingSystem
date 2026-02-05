using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.Downward.Decisions;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Decision> Decisions { get; set; } = new();
    public int TotalCount { get; set; }
    public int PendingAcknowledgmentCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? OutcomeFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? RequestTypeFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ReportId { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool OnlyPendingAck { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync()
    {
        var query = _context.Decisions
            .Include(d => d.Report)
                .ThenInclude(r => r.SubmittedBy)
            .Include(d => d.Report.ReportTemplate)
            .Include(d => d.DecidedBy)
            .Include(d => d.SuggestedAction)
            .Include(d => d.ResourceRequest)
            .Include(d => d.SupportRequest)
            .AsQueryable();

        if (!string.IsNullOrEmpty(OutcomeFilter))
        {
            query = query.Where(d => d.Outcome == OutcomeFilter);
        }

        if (!string.IsNullOrEmpty(RequestTypeFilter))
        {
            query = query.Where(d => d.RequestType == RequestTypeFilter);
        }

        if (ReportId.HasValue)
        {
            query = query.Where(d => d.ReportId == ReportId.Value);
        }

        if (OnlyPendingAck)
        {
            query = query.Where(d => !d.IsAcknowledged && d.Outcome != DecisionOutcome.Pending);
        }

        if (!string.IsNullOrEmpty(SearchTerm))
        {
            var term = SearchTerm.ToLower();
            query = query.Where(d =>
                d.Title.ToLower().Contains(term) ||
                d.Justification.ToLower().Contains(term) ||
                d.DecidedBy.Name.ToLower().Contains(term));
        }

        TotalCount = await query.CountAsync();
        PendingAcknowledgmentCount = await _context.Decisions
            .Where(d => !d.IsAcknowledged && d.Outcome != DecisionOutcome.Pending)
            .CountAsync();

        Decisions = await query
            .OrderByDescending(d => d.CreatedAt)
            .Take(100)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostUpdateOutcomeAsync(int id, string newOutcome)
    {
        var decision = await _context.Decisions.FindAsync(id);
        if (decision == null) return NotFound();

        decision.Outcome = newOutcome;
        decision.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Outcome updated to {DecisionOutcome.DisplayName(newOutcome)}.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkAcknowledgedAsync(int id)
    {
        var decision = await _context.Decisions.FindAsync(id);
        if (decision == null) return NotFound();

        decision.IsAcknowledged = true;
        decision.AcknowledgedAt = DateTime.UtcNow;
        decision.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Decision marked as acknowledged.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var decision = await _context.Decisions.FindAsync(id);
        if (decision == null) return NotFound();

        _context.Decisions.Remove(decision);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Decision deleted.";

        return RedirectToPage();
    }
}
