using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Admin.Organization.Committees;

public class EditModel : PageModel
{
    private readonly OrganizationService _orgService;

    public EditModel(OrganizationService orgService)
    {
        _orgService = orgService;
    }

    [BindProperty]
    public Committee Committee { get; set; } = new();

    public List<SelectListItem> ParentOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();

        var committee = await _orgService.GetCommitteeByIdAsync(id.Value);
        if (committee == null) return NotFound();

        Committee = committee;
        await LoadParentOptions(id.Value);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadParentOptions(Committee.Id);
            return Page();
        }

        await _orgService.UpdateCommitteeAsync(Committee);

        TempData["SuccessMessage"] = $"Committee '{Committee.Name}' updated successfully!";
        return RedirectToPage("Index");
    }

    private async Task LoadParentOptions(int excludeId)
    {
        var parents = await _orgService.GetPotentialParentsAsync(excludeId);
        ParentOptions = parents.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = $"[{c.HierarchyLevel}] {c.Name}"
        }).ToList();
        ParentOptions.Insert(0, new SelectListItem("(None - Root)", ""));
    }
}
