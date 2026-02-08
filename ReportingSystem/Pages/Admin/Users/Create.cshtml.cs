using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

    public IActionResult OnGet()
    {
        User.IsActive = true;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == User.Email.ToLower());

        if (existingUser != null)
        {
            ModelState.AddModelError("User.Email", "A user with this email already exists.");
            return Page();
        }

        User.CreatedAt = DateTime.UtcNow;

        _context.Users.Add(User);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"User '{User.Name}' created successfully!";
        return RedirectToPage("./Index");
    }
}
