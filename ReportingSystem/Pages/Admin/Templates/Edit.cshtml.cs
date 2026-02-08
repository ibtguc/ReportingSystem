using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Admin.Templates;

[Authorize]
public class EditModel : PageModel
{
    private readonly ReportTemplateService _templateService;
    private readonly ApplicationDbContext _context;

    public EditModel(ReportTemplateService templateService, ApplicationDbContext context)
    {
        _templateService = templateService;
        _context = context;
    }

    [BindProperty]
    public ReportTemplate Template { get; set; } = null!;

    public List<SelectListItem> CommitteeOptions { get; set; } = new();
    public int UsageCount { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var template = await _templateService.GetTemplateByIdAsync(id);
        if (template == null) return NotFound();

        Template = template;
        UsageCount = await _templateService.GetTemplateUsageCountAsync(id);
        await LoadCommitteeOptions();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove("Template.CreatedBy");
        ModelState.Remove("Template.Committee");

        if (!ModelState.IsValid)
        {
            UsageCount = await _templateService.GetTemplateUsageCountAsync(Template.Id);
            await LoadCommitteeOptions();
            return Page();
        }

        var result = await _templateService.UpdateTemplateAsync(Template);
        if (result == null)
        {
            TempData["ErrorMessage"] = "Template not found.";
            return RedirectToPage("Index");
        }

        TempData["SuccessMessage"] = $"Template \"{result.Name}\" updated.";
        return RedirectToPage("Details", new { id = result.Id });
    }

    private async Task LoadCommitteeOptions()
    {
        CommitteeOptions = await _context.Committees
            .Where(c => c.IsActive)
            .OrderBy(c => c.HierarchyLevel)
            .ThenBy(c => c.Name)
            .Select(c => new SelectListItem($"[{c.HierarchyLevel}] {c.Name}", c.Id.ToString()))
            .ToListAsync();
    }
}
