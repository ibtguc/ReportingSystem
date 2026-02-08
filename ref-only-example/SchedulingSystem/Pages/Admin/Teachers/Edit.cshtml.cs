using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Teachers;

public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Teacher Teacher { get; set; } = new();

    public SelectList DepartmentList { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var teacher = await _context.Teachers.FindAsync(id);

        if (teacher == null)
        {
            return NotFound();
        }

        Teacher = teacher;
        await LoadSelectListsAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadSelectListsAsync();
            return Page();
        }

        // Check if another teacher with same FirstName exists
        var existingTeacher = await _context.Teachers
            .FirstOrDefaultAsync(t => t.FirstName == Teacher.FirstName && t.Id != Teacher.Id);

        if (existingTeacher != null)
        {
            ModelState.AddModelError("Teacher.FirstName", "A teacher with this name already exists.");
            await LoadSelectListsAsync();
            return Page();
        }

        _context.Attach(Teacher).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!TeacherExists(Teacher.Id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        TempData["SuccessMessage"] = $"Teacher '{Teacher.FullName}' updated successfully!";
        return RedirectToPage("./Index");
    }

    private bool TeacherExists(int id)
    {
        return _context.Teachers.Any(e => e.Id == id);
    }

    private async Task LoadSelectListsAsync()
    {
        var departments = await _context.Departments
            .OrderBy(d => d.Name)
            .ToListAsync();

        DepartmentList = new SelectList(departments, "Id", "Name");
    }
}
