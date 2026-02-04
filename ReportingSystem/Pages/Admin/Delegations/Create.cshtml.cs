using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.Delegations;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Delegation Delegation { get; set; } = new();

    public List<SelectListItem> UserOptions { get; set; } = new();
    public List<SelectListItem> ScopeOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        Delegation.StartDate = DateTime.UtcNow.Date;
        Delegation.EndDate = DateTime.UtcNow.Date.AddDays(7);

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

        if (Delegation.DelegatorId == Delegation.DelegateId)
        {
            ModelState.AddModelError("Delegation.DelegateId", "Delegator and delegate must be different users.");
            await LoadDropdowns();
            return Page();
        }

        if (Delegation.EndDate <= Delegation.StartDate)
        {
            ModelState.AddModelError("Delegation.EndDate", "End date must be after start date.");
            await LoadDropdowns();
            return Page();
        }

        // Check for overlapping active delegations from the same delegator
        var hasOverlap = await _context.Delegations.AnyAsync(d =>
            d.DelegatorId == Delegation.DelegatorId &&
            d.IsActive &&
            d.RevokedAt == null &&
            d.StartDate < Delegation.EndDate &&
            d.EndDate > Delegation.StartDate);

        if (hasOverlap)
        {
            ModelState.AddModelError("", "This delegator already has an active delegation that overlaps with the specified period.");
            await LoadDropdowns();
            return Page();
        }

        Delegation.CreatedAt = DateTime.UtcNow;
        Delegation.IsActive = true;

        _context.Delegations.Add(Delegation);
        await _context.SaveChangesAsync();

        var delegator = await _context.Users.FindAsync(Delegation.DelegatorId);
        var delegatee = await _context.Users.FindAsync(Delegation.DelegateId);

        TempData["SuccessMessage"] = $"Delegation from '{delegator?.Name}' to '{delegatee?.Name}' created successfully!";
        return RedirectToPage("./Index");
    }

    private async Task LoadDropdowns()
    {
        var users = await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .ToListAsync();

        UserOptions = users.Select(u => new SelectListItem(
            $"{u.Name} ({u.Email})",
            u.Id.ToString())).ToList();

        ScopeOptions = DelegationScope.All
            .Select(s => new SelectListItem(DelegationScope.DisplayName(s), s))
            .ToList();
    }
}
