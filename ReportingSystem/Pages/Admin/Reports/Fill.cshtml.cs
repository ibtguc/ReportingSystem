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

    // Upward flow items (Phase 4)
    public List<SuggestedAction> SuggestedActions { get; set; } = new();
    public List<ResourceRequest> ResourceRequests { get; set; } = new();
    public List<SupportRequest> SupportRequests { get; set; } = new();

    [BindProperty]
    public Dictionary<int, string?> Values { get; set; } = new();

    [BindProperty]
    public string SubmitAction { get; set; } = "save";

    // Upward flow input models
    [BindProperty]
    public SuggestedActionInput? NewSuggestedAction { get; set; }

    [BindProperty]
    public ResourceRequestInput? NewResourceRequest { get; set; }

    [BindProperty]
    public SupportRequestInput? NewSupportRequest { get; set; }

    public class SuggestedActionInput
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Justification { get; set; }
        public string? ExpectedOutcome { get; set; }
        public string? Timeline { get; set; }
        public string Category { get; set; } = ActionCategory.ProcessImprovement;
        public string Priority { get; set; } = ActionPriority.Medium;
    }

    public class ResourceRequestInput
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Quantity { get; set; }
        public string? Justification { get; set; }
        public string Category { get; set; } = ResourceCategory.Equipment;
        public string Urgency { get; set; } = ResourceUrgency.Medium;
        public decimal? EstimatedCost { get; set; }
        public string? Currency { get; set; } = "EGP";
    }

    public class SupportRequestInput
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? CurrentSituation { get; set; }
        public string? DesiredOutcome { get; set; }
        public string Category { get; set; } = SupportCategory.TechnicalAssistance;
        public string Urgency { get; set; } = SupportUrgency.Medium;
    }

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
            .Include(r => r.SuggestedActions)
            .Include(r => r.ResourceRequests)
            .Include(r => r.SupportRequests)
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

        // Load upward flow items
        SuggestedActions = report.SuggestedActions.OrderBy(a => a.CreatedAt).ToList();
        ResourceRequests = report.ResourceRequests.OrderBy(r => r.CreatedAt).ToList();
        SupportRequests = report.SupportRequests.OrderBy(s => s.CreatedAt).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAddSuggestedActionAsync(int id)
    {
        var report = await _context.Reports
            .Include(r => r.ReportTemplate)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (report == null) return NotFound();
        if (!report.IsEditable || !report.ReportTemplate.IncludeSuggestedActions)
        {
            return RedirectToPage("Fill", new { id });
        }

        if (NewSuggestedAction != null && !string.IsNullOrWhiteSpace(NewSuggestedAction.Title))
        {
            var action = new SuggestedAction
            {
                ReportId = id,
                Title = NewSuggestedAction.Title,
                Description = NewSuggestedAction.Description ?? string.Empty,
                Justification = NewSuggestedAction.Justification,
                ExpectedOutcome = NewSuggestedAction.ExpectedOutcome,
                Timeline = NewSuggestedAction.Timeline,
                Category = NewSuggestedAction.Category,
                Priority = NewSuggestedAction.Priority,
                Status = ActionStatus.Submitted,
                CreatedAt = DateTime.UtcNow
            };
            _context.SuggestedActions.Add(action);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Suggested action added.";
        }

        return RedirectToPage("Fill", new { id });
    }

    public async Task<IActionResult> OnPostRemoveSuggestedActionAsync(int id, int actionId)
    {
        var action = await _context.SuggestedActions
            .Include(a => a.Report)
            .FirstOrDefaultAsync(a => a.Id == actionId && a.ReportId == id);

        if (action == null) return NotFound();
        if (!action.Report.IsEditable)
        {
            return RedirectToPage("Fill", new { id });
        }

        _context.SuggestedActions.Remove(action);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Suggested action removed.";

        return RedirectToPage("Fill", new { id });
    }

    public async Task<IActionResult> OnPostAddResourceRequestAsync(int id)
    {
        var report = await _context.Reports
            .Include(r => r.ReportTemplate)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (report == null) return NotFound();
        if (!report.IsEditable || !report.ReportTemplate.IncludeNeededResources)
        {
            return RedirectToPage("Fill", new { id });
        }

        if (NewResourceRequest != null && !string.IsNullOrWhiteSpace(NewResourceRequest.Title))
        {
            var request = new ResourceRequest
            {
                ReportId = id,
                Title = NewResourceRequest.Title,
                Description = NewResourceRequest.Description ?? string.Empty,
                Quantity = NewResourceRequest.Quantity,
                Justification = NewResourceRequest.Justification,
                Category = NewResourceRequest.Category,
                Urgency = NewResourceRequest.Urgency,
                EstimatedCost = NewResourceRequest.EstimatedCost,
                Currency = NewResourceRequest.Currency ?? "EGP",
                Status = ResourceStatus.Submitted,
                CreatedAt = DateTime.UtcNow
            };
            _context.ResourceRequests.Add(request);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Resource request added.";
        }

        return RedirectToPage("Fill", new { id });
    }

    public async Task<IActionResult> OnPostRemoveResourceRequestAsync(int id, int requestId)
    {
        var request = await _context.ResourceRequests
            .Include(r => r.Report)
            .FirstOrDefaultAsync(r => r.Id == requestId && r.ReportId == id);

        if (request == null) return NotFound();
        if (!request.Report.IsEditable)
        {
            return RedirectToPage("Fill", new { id });
        }

        _context.ResourceRequests.Remove(request);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Resource request removed.";

        return RedirectToPage("Fill", new { id });
    }

    public async Task<IActionResult> OnPostAddSupportRequestAsync(int id)
    {
        var report = await _context.Reports
            .Include(r => r.ReportTemplate)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (report == null) return NotFound();
        if (!report.IsEditable || !report.ReportTemplate.IncludeNeededSupport)
        {
            return RedirectToPage("Fill", new { id });
        }

        if (NewSupportRequest != null && !string.IsNullOrWhiteSpace(NewSupportRequest.Title))
        {
            var request = new SupportRequest
            {
                ReportId = id,
                Title = NewSupportRequest.Title,
                Description = NewSupportRequest.Description ?? string.Empty,
                CurrentSituation = NewSupportRequest.CurrentSituation,
                DesiredOutcome = NewSupportRequest.DesiredOutcome,
                Category = NewSupportRequest.Category,
                Urgency = NewSupportRequest.Urgency,
                Status = SupportStatus.Submitted,
                CreatedAt = DateTime.UtcNow
            };
            _context.SupportRequests.Add(request);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Support request added.";
        }

        return RedirectToPage("Fill", new { id });
    }

    public async Task<IActionResult> OnPostRemoveSupportRequestAsync(int id, int requestId)
    {
        var request = await _context.SupportRequests
            .Include(r => r.Report)
            .FirstOrDefaultAsync(r => r.Id == requestId && r.ReportId == id);

        if (request == null) return NotFound();
        if (!request.Report.IsEditable)
        {
            return RedirectToPage("Fill", new { id });
        }

        _context.SupportRequests.Remove(request);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Support request removed.";

        return RedirectToPage("Fill", new { id });
    }
}
