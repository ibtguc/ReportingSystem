using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Confidentiality;

[Authorize]
public class MarkModel : PageModel
{
    private readonly ConfidentialityService _confidentialityService;

    public MarkModel(ConfidentialityService confidentialityService)
    {
        _confidentialityService = confidentialityService;
    }

    [BindProperty(SupportsGet = true)]
    public ConfidentialItemType ItemType { get; set; }

    [BindProperty(SupportsGet = true)]
    public int ItemId { get; set; }

    public bool IsCurrentlyConfidential { get; set; }
    public ConfidentialityMarking? ActiveMarking { get; set; }
    public AccessImpactPreview? ImpactPreview { get; set; }
    public Committee? ItemCommittee { get; set; }
    public bool CanMark { get; set; }
    public string ItemTitle { get; set; } = string.Empty;

    [BindProperty]
    public string? Reason { get; set; }

    [BindProperty]
    public int? MinChairmanOfficeRank { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetUserId();
        CanMark = await _confidentialityService.CanUserMarkConfidentialAsync(ItemType, ItemId, userId);
        if (!CanMark)
        {
            TempData["ErrorMessage"] = "You do not have permission to manage confidentiality for this item.";
            return RedirectToItemPage();
        }

        ActiveMarking = await _confidentialityService.GetActiveMarkingAsync(ItemType, ItemId);
        IsCurrentlyConfidential = ActiveMarking != null;
        ItemCommittee = await _confidentialityService.GetItemCommitteeAsync(ItemType, ItemId);
        ItemTitle = await GetItemTitleAsync();

        // Show impact preview for marking
        if (!IsCurrentlyConfidential && ItemCommittee != null)
        {
            ImpactPreview = await _confidentialityService.GetAccessImpactPreviewAsync(
                ItemType, ItemId, ItemCommittee.Id, MinChairmanOfficeRank);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostMarkAsync()
    {
        var userId = GetUserId();
        if (!await _confidentialityService.CanUserMarkConfidentialAsync(ItemType, ItemId, userId))
        {
            TempData["ErrorMessage"] = "You do not have permission to mark this item.";
            return RedirectToItemPage();
        }

        var committee = await _confidentialityService.GetItemCommitteeAsync(ItemType, ItemId);
        if (committee == null)
        {
            TempData["ErrorMessage"] = "Could not determine the item's committee.";
            return RedirectToItemPage();
        }

        await _confidentialityService.MarkAsConfidentialAsync(
            ItemType, ItemId, userId, committee.Id, Reason, MinChairmanOfficeRank);

        TempData["SuccessMessage"] = "Item marked as confidential.";
        return RedirectToItemPage();
    }

    public async Task<IActionResult> OnPostUnmarkAsync()
    {
        var userId = GetUserId();
        var result = await _confidentialityService.RemoveConfidentialMarkingAsync(ItemType, ItemId, userId);

        if (result)
            TempData["SuccessMessage"] = "Confidentiality marking removed.";
        else
            TempData["ErrorMessage"] = "Unable to remove marking. Only the original marker or SystemAdmin can remove it.";

        return RedirectToItemPage();
    }

    private IActionResult RedirectToItemPage()
    {
        return ItemType switch
        {
            ConfidentialItemType.Report => RedirectToPage("/Reports/Details", new { id = ItemId }),
            ConfidentialItemType.Directive => RedirectToPage("/Directives/Details", new { id = ItemId }),
            ConfidentialItemType.Meeting => RedirectToPage("/Meetings/Details", new { id = ItemId }),
            _ => RedirectToPage("/Index")
        };
    }

    private async Task<string> GetItemTitleAsync()
    {
        // We'll use the committee service indirectly through the confidentiality service's context
        // For now, return a simple label based on type
        return $"{ItemType} #{ItemId}";
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
}
