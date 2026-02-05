using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.Reports;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public int SelectedTemplateId { get; set; }

    [BindProperty]
    public int SelectedPeriodId { get; set; }

    [BindProperty]
    public int SelectedUserId { get; set; }

    public List<SelectListItem> TemplateOptions { get; set; } = new();
    public List<SelectListItem> PeriodOptions { get; set; } = new();
    public List<SelectListItem> UserOptions { get; set; } = new();

    public async Task OnGetAsync(int? templateId)
    {
        await LoadDropdownsAsync(templateId);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Validate selections
        var template = await _context.ReportTemplates.FindAsync(SelectedTemplateId);
        if (template == null)
        {
            ModelState.AddModelError("SelectedTemplateId", "Invalid template.");
            await LoadDropdownsAsync(null);
            return Page();
        }

        var period = await _context.ReportPeriods.FindAsync(SelectedPeriodId);
        if (period == null || period.ReportTemplateId != SelectedTemplateId)
        {
            ModelState.AddModelError("SelectedPeriodId", "Invalid period for this template.");
            await LoadDropdownsAsync(SelectedTemplateId);
            return Page();
        }

        if (period.Status != PeriodStatus.Open)
        {
            ModelState.AddModelError("SelectedPeriodId", "This period is not open for submissions.");
            await LoadDropdownsAsync(SelectedTemplateId);
            return Page();
        }

        var user = await _context.Users.FindAsync(SelectedUserId);
        if (user == null)
        {
            ModelState.AddModelError("SelectedUserId", "Invalid user.");
            await LoadDropdownsAsync(SelectedTemplateId);
            return Page();
        }

        // Check if report already exists for this user/template/period
        var existing = await _context.Reports
            .FirstOrDefaultAsync(r =>
                r.ReportTemplateId == SelectedTemplateId &&
                r.ReportPeriodId == SelectedPeriodId &&
                r.SubmittedById == SelectedUserId);

        if (existing != null)
        {
            TempData["ErrorMessage"] = "A report already exists for this user, template, and period.";
            return RedirectToPage("Fill", new { id = existing.Id });
        }

        // Create the report
        var report = new Report
        {
            ReportTemplateId = SelectedTemplateId,
            ReportPeriodId = SelectedPeriodId,
            SubmittedById = SelectedUserId,
            Status = ReportStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reports.Add(report);
        await _context.SaveChangesAsync();

        // Create empty field values for all active fields
        var fields = await _context.ReportFields
            .Where(f => f.ReportTemplateId == SelectedTemplateId && f.IsActive)
            .ToListAsync();

        // Check for pre-population
        ReportFieldValue[]? previousValues = null;
        if (template.AllowPrePopulation)
        {
            var previousReport = await _context.Reports
                .Include(r => r.FieldValues)
                .Where(r => r.ReportTemplateId == SelectedTemplateId &&
                            r.SubmittedById == SelectedUserId &&
                            r.Id != report.Id &&
                            r.Status != ReportStatus.Draft)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            previousValues = previousReport?.FieldValues.ToArray();
        }

        foreach (var field in fields)
        {
            var fieldValue = new ReportFieldValue
            {
                ReportId = report.Id,
                ReportFieldId = field.Id,
                CreatedAt = DateTime.UtcNow
            };

            // Pre-populate from previous period if available
            if (previousValues != null && field.PrePopulateFromPrevious)
            {
                var prev = previousValues.FirstOrDefault(v => v.ReportFieldId == field.Id);
                if (prev != null)
                {
                    fieldValue.Value = prev.Value;
                    fieldValue.NumericValue = prev.NumericValue;
                    fieldValue.WasPrePopulated = true;
                    report.WasPrePopulated = true;
                }
            }

            // Apply default value if no pre-populated value
            if (fieldValue.Value == null && !string.IsNullOrEmpty(field.DefaultValue))
            {
                fieldValue.Value = field.DefaultValue;
            }

            _context.ReportFieldValues.Add(fieldValue);
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Report created. Fill in the fields below.";
        return RedirectToPage("Fill", new { id = report.Id });
    }

    private async Task LoadDropdownsAsync(int? templateId)
    {
        TemplateOptions = await _context.ReportTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new SelectListItem(t.Name, t.Id.ToString()))
            .ToListAsync();

        if (templateId.HasValue)
        {
            SelectedTemplateId = templateId.Value;
            PeriodOptions = await _context.ReportPeriods
                .Where(p => p.ReportTemplateId == templateId.Value && p.Status == PeriodStatus.Open && p.IsActive)
                .OrderByDescending(p => p.StartDate)
                .Select(p => new SelectListItem(p.Name, p.Id.ToString()))
                .ToListAsync();
        }

        UserOptions = await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .Select(u => new SelectListItem($"{u.Name} ({u.Email})", u.Id.ToString()))
            .ToListAsync();
    }
}
