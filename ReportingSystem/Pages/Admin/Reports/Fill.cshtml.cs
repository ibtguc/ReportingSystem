using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.Reports;

public class FillModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public FillModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public Report Report { get; set; } = null!;
    public List<ReportField> Fields { get; set; } = new();
    public Dictionary<int, ReportFieldValue> FieldValues { get; set; } = new();

    [BindProperty]
    public Dictionary<int, string?> Values { get; set; } = new();

    [BindProperty]
    public string SubmitAction { get; set; } = "save";

    public async Task<IActionResult> OnGetAsync(int id)
    {
        return await LoadReportAsync(id);
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var report = await _context.Reports
            .Include(r => r.ReportTemplate)
            .Include(r => r.ReportPeriod)
            .Include(r => r.SubmittedBy)
            .Include(r => r.FieldValues)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (report == null) return NotFound();
        if (!report.IsEditable)
        {
            TempData["ErrorMessage"] = "This report is locked and cannot be edited.";
            return RedirectToPage("View", new { id });
        }

        Report = report;

        Fields = await _context.ReportFields
            .Where(f => f.ReportTemplateId == report.ReportTemplateId && f.IsActive)
            .OrderBy(f => f.SectionOrder)
            .ThenBy(f => f.FieldOrder)
            .ToListAsync();

        // Validate required fields if submitting
        if (SubmitAction == "submit")
        {
            foreach (var field in Fields.Where(f => f.IsRequired))
            {
                var value = Values.GetValueOrDefault(field.Id);
                if (string.IsNullOrWhiteSpace(value))
                {
                    ModelState.AddModelError($"Values[{field.Id}]", $"{field.Label} is required.");
                }
            }

            // Validate numeric ranges
            foreach (var field in Fields.Where(f => f.Type == FieldType.Numeric))
            {
                var value = Values.GetValueOrDefault(field.Id);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    if (!double.TryParse(value, out var numVal))
                    {
                        ModelState.AddModelError($"Values[{field.Id}]", $"{field.Label} must be a valid number.");
                    }
                    else
                    {
                        if (field.MinValue.HasValue && numVal < field.MinValue.Value)
                        {
                            ModelState.AddModelError($"Values[{field.Id}]", $"{field.Label} must be at least {field.MinValue.Value}.");
                        }
                        if (field.MaxValue.HasValue && numVal > field.MaxValue.Value)
                        {
                            ModelState.AddModelError($"Values[{field.Id}]", $"{field.Label} must be at most {field.MaxValue.Value}.");
                        }
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                FieldValues = report.FieldValues.ToDictionary(fv => fv.ReportFieldId);
                return Page();
            }
        }

        // Save all field values
        foreach (var field in Fields)
        {
            var value = Values.GetValueOrDefault(field.Id);
            var existingValue = report.FieldValues.FirstOrDefault(fv => fv.ReportFieldId == field.Id);

            if (existingValue != null)
            {
                existingValue.Value = value;
                existingValue.UpdatedAt = DateTime.UtcNow;

                // Parse numeric value for aggregation support
                if (field.Type == FieldType.Numeric && double.TryParse(value, out var numVal))
                {
                    existingValue.NumericValue = numVal;
                }
                else
                {
                    existingValue.NumericValue = null;
                }
            }
            else
            {
                var newValue = new ReportFieldValue
                {
                    ReportId = report.Id,
                    ReportFieldId = field.Id,
                    Value = value,
                    CreatedAt = DateTime.UtcNow
                };

                if (field.Type == FieldType.Numeric && double.TryParse(value, out var numVal))
                {
                    newValue.NumericValue = numVal;
                }

                _context.ReportFieldValues.Add(newValue);
            }
        }

        report.UpdatedAt = DateTime.UtcNow;
        report.LastAutoSaveAt = DateTime.UtcNow;

        if (SubmitAction == "submit")
        {
            report.Status = ReportStatus.Submitted;
            report.SubmittedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        if (SubmitAction == "submit")
        {
            TempData["SuccessMessage"] = "Report submitted successfully.";
            return RedirectToPage("View", new { id });
        }

        TempData["SuccessMessage"] = "Report saved as draft.";
        return RedirectToPage("Fill", new { id });
    }

    private async Task<IActionResult> LoadReportAsync(int id)
    {
        var report = await _context.Reports
            .Include(r => r.ReportTemplate)
            .Include(r => r.ReportPeriod)
            .Include(r => r.SubmittedBy)
                .ThenInclude(u => u.OrganizationalUnit)
            .Include(r => r.FieldValues)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (report == null) return NotFound();

        if (!report.IsEditable)
        {
            return RedirectToPage("View", new { id });
        }

        Report = report;

        Fields = await _context.ReportFields
            .Where(f => f.ReportTemplateId == report.ReportTemplateId && f.IsActive)
            .OrderBy(f => f.SectionOrder)
            .ThenBy(f => f.FieldOrder)
            .ToListAsync();

        FieldValues = report.FieldValues.ToDictionary(fv => fv.ReportFieldId);

        // Populate Values dict from existing field values
        foreach (var fv in report.FieldValues)
        {
            Values[fv.ReportFieldId] = fv.Value;
        }

        return Page();
    }
}
