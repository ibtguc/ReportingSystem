using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Users;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public User User { get; set; } = new();

    public IActionResult OnGet()
    {
        // Set default values
        User.IsActive = true;
        User.Role = "Administrator";

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Check if user with same email already exists
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
