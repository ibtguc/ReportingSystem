using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Departments;

public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Department Department { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var department = await _context.Departments.FindAsync(id);
        if (department == null)
        {
            return NotFound();
        }

        Department = department;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Check for duplicate department code (excluding current department)
        var existingDept = await _context.Departments
            .FirstOrDefaultAsync(d => d.Name == Department.Name && d.Id != Department.Id);

        if (existingDept != null)
        {
            ModelState.AddModelError("Department.Name", "A department with this code already exists.");
            return Page();
        }

        _context.Attach(Department).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await DepartmentExists(Department.Id))
            {
                return NotFound();
            }
            throw;
        }

        return RedirectToPage("./Index");
    }

    private async Task<bool> DepartmentExists(int id)
    {
        return await _context.Departments.AnyAsync(e => e.Id == id);
    }
}
