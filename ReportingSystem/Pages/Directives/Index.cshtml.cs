using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Directives;

[Authorize]
public class IndexModel : PageModel
{
    private readonly DirectiveService _directiveService;
    private readonly ConfidentialityService _confidentialityService;

    public IndexModel(DirectiveService directiveService, ConfidentialityService confidentialityService)
    {
        _directiveService = directiveService;
        _confidentialityService = confidentialityService;
    }

    public List<Directive> Directives { get; set; } = new();
    public bool CanIssue { get; set; }

    [BindProperty(SupportsGet = true)]
    public DirectiveStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public DirectivePriority? Priority { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool ShowMine { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool IncludeClosed { get; set; }

    // Pipeline counts
    public int IssuedCount { get; set; }
    public int AcknowledgedCount { get; set; }
    public int InProgressCount { get; set; }
    public int ImplementedCount { get; set; }
    public int OverdueCount { get; set; }

    public async Task OnGetAsync()
    {
        var userId = GetUserId();
        CanIssue = await _directiveService.CanUserIssueDirectivesAsync(userId);

        List<Directive> allDirectives;
        if (ShowMine)
        {
            allDirectives = await _directiveService.GetDirectivesForUserAsync(userId, IncludeClosed);
        }
        else
        {
            allDirectives = await _directiveService.GetDirectivesAsync(
                status: Status,
                priority: Priority,
                includeClosed: IncludeClosed);
        }

        // Apply additional filters if ShowMine is combined with status/priority
        if (ShowMine && Status.HasValue)
            allDirectives = allDirectives.Where(d => d.Status == Status.Value).ToList();
        if (ShowMine && Priority.HasValue)
            allDirectives = allDirectives.Where(d => d.Priority == Priority.Value).ToList();

        // Filter out confidential items the user cannot access
        allDirectives = await _confidentialityService.FilterAccessibleDirectivesAsync(allDirectives, userId);

        Directives = allDirectives;

        // Compute pipeline counts from unfiltered user directives
        var stats = await _directiveService.GetDirectiveStatsAsync();
        IssuedCount = stats.issued;
        AcknowledgedCount = stats.acknowledged;
        InProgressCount = stats.inProgress;
        ImplementedCount = stats.implemented;
        OverdueCount = stats.overdue;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
}
