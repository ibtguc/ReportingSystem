using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Meetings;

[Authorize]
public class ActionItemsModel : PageModel
{
    private readonly MeetingService _meetingService;

    public ActionItemsModel(MeetingService meetingService)
    {
        _meetingService = meetingService;
    }

    public List<ActionItem> ActionItems { get; set; } = new();
    public List<ActionItem> OverdueItems { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public ActionItemStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool ShowMine { get; set; }

    public async Task OnGetAsync()
    {
        var userId = GetUserId();

        if (ShowMine)
        {
            ActionItems = await _meetingService.GetActionItemsForUserAsync(userId);
        }
        else
        {
            ActionItems = await _meetingService.GetAllActionItemsAsync(status: Status);
        }

        if (ShowMine && Status.HasValue)
            ActionItems = ActionItems.Where(a => a.Status == Status.Value).ToList();

        OverdueItems = await _meetingService.GetOverdueActionItemsAsync();
    }

    public async Task<IActionResult> OnPostStartAsync(int actionItemId, int meetingId)
    {
        await _meetingService.StartActionItemAsync(actionItemId);
        TempData["SuccessMessage"] = "Action item marked as In Progress.";
        return RedirectToPage(new { ShowMine });
    }

    public async Task<IActionResult> OnPostCompleteAsync(int actionItemId, int meetingId)
    {
        await _meetingService.CompleteActionItemAsync(actionItemId);
        TempData["SuccessMessage"] = "Action item marked as Completed.";
        return RedirectToPage(new { ShowMine });
    }

    public async Task<IActionResult> OnPostVerifyAsync(int actionItemId, int meetingId)
    {
        await _meetingService.VerifyActionItemAsync(actionItemId);
        TempData["SuccessMessage"] = "Action item verified.";
        return RedirectToPage(new { ShowMine });
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
}
