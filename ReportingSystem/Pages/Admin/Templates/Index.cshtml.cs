using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Admin.Templates;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ReportTemplateService _templateService;

    public IndexModel(ReportTemplateService templateService)
    {
        _templateService = templateService;
    }

    [BindProperty(SupportsGet = true)]
    public bool ShowInactive { get; set; }

    public List<ReportTemplate> Templates { get; set; } = new();
    public Dictionary<int, int> UsageCounts { get; set; } = new();

    public async Task OnGetAsync()
    {
        Templates = await _templateService.GetTemplatesAsync(includeInactive: ShowInactive);

        foreach (var template in Templates)
        {
            UsageCounts[template.Id] = await _templateService.GetTemplateUsageCountAsync(template.Id);
        }
    }
}
