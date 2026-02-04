using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.Templates;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public ReportTemplate Template { get; set; } = new();

    public List<SelectListItem> ScheduleOptions { get; set; } = new();

    public void OnGet()
    {
        LoadDropdowns();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            LoadDropdowns();
            return Page();
        }

        // Set creator from current user
        var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (userEmail != null)
        {
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (currentUser != null)
            {
                Template.CreatedById = currentUser.Id;
            }
        }

        Template.CreatedAt = DateTime.UtcNow;
        Template.Version = 1;

        _context.ReportTemplates.Add(Template);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Template '{Template.Name}' created successfully.";
        return RedirectToPage("Details", new { id = Template.Id });
    }

    private void LoadDropdowns()
    {
        ScheduleOptions = ReportSchedule.All
            .Select(s => new SelectListItem(ReportSchedule.DisplayName(s), s))
            .ToList();
    }
}
