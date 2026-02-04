using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.Periods;

public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public ReportTemplate Template { get; set; } = null!;

    [BindProperty]
    public ReportPeriod Period { get; set; } = null!;

    public int ReportCount { get; set; }

    public async Task<IActionResult> OnGetAsync(int templateId, int id)
    {
        var template = await _context.ReportTemplates.FindAsync(templateId);
        if (template == null) return NotFound();

        var period = await _context.ReportPeriods.FindAsync(id);
        if (period == null || period.ReportTemplateId != templateId) return NotFound();

        Template = template;
        Period = period;
        ReportCount = await _context.Reports.CountAsync(r => r.ReportPeriodId == id);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int templateId)
    {
        var template = await _context.ReportTemplates.FindAsync(templateId);
        if (template == null) return NotFound();

        Template = template;

        if (!ModelState.IsValid)
        {
            ReportCount = await _context.Reports.CountAsync(r => r.ReportPeriodId == Period.Id);
            return Page();
        }

        var existing = await _context.ReportPeriods.FindAsync(Period.Id);
        if (existing == null || existing.ReportTemplateId != templateId) return NotFound();

        existing.Name = Period.Name;
        existing.StartDate = Period.StartDate;
        existing.EndDate = Period.EndDate;
        existing.SubmissionDeadline = Period.SubmissionDeadline;
        existing.GracePeriodDays = Period.GracePeriodDays;
        existing.Status = Period.Status;
        existing.IsActive = Period.IsActive;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Period '{existing.Name}' updated successfully.";
        return RedirectToPage("Index", new { templateId });
    }
}
