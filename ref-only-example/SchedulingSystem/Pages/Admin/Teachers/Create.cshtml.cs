using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Teachers;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Teacher Teacher { get; set; } = new();

    public SelectList DepartmentList { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadSelectListsAsync();

        // Set default values
        Teacher.IsActive = true;
        Teacher.AvailableForSubstitution = true;
        Teacher.WeeklyQuota = 40;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadSelectListsAsync();
            return Page();
        }

        // Check if teacher with same FirstName already exists (FirstName is the unique identifier in UNTIS)
        var existingTeacher = await _context.Teachers
            .FirstOrDefaultAsync(t => t.FirstName == Teacher.FirstName);

        if (existingTeacher != null)
        {
            ModelState.AddModelError("Teacher.FirstName", "A teacher with this name already exists.");
            await LoadSelectListsAsync();
            return Page();
        }

        _context.Teachers.Add(Teacher);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Teacher '{Teacher.FullName}' created successfully!";
        return RedirectToPage("./Index");
    }

    private async Task LoadSelectListsAsync()
    {
        var departments = await _context.Departments
            .OrderBy(d => d.Name)
            .ToListAsync();

        DepartmentList = new SelectList(departments, "Id", "Name");
    }
}
