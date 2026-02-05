using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.Delegations;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Delegation> Delegations { get; set; } = new();

    public async Task OnGetAsync()
    {
        Delegations = await _context.Delegations
            .Include(d => d.Delegator)
            .Include(d => d.Delegate)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostRevokeAsync(int id)
    {
        var delegation = await _context.Delegations.FindAsync(id);
        if (delegation == null)
        {
            return NotFound();
        }

        delegation.IsActive = false;
        delegation.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Delegation revoked successfully!";
        return RedirectToPage();
    }
}
