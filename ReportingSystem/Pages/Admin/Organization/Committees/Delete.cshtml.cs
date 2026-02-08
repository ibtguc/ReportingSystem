using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Admin.Organization.Committees;

public class DeleteModel : PageModel
{
    private readonly OrganizationService _orgService;

    public DeleteModel(OrganizationService orgService)
    {
        _orgService = orgService;
    }

    public Committee Committee { get; set; } = new();
    public int MemberCount { get; set; }
    public int SubCommitteeCount { get; set; }
    public bool HasDependencies => MemberCount > 0 || SubCommitteeCount > 0;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();

        var committee = await _orgService.GetCommitteeByIdAsync(id.Value);
        if (committee == null) return NotFound();

        Committee = committee;
        MemberCount = committee.Memberships.Count(m => m.EffectiveTo == null);
        SubCommitteeCount = committee.SubCommittees.Count;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var committee = await _orgService.GetCommitteeByIdAsync(id);
        if (committee == null) return NotFound();

        if (committee.SubCommittees.Any())
        {
            TempData["ErrorMessage"] = "Cannot delete a committee that has sub-committees. Remove sub-committees first.";
            return RedirectToPage("Details", new { id });
        }

        var name = committee.Name;
        await _orgService.DeleteCommitteeAsync(id);

        TempData["SuccessMessage"] = $"Committee '{name}' deleted successfully!";
        return RedirectToPage("Index");
    }
}
