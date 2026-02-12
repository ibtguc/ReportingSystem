using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Admin.Templates;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly ReportTemplateService _templateService;

    public DetailsModel(ReportTemplateService templateService)
    {
        _templateService = templateService;
    }

    public ReportTemplate Template { get; set; } = null!;
    public int UsageCount { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var template = await _templateService.GetTemplateByIdAsync(id);
        if (template == null) return NotFound();

        Template = template;
        UsageCount = await _templateService.GetTemplateUsageCountAsync(id);
        return Page();
    }
}
