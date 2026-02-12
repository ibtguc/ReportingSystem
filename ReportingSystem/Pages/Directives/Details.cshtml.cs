using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Directives;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly DirectiveService _directiveService;
    private readonly ConfidentialityService _confidentialityService;
    private readonly AuditService _auditService;
    private readonly NotificationService _notificationService;

    public DetailsModel(DirectiveService directiveService, ConfidentialityService confidentialityService,
        AuditService auditService, NotificationService notificationService)
    {
        _directiveService = directiveService;
        _confidentialityService = confidentialityService;
        _auditService = auditService;
        _notificationService = notificationService;
    }

    public Directive Directive { get; set; } = null!;
    public DirectivePropagationNode PropagationTree { get; set; } = null!;
    public List<Committee> ForwardableCommittees { get; set; } = new();
    public bool IsIssuer { get; set; }
    public bool IsTarget { get; set; }
    public bool CanForward { get; set; }

    [BindProperty]
    public int? ForwardToCommitteeId { get; set; }

    [BindProperty]
    public string? ForwardAnnotation { get; set; }

    [BindProperty]
    public string? ActionComments { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var directive = await _directiveService.GetDirectiveByIdAsync(id);
        if (directive == null) return NotFound();

        // Confidentiality access check
        if (directive.IsConfidential)
        {
            var userId = GetUserId();
            if (!await _confidentialityService.CanUserAccessConfidentialItemAsync(
                ConfidentialItemType.Directive, id, userId))
            {
                TempData["ErrorMessage"] = "You do not have access to this confidential directive.";
                return RedirectToPage("Index");
            }
        }

        Directive = directive;
        await ComputePermissions();

        // Auto-mark as Delivered when target views it
        if (IsTarget && directive.Status == DirectiveStatus.Issued)
        {
            var userId = GetUserId();
            await _directiveService.MarkDeliveredAsync(id, userId);
            directive.Status = DirectiveStatus.Delivered;
        }

        PropagationTree = await _directiveService.GetPropagationTreeAsync(id);
        ForwardableCommittees = await _directiveService.GetForwardableCommitteesAsync(id);
        CanForward = IsTarget && ForwardableCommittees.Any()
            && directive.Status != DirectiveStatus.Closed;

        return Page();
    }

    public async Task<IActionResult> OnPostAcknowledgeAsync(int id)
    {
        var userId = GetUserId();
        var directive = await _directiveService.GetDirectiveByIdAsync(id);
        var success = await _directiveService.AcknowledgeAsync(id, userId);
        if (success && directive != null)
        {
            await _auditService.LogStatusChangeAsync("Directive", id, null, "Delivered", "Acknowledged", userId, User.Identity?.Name);
            await _notificationService.NotifyDirectiveStatusChangedAsync(id, directive.Title, directive.IssuerId, "Acknowledged");
        }
        TempData[success ? "SuccessMessage" : "ErrorMessage"] =
            success ? "Directive acknowledged." : "Unable to acknowledge directive.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostStartProgressAsync(int id)
    {
        var userId = GetUserId();
        var directive = await _directiveService.GetDirectiveByIdAsync(id);
        var success = await _directiveService.StartProgressAsync(id, userId);
        if (success && directive != null)
        {
            await _auditService.LogStatusChangeAsync("Directive", id, null, "Acknowledged", "InProgress", userId, User.Identity?.Name);
            await _notificationService.NotifyDirectiveStatusChangedAsync(id, directive.Title, directive.IssuerId, "In Progress");
        }
        TempData[success ? "SuccessMessage" : "ErrorMessage"] =
            success ? "Work started on directive." : "Unable to start progress.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostImplementAsync(int id)
    {
        var userId = GetUserId();
        var directive = await _directiveService.GetDirectiveByIdAsync(id);
        var success = await _directiveService.MarkImplementedAsync(id, userId, ActionComments);
        if (success && directive != null)
        {
            await _auditService.LogStatusChangeAsync("Directive", id, null, "InProgress", "Implemented", userId, User.Identity?.Name, ActionComments);
            await _notificationService.NotifyDirectiveStatusChangedAsync(id, directive.Title, directive.IssuerId, "Implemented");
        }
        TempData[success ? "SuccessMessage" : "ErrorMessage"] =
            success ? "Directive marked as implemented." : "Unable to mark as implemented.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostVerifyAsync(int id)
    {
        var userId = GetUserId();
        var directive = await _directiveService.GetDirectiveByIdAsync(id);
        var success = await _directiveService.VerifyAsync(id, userId, ActionComments);
        if (success && directive != null)
        {
            await _auditService.LogStatusChangeAsync("Directive", id, null, "Implemented", "Verified", userId, User.Identity?.Name, ActionComments);
            await _notificationService.NotifyDirectiveStatusChangedAsync(id, directive.Title, directive.IssuerId, "Verified");
        }
        TempData[success ? "SuccessMessage" : "ErrorMessage"] =
            success ? "Directive implementation verified." : "Unable to verify.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCloseAsync(int id)
    {
        var userId = GetUserId();
        var directive = await _directiveService.GetDirectiveByIdAsync(id);
        var success = await _directiveService.CloseAsync(id, userId, ActionComments);
        if (success && directive != null)
        {
            await _auditService.LogStatusChangeAsync("Directive", id, null, "Verified", "Closed", userId, User.Identity?.Name, ActionComments);
            await _notificationService.NotifyDirectiveStatusChangedAsync(id, directive.Title, directive.IssuerId, "Closed");
        }
        TempData[success ? "SuccessMessage" : "ErrorMessage"] =
            success ? "Directive closed." : "Unable to close directive.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostForwardAsync(int id)
    {
        if (!ForwardToCommitteeId.HasValue)
        {
            TempData["ErrorMessage"] = "Please select a committee to forward to.";
            return RedirectToPage(new { id });
        }

        var userId = GetUserId();
        var child = await _directiveService.ForwardDirectiveAsync(id, ForwardToCommitteeId.Value,
            ForwardAnnotation, userId);

        await _auditService.LogAsync(AuditActionType.DirectiveForwarded, "Directive", id,
            userId: userId, userName: User.Identity?.Name,
            details: $"Forwarded to committee {ForwardToCommitteeId.Value} as #{child.Id}");

        // Notify forwarded committee members
        await _notificationService.NotifyDirectiveIssuedAsync(
            child.Id, child.Title, child.TargetCommitteeId, child.TargetUserId);

        TempData["SuccessMessage"] = $"Directive forwarded as #{child.Id}.";
        return RedirectToPage(new { id });
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

    private async Task ComputePermissions()
    {
        var userId = GetUserId();
        IsIssuer = Directive.IssuerId == userId;
        IsTarget = await _directiveService.IsUserTargetOfDirectiveAsync(userId, Directive);
    }
}
