using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Admin.Organization.Committees;

public class DetailsModel : PageModel
{
    private readonly OrganizationService _orgService;

    public DetailsModel(OrganizationService orgService)
    {
        _orgService = orgService;
    }

    public Committee Committee { get; set; } = new();
    public List<CommitteeMembership> Heads { get; set; } = new();
    public List<CommitteeMembership> Members { get; set; } = new();
    public List<ShadowAssignment> Shadows { get; set; } = new();
    public List<Committee> SubCommittees { get; set; } = new();
    public List<SelectListItem> AvailableUsers { get; set; } = new();

    [BindProperty]
    public int NewMemberUserId { get; set; }

    [BindProperty]
    public CommitteeRole NewMemberRole { get; set; }

    /// <summary>
    /// Tracks which page the user came from so the back button returns correctly.
    /// Values: "org" (Org Tree), "dashboard" (Dashboard), "tree" (Committee Tree), default (Committees list).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? ReturnTo { get; set; }

    public string BackUrl => ReturnTo switch
    {
        "org" => $"/Admin/Organization?highlight={Committee.Id}",
        "dashboard" => "/Dashboard",
        "tree" => "/Admin/Organization/CommitteeTree",
        _ => "/Admin/Organization/Committees"
    };

    public string BackLabel => ReturnTo switch
    {
        "org" => "Back to Org Tree",
        "dashboard" => "Back to Dashboard",
        "tree" => "Back to Committee Tree",
        _ => "Back to Committees"
    };

    public string BackIcon => ReturnTo switch
    {
        "org" => "bi-diagram-3",
        "dashboard" => "bi-speedometer2",
        "tree" => "bi-diagram-2",
        _ => "bi-arrow-left"
    };

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();

        var committee = await _orgService.GetCommitteeByIdAsync(id.Value);
        if (committee == null) return NotFound();

        Committee = committee;
        LoadMembershipData();
        SubCommittees = await _orgService.GetSubCommitteesAsync(id.Value);
        await LoadAvailableUsers();
        return Page();
    }

    public async Task<IActionResult> OnPostAddMemberAsync(int id)
    {
        var membership = new CommitteeMembership
        {
            UserId = NewMemberUserId,
            CommitteeId = id,
            Role = NewMemberRole
        };
        await _orgService.AddMembershipAsync(membership);

        TempData["SuccessMessage"] = "Member added successfully!";
        return RedirectToPage("Details", new { id, returnTo = ReturnTo });
    }

    public async Task<IActionResult> OnPostRemoveMemberAsync(int id, int membershipId)
    {
        await _orgService.RemoveMembershipAsync(membershipId);
        TempData["SuccessMessage"] = "Member removed successfully!";
        return RedirectToPage("Details", new { id, returnTo = ReturnTo });
    }

    public async Task<IActionResult> OnPostToggleRoleAsync(int id, int membershipId, CommitteeRole newRole)
    {
        await _orgService.UpdateMembershipRoleAsync(membershipId, newRole);
        TempData["SuccessMessage"] = "Member role updated!";
        return RedirectToPage("Details", new { id, returnTo = ReturnTo });
    }

    private void LoadMembershipData()
    {
        var activeMemberships = Committee.Memberships.Where(m => m.EffectiveTo == null).ToList();
        Heads = activeMemberships.Where(m => m.Role == CommitteeRole.Head).ToList();
        Members = activeMemberships.Where(m => m.Role == CommitteeRole.Member).ToList();
        Shadows = Committee.ShadowAssignments.Where(s => s.IsActive).ToList();
    }

    private async Task LoadAvailableUsers()
    {
        var allUsers = await _orgService.GetAvailableUsersAsync();
        var existingUserIds = Committee.Memberships
            .Where(m => m.EffectiveTo == null)
            .Select(m => m.UserId)
            .ToHashSet();

        AvailableUsers = allUsers
            .Where(u => !existingUserIds.Contains(u.Id))
            .Select(u => new SelectListItem(
                $"{u.Name} ({u.Email})",
                u.Id.ToString()))
            .ToList();
    }
}
