using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.OrgUnits;

public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DeleteModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public OrganizationalUnit OrgUnit { get; set; } = new();

    public int ChildCount { get; set; }
    public int UserCount { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var orgUnit = await _context.OrganizationalUnits
            .Include(ou => ou.Parent)
            .Include(ou => ou.Children)
            .Include(ou => ou.Users)
            .FirstOrDefaultAsync(ou => ou.Id == id);

        if (orgUnit == null)
        {
            return NotFound();
        }

        OrgUnit = orgUnit;
        ChildCount = orgUnit.Children.Count;
        UserCount = orgUnit.Users.Count;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var orgUnit = await _context.OrganizationalUnits
            .Include(ou => ou.Children)
            .Include(ou => ou.Users)
            .FirstOrDefaultAsync(ou => ou.Id == id);

        if (orgUnit == null)
        {
            return RedirectToPage("./Index");
        }

        // Prevent deletion if unit has children
        if (orgUnit.Children.Any())
        {
            TempData["ErrorMessage"] = $"Cannot delete '{orgUnit.Name}' because it has {orgUnit.Children.Count} child unit(s). Delete or move them first.";
            return RedirectToPage("./Index");
        }

        // Unassign users from this unit before deleting
        foreach (var user in orgUnit.Users)
        {
            user.OrganizationalUnitId = null;
        }

        _context.OrganizationalUnits.Remove(orgUnit);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Organizational unit '{orgUnit.Name}' deleted successfully!";
        return RedirectToPage("./Index");
    }
}
