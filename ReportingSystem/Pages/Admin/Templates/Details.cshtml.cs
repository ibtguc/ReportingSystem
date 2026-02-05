using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.Templates;

public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DetailsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public ReportTemplate Template { get; set; } = null!;
    public List<ReportField> Fields { get; set; } = new();
    public List<ReportTemplateAssignment> Assignments { get; set; } = new();
    public int ActivePeriodCount { get; set; }
    public int TotalReportCount { get; set; }

    // For adding fields inline
    [BindProperty]
    public ReportField NewField { get; set; } = new();

    public List<SelectListItem> FieldTypeOptions { get; set; } = new();

    // For adding assignments inline
    [BindProperty]
    public ReportTemplateAssignment NewAssignment { get; set; } = new();

    public List<SelectListItem> AssignmentTypeOptions { get; set; } = new();
    public List<SelectListItem> OrgUnitOptions { get; set; } = new();
    public List<SelectListItem> RoleOptions { get; set; } = new();
    public List<SelectListItem> UserOptions { get; set; } = new();

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

        Fields = await _context.ReportFields
            .Where(f => f.ReportTemplateId == id)
            .OrderBy(f => f.SectionOrder)
            .ThenBy(f => f.FieldOrder)
            .ToListAsync();

        Assignments = await _context.ReportTemplateAssignments
            .Where(a => a.ReportTemplateId == id)
            .ToListAsync();

        ActivePeriodCount = await _context.ReportPeriods
            .CountAsync(p => p.ReportTemplateId == id && p.IsActive);

        TotalReportCount = await _context.Reports
            .CountAsync(r => r.ReportTemplateId == id);

        await LoadDropdownsAsync();

        NewField.ReportTemplateId = id;
        NewAssignment.ReportTemplateId = id;

        return Page();
    }

    public async Task<IActionResult> OnPostAddFieldAsync(int id)
    {
        var template = await _context.ReportTemplates.FindAsync(id);
        if (template == null) return NotFound();

        NewField.ReportTemplateId = id;
        NewField.CreatedAt = DateTime.UtcNow;

        // Auto-set field order
        var maxOrder = await _context.ReportFields
            .Where(f => f.ReportTemplateId == id && f.Section == NewField.Section)
            .MaxAsync(f => (int?)f.FieldOrder) ?? 0;
        NewField.FieldOrder = maxOrder + 1;

        // Auto-generate field key if not provided
        if (string.IsNullOrWhiteSpace(NewField.FieldKey))
        {
            NewField.FieldKey = NewField.Label.ToLowerInvariant()
                .Replace(" ", "_")
                .Replace("-", "_");
        }

        _context.ReportFields.Add(NewField);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Field '{NewField.Label}' added successfully.";
        return RedirectToPage("Details", new { id });
    }

    public async Task<IActionResult> OnPostDeleteFieldAsync(int id, int fieldId)
    {
        var field = await _context.ReportFields.FindAsync(fieldId);
        if (field == null || field.ReportTemplateId != id)
        {
            return NotFound();
        }

        // Check if field has values
        var hasValues = await _context.ReportFieldValues.AnyAsync(v => v.ReportFieldId == fieldId);
        if (hasValues)
        {
            TempData["ErrorMessage"] = "Cannot delete field with existing report data. Deactivate it instead.";
            return RedirectToPage("Details", new { id });
        }

        _context.ReportFields.Remove(field);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Field '{field.Label}' deleted.";
        return RedirectToPage("Details", new { id });
    }

    public async Task<IActionResult> OnPostAddAssignmentAsync(int id)
    {
        var template = await _context.ReportTemplates.FindAsync(id);
        if (template == null) return NotFound();

        NewAssignment.ReportTemplateId = id;
        NewAssignment.CreatedAt = DateTime.UtcNow;

        _context.ReportTemplateAssignments.Add(NewAssignment);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Assignment added successfully.";
        return RedirectToPage("Details", new { id });
    }

    public async Task<IActionResult> OnPostDeleteAssignmentAsync(int id, int assignmentId)
    {
        var assignment = await _context.ReportTemplateAssignments.FindAsync(assignmentId);
        if (assignment == null || assignment.ReportTemplateId != id)
        {
            return NotFound();
        }

        _context.ReportTemplateAssignments.Remove(assignment);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Assignment removed.";
        return RedirectToPage("Details", new { id });
    }

    private async Task LoadDropdownsAsync()
    {
        FieldTypeOptions = Enum.GetValues<FieldType>()
            .Select(ft => new SelectListItem(ft.ToString(), ((int)ft).ToString()))
            .ToList();

        AssignmentTypeOptions = TemplateAssignmentType.All
            .Select(t => new SelectListItem(TemplateAssignmentType.DisplayName(t), t))
            .ToList();

        var orgUnits = await _context.OrganizationalUnits
            .Where(ou => ou.IsActive)
            .OrderBy(ou => ou.Level)
            .ThenBy(ou => ou.Name)
            .ToListAsync();

        OrgUnitOptions = orgUnits
            .Select(ou => new SelectListItem(
                $"{new string('\u00A0', (int)ou.Level * 2)}{ou.Name} ({ou.Level})",
                ou.Id.ToString()))
            .ToList();

        RoleOptions = SystemRoles.All
            .Select(r => new SelectListItem(SystemRoles.DisplayName(r), r))
            .ToList();

        UserOptions = await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .Select(u => new SelectListItem($"{u.Name} ({u.Email})", u.Id.ToString()))
            .ToListAsync();
    }
}
