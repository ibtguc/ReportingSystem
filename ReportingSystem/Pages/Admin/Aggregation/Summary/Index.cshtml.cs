using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.Aggregation.Summary;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<AggregatedValue> AggregatedValues { get; set; } = new();
    public int TotalCount { get; set; }
    public int StaleCount { get; set; }
    public int WithAmendmentsCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? TemplateId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? PeriodId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? OrgUnitId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool OnlyWithAmendments { get; set; }

    public List<SelectListItem> TemplateOptions { get; set; } = new();
    public List<SelectListItem> PeriodOptions { get; set; } = new();
    public List<SelectListItem> OrgUnitOptions { get; set; } = new();
    public List<SelectListItem> StatusOptions { get; set; } = new();

    // Drill-down data
    public AggregatedValue? SelectedValue { get; set; }
    public List<Report> SourceReports { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? DrillDownId { get; set; }

    public async Task OnGetAsync()
    {
        await LoadFilterOptions();

        if (DrillDownId.HasValue)
        {
            await LoadDrillDownData();
        }
        else
        {
            await LoadAggregatedValues();
        }
    }

    private async Task LoadFilterOptions()
    {
        TemplateOptions = await _context.ReportTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
            .ToListAsync();

        PeriodOptions = await _context.ReportPeriods
            .Include(p => p.ReportTemplate)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.StartDate)
            .Take(50)
            .Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = $"{p.ReportTemplate.Name} - {p.Name}"
            })
            .ToListAsync();

        OrgUnitOptions = await _context.OrganizationalUnits
            .Where(o => o.IsActive)
            .OrderBy(o => o.Level)
            .ThenBy(o => o.Name)
            .Select(o => new SelectListItem
            {
                Value = o.Id.ToString(),
                Text = $"{new string('-', (int)o.Level * 2)} {o.Name}"
            })
            .ToListAsync();

        StatusOptions = AggregatedValueStatus.All
            .Select(s => new SelectListItem { Value = s, Text = AggregatedValueStatus.DisplayName(s) })
            .ToList();
    }

    private async Task LoadAggregatedValues()
    {
        var query = _context.AggregatedValues
            .Include(v => v.AggregationRule)
                .ThenInclude(r => r.ReportField)
                    .ThenInclude(f => f.ReportTemplate)
            .Include(v => v.ReportPeriod)
            .Include(v => v.OrganizationalUnit)
            .Include(v => v.ComputedBy)
            .Include(v => v.Amendments.Where(a => a.IsActive))
            .AsQueryable();

        if (TemplateId.HasValue)
        {
            query = query.Where(v => v.AggregationRule.ReportField.ReportTemplateId == TemplateId.Value);
        }

        if (PeriodId.HasValue)
        {
            query = query.Where(v => v.ReportPeriodId == PeriodId.Value);
        }

        if (OrgUnitId.HasValue)
        {
            query = query.Where(v => v.OrganizationalUnitId == OrgUnitId.Value);
        }

        if (!string.IsNullOrEmpty(StatusFilter))
        {
            query = query.Where(v => v.Status == StatusFilter);
        }

        if (OnlyWithAmendments)
        {
            query = query.Where(v => v.HasAmendment);
        }

        TotalCount = await query.CountAsync();
        StaleCount = await _context.AggregatedValues.Where(v => v.Status == AggregatedValueStatus.Stale).CountAsync();
        WithAmendmentsCount = await _context.AggregatedValues.Where(v => v.HasAmendment).CountAsync();

        AggregatedValues = await query
            .OrderByDescending(v => v.ComputedAt)
            .Take(100)
            .ToListAsync();
    }

    private async Task LoadDrillDownData()
    {
        SelectedValue = await _context.AggregatedValues
            .Include(v => v.AggregationRule)
                .ThenInclude(r => r.ReportField)
                    .ThenInclude(f => f.ReportTemplate)
            .Include(v => v.ReportPeriod)
            .Include(v => v.OrganizationalUnit)
            .Include(v => v.ComputedBy)
            .Include(v => v.Amendments)
                .ThenInclude(a => a.AmendedBy)
            .FirstOrDefaultAsync(v => v.Id == DrillDownId);

        if (SelectedValue != null && !string.IsNullOrEmpty(SelectedValue.SourceReportIdsJson))
        {
            try
            {
                var sourceIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(SelectedValue.SourceReportIdsJson);
                if (sourceIds != null && sourceIds.Any())
                {
                    SourceReports = await _context.Reports
                        .Include(r => r.SubmittedBy)
                            .ThenInclude(u => u.OrganizationalUnit)
                        .Include(r => r.ReportTemplate)
                        .Include(r => r.FieldValues)
                            .ThenInclude(fv => fv.ReportField)
                        .Where(r => sourceIds.Contains(r.Id))
                        .OrderBy(r => r.SubmittedBy.OrganizationalUnit!.Name)
                        .ToListAsync();
                }
            }
            catch
            {
                // Invalid JSON, ignore
            }
        }
    }

    public async Task<IActionResult> OnPostRecomputeAsync(int id)
    {
        var value = await _context.AggregatedValues.FindAsync(id);
        if (value == null) return NotFound();

        // Mark as stale to trigger recomputation
        value.Status = AggregatedValueStatus.Stale;
        value.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Value marked for recomputation.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var value = await _context.AggregatedValues.FindAsync(id);
        if (value == null) return NotFound();

        _context.AggregatedValues.Remove(value);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Aggregated value deleted.";

        return RedirectToPage();
    }
}
