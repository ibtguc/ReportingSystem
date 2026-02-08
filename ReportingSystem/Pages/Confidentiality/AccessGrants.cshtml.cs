using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Confidentiality;

[Authorize]
public class AccessGrantsModel : PageModel
{
    private readonly ConfidentialityService _confidentialityService;
    private readonly ApplicationDbContext _context;

    public AccessGrantsModel(ConfidentialityService confidentialityService, ApplicationDbContext context)
    {
        _confidentialityService = confidentialityService;
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public ConfidentialItemType ItemType { get; set; }

    [BindProperty(SupportsGet = true)]
    public int ItemId { get; set; }

    public List<AccessGrant> Grants { get; set; } = new();
    public List<ConfidentialityMarking> MarkingHistory { get; set; } = new();
    public List<User> AvailableUsers { get; set; } = new();
    public bool CanManage { get; set; }

    [BindProperty]
    public int GrantToUserId { get; set; }

    [BindProperty]
    public string? GrantReason { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetUserId();
        CanManage = await _confidentialityService.CanUserMarkConfidentialAsync(ItemType, ItemId, userId);

        if (!CanManage)
        {
            TempData["ErrorMessage"] = "You do not have permission to manage access for this item.";
            return RedirectToItemPage();
        }

        Grants = await _confidentialityService.GetAccessGrantsAsync(ItemType, ItemId);
        MarkingHistory = await _confidentialityService.GetMarkingHistoryAsync(ItemType, ItemId);

        // Get users who don't already have grants
        var grantedUserIds = Grants.Select(g => g.GrantedToUserId).ToHashSet();
        AvailableUsers = await _context.Users
            .Where(u => u.IsActive && !grantedUserIds.Contains(u.Id))
            .OrderBy(u => u.Name)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostGrantAsync()
    {
        var userId = GetUserId();
        if (!await _confidentialityService.CanUserMarkConfidentialAsync(ItemType, ItemId, userId))
        {
            TempData["ErrorMessage"] = "You do not have permission to grant access.";
            return RedirectToPage(new { ItemType, ItemId });
        }

        await _confidentialityService.GrantAccessAsync(ItemType, ItemId, GrantToUserId, userId, GrantReason);
        TempData["SuccessMessage"] = "Access granted successfully.";
        return RedirectToPage(new { ItemType, ItemId });
    }

    public async Task<IActionResult> OnPostRevokeAsync(int grantId)
    {
        var userId = GetUserId();
        if (!await _confidentialityService.CanUserMarkConfidentialAsync(ItemType, ItemId, userId))
        {
            TempData["ErrorMessage"] = "You do not have permission to revoke access.";
            return RedirectToPage(new { ItemType, ItemId });
        }

        var result = await _confidentialityService.RevokeAccessAsync(grantId, userId);
        TempData[result ? "SuccessMessage" : "ErrorMessage"] =
            result ? "Access grant revoked." : "Unable to revoke access grant.";
        return RedirectToPage(new { ItemType, ItemId });
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

    private int GetUserId() =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
}
