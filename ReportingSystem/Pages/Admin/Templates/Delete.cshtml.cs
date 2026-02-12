using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Admin.Templates;

[Authorize]
public class DeleteModel : PageModel
{
    private readonly ReportTemplateService _templateService;

    public DeleteModel(ReportTemplateService templateService)
    {
        _templateService = templateService;
    }

    public ReportTemplate Template { get; set; } = null!;
    public int UsageCount { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var template = await _templateService.GetTemplateByIdAsync(id);
        if (template == null) return NotFound();
        if (template.IsDefault)
        {
            TempData["ErrorMessage"] = "Default templates cannot be deleted.";
            return RedirectToPage("Index");
        }

        Template = template;
        UsageCount = await _templateService.GetTemplateUsageCountAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var template = await _templateService.GetTemplateByIdAsync(id);
        if (template == null) return NotFound();
        if (template.IsDefault)
        {
            TempData["ErrorMessage"] = "Default templates cannot be deleted.";
            return RedirectToPage("Index");
        }

        var result = await _templateService.DeleteTemplateAsync(id);
        if (!result)
        {
            TempData["ErrorMessage"] = "Failed to delete template.";
            return RedirectToPage("Index");
        }

        var usageCount = await _templateService.GetTemplateUsageCountAsync(id);
        if (usageCount > 0)
            TempData["SuccessMessage"] = $"Template \"{template.Name}\" deactivated (in use by {usageCount} reports).";
        else
            TempData["SuccessMessage"] = $"Template \"{template.Name}\" deleted.";

        return RedirectToPage("Index");
    }
}
