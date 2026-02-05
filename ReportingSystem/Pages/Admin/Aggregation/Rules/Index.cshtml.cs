using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.Aggregation.Rules;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<AggregationRule> Rules { get; set; } = new();
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? MethodFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? TemplateId { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool OnlyActive { get; set; } = true;

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public List<SelectListItem> TemplateOptions { get; set; } = new();
    public List<SelectListItem> MethodOptions { get; set; } = new();

    // For creating new rules
    [BindProperty]
    public int NewRuleFieldId { get; set; }

    [BindProperty]
    public string NewRuleName { get; set; } = string.Empty;

    [BindProperty]
    public string NewRuleMethod { get; set; } = AggregationMethod.Sum;

    [BindProperty]
    public string? NewRuleDescription { get; set; }

    public List<SelectListItem> FieldOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadFilterOptions();
        await LoadRules();
    }

    private async Task LoadFilterOptions()
    {
        TemplateOptions = await _context.ReportTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
            .ToListAsync();

        MethodOptions = AggregationMethod.All
            .Select(m => new SelectListItem { Value = m, Text = AggregationMethod.DisplayName(m) })
            .ToList();

        FieldOptions = await _context.ReportFields
            .Include(f => f.ReportTemplate)
            .Where(f => f.IsActive && f.ReportTemplate.IsActive)
            .OrderBy(f => f.ReportTemplate.Name)
            .ThenBy(f => f.Label)
            .Select(f => new SelectListItem
            {
                Value = f.Id.ToString(),
                Text = $"{f.ReportTemplate.Name} - {f.Label}"
            })
            .ToListAsync();
    }

    private async Task LoadRules()
    {
        var query = _context.AggregationRules
            .Include(r => r.ReportField)
                .ThenInclude(f => f.ReportTemplate)
            .AsQueryable();

        if (!string.IsNullOrEmpty(MethodFilter))
        {
            query = query.Where(r => r.Method == MethodFilter);
        }

        if (TemplateId.HasValue)
        {
            query = query.Where(r => r.ReportField.ReportTemplateId == TemplateId.Value);
        }

        if (OnlyActive)
        {
            query = query.Where(r => r.IsActive);
        }

        if (!string.IsNullOrEmpty(SearchTerm))
        {
            var term = SearchTerm.ToLower();
            query = query.Where(r =>
                r.Name.ToLower().Contains(term) ||
                r.ReportField.Label.ToLower().Contains(term) ||
                (r.Description != null && r.Description.ToLower().Contains(term)));
        }

        TotalCount = await query.CountAsync();
        ActiveCount = await _context.AggregationRules.Where(r => r.IsActive).CountAsync();

        Rules = await query
            .OrderBy(r => r.ReportField.ReportTemplate.Name)
            .ThenBy(r => r.ReportField.Label)
            .ThenBy(r => r.Priority)
            .Take(100)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateRuleAsync()
    {
        var field = await _context.ReportFields.FindAsync(NewRuleFieldId);
        if (field == null)
        {
            TempData["ErrorMessage"] = "Selected field not found.";
            return RedirectToPage();
        }

        var rule = new AggregationRule
        {
            ReportFieldId = NewRuleFieldId,
            Name = NewRuleName,
            Method = NewRuleMethod,
            Description = NewRuleDescription,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.AggregationRules.Add(rule);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Aggregation rule '{NewRuleName}' created successfully.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(int id)
    {
        var rule = await _context.AggregationRules.FindAsync(id);
        if (rule == null) return NotFound();

        rule.IsActive = !rule.IsActive;
        rule.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = rule.IsActive
            ? "Rule activated."
            : "Rule deactivated.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateMethodAsync(int id, string newMethod)
    {
        var rule = await _context.AggregationRules.FindAsync(id);
        if (rule == null) return NotFound();

        rule.Method = newMethod;
        rule.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Aggregation method updated to {AggregationMethod.DisplayName(newMethod)}.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var rule = await _context.AggregationRules
            .Include(r => r.AggregatedValues)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rule == null) return NotFound();

        if (rule.AggregatedValues.Any())
        {
            TempData["ErrorMessage"] = "Cannot delete rule with existing aggregated values. Deactivate it instead.";
            return RedirectToPage();
        }

        _context.AggregationRules.Remove(rule);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Aggregation rule deleted.";

        return RedirectToPage();
    }
}
