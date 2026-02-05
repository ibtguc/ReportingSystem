using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.Users;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public new User User { get; set; } = new();

    public List<SelectListItem> RoleOptions { get; set; } = new();
    public List<SelectListItem> OrgUnitOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        User.IsActive = true;
        User.Role = SystemRoles.Administrator;

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
            .FirstOrDefaultAsync(u => u.Email.ToLower() == User.Email.ToLower());

        if (existingUser != null)
        {
            ModelState.AddModelError("User.Email", "A user with this email already exists.");
            await LoadDropdowns();
            return Page();
        }

        User.CreatedAt = DateTime.UtcNow;

        _context.Users.Add(User);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"User '{User.Name}' created successfully!";
        return RedirectToPage("./Index");
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
