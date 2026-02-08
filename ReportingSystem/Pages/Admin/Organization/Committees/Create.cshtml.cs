using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Admin.Organization.Committees;

public class CreateModel : PageModel
{
    private readonly OrganizationService _orgService;

    public CreateModel(OrganizationService orgService)
    {
        _orgService = orgService;
    }

    [BindProperty]
    public Committee Committee { get; set; } = new();

    public List<SelectListItem> ParentOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadParentOptions();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadParentOptions();
            return Page();
        }

        await _orgService.CreateCommitteeAsync(Committee);

        TempData["SuccessMessage"] = $"Committee '{Committee.Name}' created successfully!";
        return RedirectToPage("Index");
    }

    private async Task LoadParentOptions()
    {
        var parents = await _orgService.GetPotentialParentsAsync();
        ParentOptions = parents.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = $"[{c.HierarchyLevel}] {c.Name}"
        }).ToList();
        ParentOptions.Insert(0, new SelectListItem("(None - Root)", ""));
    }
}
