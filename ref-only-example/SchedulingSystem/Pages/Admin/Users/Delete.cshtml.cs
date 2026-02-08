using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Users;

public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DeleteModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public User User { get; set; } = new();

    public int MagicLinkCount { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _context.Users
            .Include(u => u.MagicLinks)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        User = user;
        MagicLinkCount = user.MagicLinks.Count;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _context.Users
            .Include(u => u.MagicLinks)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (user != null)
        {
            // Remove related magic links first
            _context.MagicLinks.RemoveRange(user.MagicLinks);

            // Remove the user
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"User '{user.Name}' deleted successfully!";
        }

        return RedirectToPage("./Index");
    }
}
