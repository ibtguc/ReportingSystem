using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Directives;

[Authorize]
public class CreateModel : PageModel
{
    private readonly DirectiveService _directiveService;
    private readonly ReportService _reportService;
    private readonly NotificationService _notificationService;

    public CreateModel(DirectiveService directiveService, ReportService reportService, NotificationService notificationService)
    {
        _directiveService = directiveService;
        _reportService = reportService;
        _notificationService = notificationService;
    }

    [BindProperty]
    public Directive Directive { get; set; } = new();

    public List<SelectListItem> CommitteeOptions { get; set; } = new();

    /// <summary>
    /// Pre-selected report ID from query string (when creating directive from report details).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int? ReportId { get; set; }

    public Report? LinkedReport { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetUserId();
        if (!await _directiveService.CanUserIssueDirectivesAsync(userId))
        {
            TempData["ErrorMessage"] = "You do not have permission to issue directives.";
            return RedirectToPage("Index");
        }

        await PopulateOptionsAsync(userId);

        if (ReportId.HasValue)
        {
            LinkedReport = await _reportService.GetReportByIdAsync(ReportId.Value);
            if (LinkedReport != null)
            {
                Directive.RelatedReportId = LinkedReport.Id;
                Directive.Title = $"Directive: {LinkedReport.Title}";
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = GetUserId();
        if (!await _directiveService.CanUserIssueDirectivesAsync(userId))
        {
            TempData["ErrorMessage"] = "You do not have permission to issue directives.";
            return RedirectToPage("Index");
        }

        ModelState.Remove("Directive.Issuer");
        ModelState.Remove("Directive.TargetCommittee");

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(userId);
            return Page();
        }

        var directive = await _directiveService.CreateDirectiveAsync(Directive, userId);

        await _notificationService.NotifyDirectiveIssuedAsync(
            directive.Id, directive.Title, directive.TargetCommitteeId, directive.TargetUserId);

        TempData["SuccessMessage"] = $"Directive \"{directive.Title}\" issued successfully.";
        return RedirectToPage("Details", new { id = directive.Id });
    }

    private async Task PopulateOptionsAsync(int userId)
    {
        var committees = await _directiveService.GetTargetableCommitteesAsync(userId);
        CommitteeOptions = committees.Select(c => new SelectListItem(
            $"{c.Name} ({c.HierarchyLevel})", c.Id.ToString())).ToList();
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
}
