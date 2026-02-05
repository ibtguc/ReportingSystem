using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.OrgUnits;

public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public OrganizationalUnit OrgUnit { get; set; } = new();

    public List<SelectListItem> ParentOptions { get; set; } = new();
    public List<SelectListItem> LevelOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var orgUnit = await _context.OrganizationalUnits.FindAsync(id);
        if (orgUnit == null)
        {
            return NotFound();
        }

        OrgUnit = orgUnit;
        await LoadDropdowns(orgUnit.Id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdowns(OrgUnit.Id);
            return Page();
        }

        // Validate code uniqueness (excluding self)
        if (!string.IsNullOrEmpty(OrgUnit.Code))
        {
            var codeExists = await _context.OrganizationalUnits
                .AnyAsync(ou => ou.Code == OrgUnit.Code && ou.Id != OrgUnit.Id);
            if (codeExists)
            {
                ModelState.AddModelError("OrgUnit.Code", "An organizational unit with this code already exists.");
                await LoadDropdowns(OrgUnit.Id);
                return Page();
            }
        }

        // Prevent circular parent reference
        if (OrgUnit.ParentId.HasValue && OrgUnit.ParentId.Value == OrgUnit.Id)
        {
            ModelState.AddModelError("OrgUnit.ParentId", "A unit cannot be its own parent.");
            await LoadDropdowns(OrgUnit.Id);
            return Page();
        }

        // Check if setting parent to a descendant (circular hierarchy)
        if (OrgUnit.ParentId.HasValue)
        {
            var isDescendant = await IsDescendantAsync(OrgUnit.ParentId.Value, OrgUnit.Id);
            if (isDescendant)
            {
                ModelState.AddModelError("OrgUnit.ParentId", "Cannot set parent to a descendant unit (circular hierarchy).");
                await LoadDropdowns(OrgUnit.Id);
                return Page();
            }

            var parent = await _context.OrganizationalUnits.FindAsync(OrgUnit.ParentId.Value);
            if (parent != null && OrgUnit.Level <= parent.Level)
            {
                ModelState.AddModelError("OrgUnit.Level", "Child unit level must be deeper than parent level.");
                await LoadDropdowns(OrgUnit.Id);
                return Page();
            }
        }

        OrgUnit.UpdatedAt = DateTime.UtcNow;
        _context.Attach(OrgUnit).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.OrganizationalUnits.AnyAsync(e => e.Id == OrgUnit.Id))
            {
                return NotFound();
            }
            throw;
        }

        TempData["SuccessMessage"] = $"Organizational unit '{OrgUnit.Name}' updated successfully!";
        return RedirectToPage("./Index");
    }

    private async Task<bool> IsDescendantAsync(int candidateId, int ancestorId)
    {
        var current = await _context.OrganizationalUnits.FindAsync(candidateId);
        while (current != null && current.ParentId.HasValue)
        {
            if (current.ParentId.Value == ancestorId)
                return true;
            current = await _context.OrganizationalUnits.FindAsync(current.ParentId.Value);
        }
        return false;
    }

    private async Task LoadDropdowns(int excludeId)
    {
        // Get all descendants of the current unit to exclude from parent options
        var descendantIds = new HashSet<int>();
        await CollectDescendantIdsAsync(excludeId, descendantIds);

        var allUnits = await _context.OrganizationalUnits
            .Where(ou => ou.IsActive && ou.Id != excludeId && !descendantIds.Contains(ou.Id))
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

    private async Task CollectDescendantIdsAsync(int parentId, HashSet<int> ids)
    {
        var children = await _context.OrganizationalUnits
            .Where(ou => ou.ParentId == parentId)
            .Select(ou => ou.Id)
            .ToListAsync();

        foreach (var childId in children)
        {
            ids.Add(childId);
            await CollectDescendantIdsAsync(childId, ids);
        }
    }
}
