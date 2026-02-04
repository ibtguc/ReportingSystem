using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Departments;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Department Department { get; set; } = new();

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Check for duplicate department code
        var existingDept = await _context.Departments
            .FirstOrDefaultAsync(d => d.Name == Department.Name);

        if (existingDept != null)
        {
            ModelState.AddModelError("Department.Name", "A department with this code already exists.");
            return Page();
        }

        _context.Departments.Add(Department);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
