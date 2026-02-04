using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.Users;

public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public new User User { get; set; } = new();

    public List<SelectListItem> RoleOptions { get; set; } = new();
    public List<SelectListItem> OrgUnitOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        User = user;
        await LoadDropdowns();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdowns();
            return Page();
        }

        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == User.Email.ToLower() && u.Id != User.Id);

        if (existingUser != null)
        {
            ModelState.AddModelError("User.Email", "A user with this email already exists.");
            await LoadDropdowns();
            return Page();
        }

        _context.Attach(User).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!UserExists(User.Id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        TempData["SuccessMessage"] = $"User '{User.Name}' updated successfully!";
        return RedirectToPage("./Index");
    }

    private bool UserExists(int id)
    {
        return _context.Users.Any(e => e.Id == id);
    }

    private async Task LoadDropdowns()
    {
        RoleOptions = SystemRoles.All
            .Select(r => new SelectListItem(SystemRoles.DisplayName(r), r))
            .ToList();

        var orgUnits = await _context.OrganizationalUnits
            .Where(ou => ou.IsActive)
            .OrderBy(ou => ou.Level)
            .ThenBy(ou => ou.SortOrder)
            .ThenBy(ou => ou.Name)
            .ToListAsync();

        OrgUnitOptions = new List<SelectListItem>
        {
            new SelectListItem("(Not Assigned)", "")
        };
        OrgUnitOptions.AddRange(orgUnits.Select(ou => new SelectListItem(
            $"{new string('\u00A0', (int)ou.Level * 4)}{ou.Name} ({ou.Level})",
            ou.Id.ToString())));
    }
}
