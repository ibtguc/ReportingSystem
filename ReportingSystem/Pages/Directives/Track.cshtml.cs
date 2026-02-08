using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Directives;

[Authorize]
public class TrackModel : PageModel
{
    private readonly DirectiveService _directiveService;

    public TrackModel(DirectiveService directiveService)
    {
        _directiveService = directiveService;
    }

    public List<Directive> OverdueDirectives { get; set; } = new();
    public List<Directive> ApproachingDeadlineDirectives { get; set; } = new();

    public async Task OnGetAsync()
    {
        OverdueDirectives = await _directiveService.GetOverdueDirectivesAsync();
        ApproachingDeadlineDirectives = await _directiveService.GetApproachingDeadlineDirectivesAsync(7);
    }
}
