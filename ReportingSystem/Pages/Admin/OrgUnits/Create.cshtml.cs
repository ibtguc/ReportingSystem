using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.OrgUnits;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public OrganizationalUnit OrgUnit { get; set; } = new();

    public List<SelectListItem> ParentOptions { get; set; } = new();
    public List<SelectListItem> LevelOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? parentId)
    {
        await LoadDropdowns();

        if (parentId.HasValue)
        {
            OrgUnit.ParentId = parentId.Value;
            var parent = await _context.OrganizationalUnits.FindAsync(parentId.Value);
            if (parent != null)
            {
                // Suggest the next level down from parent
                OrgUnit.Level = (OrgUnitLevel)Math.Min((int)parent.Level + 1, (int)OrgUnitLevel.Team);
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdowns();
            return Page();
        }

        // Validate code uniqueness
        if (!string.IsNullOrEmpty(OrgUnit.Code))
        {
            var codeExists = await _context.OrganizationalUnits
                .AnyAsync(ou => ou.Code == OrgUnit.Code);
            if (codeExists)
            {
                ModelState.AddModelError("OrgUnit.Code", "An organizational unit with this code already exists.");
                await LoadDropdowns();
                return Page();
            }
        }

        // Validate parent-child level consistency
        if (OrgUnit.ParentId.HasValue)
        {
            var parent = await _context.OrganizationalUnits.FindAsync(OrgUnit.ParentId.Value);
            if (parent != null && OrgUnit.Level <= parent.Level)
            {
                ModelState.AddModelError("OrgUnit.Level", "Child unit level must be deeper than parent level.");
                await LoadDropdowns();
                return Page();
            }
        }

        OrgUnit.CreatedAt = DateTime.UtcNow;
        _context.OrganizationalUnits.Add(OrgUnit);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Organizational unit '{OrgUnit.Name}' created successfully!";
        return RedirectToPage("./Index");
    }

    private async Task LoadDropdowns()
    {
        var allUnits = await _context.OrganizationalUnits
            .Where(ou => ou.IsActive)
            .OrderBy(ou => ou.Level)
            .ThenBy(ou => ou.SortOrder)
            .ThenBy(ou => ou.Name)
            .ToListAsync();

        ParentOptions = new List<SelectListItem>
        {
            new SelectListItem("(No Parent - Top Level)", "")
        };
        ParentOptions.AddRange(allUnits.Select(ou => new SelectListItem(
            $"{new string('\u00A0', (int)ou.Level * 4)}{ou.Name} ({ou.Level})",
            ou.Id.ToString())));

        LevelOptions = Enum.GetValues<OrgUnitLevel>()
            .Select(l => new SelectListItem(l.ToString(), ((int)l).ToString()))
            .ToList();
    }
}
