using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.Periods;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public ReportTemplate Template { get; set; } = null!;
    public List<ReportPeriod> Periods { get; set; } = new();
    public Dictionary<int, int> ReportCounts { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int templateId)
    {
        var template = await _context.ReportTemplates.FindAsync(templateId);
        if (template == null)
        {
            return NotFound();
        }

        Template = template;

        Periods = await _context.ReportPeriods
            .Where(p => p.ReportTemplateId == templateId)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync();

        var periodIds = Periods.Select(p => p.Id).ToList();
        ReportCounts = await _context.Reports
            .Where(r => periodIds.Contains(r.ReportPeriodId))
            .GroupBy(r => r.ReportPeriodId)
            .ToDictionaryAsync(g => g.Key, g => g.Count());

        return Page();
    }

    public async Task<IActionResult> OnPostOpenAsync(int templateId, int periodId)
    {
        var period = await _context.ReportPeriods.FindAsync(periodId);
        if (period == null || period.ReportTemplateId != templateId)
        {
            return NotFound();
        }

        period.Status = PeriodStatus.Open;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Period '{period.Name}' is now open for submissions.";
        return RedirectToPage(new { templateId });
    }

    public async Task<IActionResult> OnPostCloseAsync(int templateId, int periodId)
    {
        var period = await _context.ReportPeriods.FindAsync(periodId);
        if (period == null || period.ReportTemplateId != templateId)
        {
            return NotFound();
        }

        period.Status = PeriodStatus.Closed;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Period '{period.Name}' has been closed.";
        return RedirectToPage(new { templateId });
    }
}
