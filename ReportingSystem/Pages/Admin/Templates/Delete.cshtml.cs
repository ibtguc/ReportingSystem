using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.Templates;

public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DeleteModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public ReportTemplate Template { get; set; } = null!;

    public int FieldCount { get; set; }
    public int PeriodCount { get; set; }
    public int ReportCount { get; set; }
    public int AssignmentCount { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var template = await _context.ReportTemplates
            .Include(t => t.CreatedBy)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null)
        {
            return NotFound();
        }

        Template = template;
        await LoadCounts(id);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var template = await _context.ReportTemplates.FindAsync(Template.Id);
        if (template == null)
        {
            return NotFound();
        }

        // Check if there are reports using this template
        var hasReports = await _context.Reports.AnyAsync(r => r.ReportTemplateId == Template.Id);
        if (hasReports)
        {
            TempData["ErrorMessage"] = "Cannot delete template with existing reports. Deactivate it instead.";
            return RedirectToPage("Details", new { id = Template.Id });
        }

        // Delete assignments, fields, and periods first (cascade should handle this, but be explicit)
        var assignments = await _context.ReportTemplateAssignments
            .Where(a => a.ReportTemplateId == Template.Id).ToListAsync();
        _context.ReportTemplateAssignments.RemoveRange(assignments);

        var fields = await _context.ReportFields
            .Where(f => f.ReportTemplateId == Template.Id).ToListAsync();
        _context.ReportFields.RemoveRange(fields);

        var periods = await _context.ReportPeriods
            .Where(p => p.ReportTemplateId == Template.Id).ToListAsync();
        _context.ReportPeriods.RemoveRange(periods);

        _context.ReportTemplates.Remove(template);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Template '{template.Name}' deleted successfully.";
        return RedirectToPage("Index");
    }

    private async Task LoadCounts(int id)
    {
        FieldCount = await _context.ReportFields.CountAsync(f => f.ReportTemplateId == id);
        PeriodCount = await _context.ReportPeriods.CountAsync(p => p.ReportTemplateId == id);
        ReportCount = await _context.Reports.CountAsync(r => r.ReportTemplateId == id);
        AssignmentCount = await _context.ReportTemplateAssignments.CountAsync(a => a.ReportTemplateId == id);
    }
}
