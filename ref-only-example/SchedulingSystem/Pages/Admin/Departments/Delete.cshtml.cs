using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Departments;

public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DeleteModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Department Department { get; set; } = null!;

    public int TeacherCount { get; set; }
    public int SubjectCount { get; set; }
    public bool HasRelatedData => TeacherCount > 0 || SubjectCount > 0;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var department = await _context.Departments
            .Include(d => d.Teachers)
            .Include(d => d.Subjects)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (department == null)
        {
            return NotFound();
        }

        Department = department;
        TeacherCount = department.Teachers.Count;
        SubjectCount = department.Subjects.Count;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var department = await _context.Departments
            .Include(d => d.Teachers)
            .Include(d => d.Subjects)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department != null)
        {
            // Clear relationships before deleting
            foreach (var teacher in department.Teachers.ToList())
            {
                teacher.DepartmentId = null;
            }
            foreach (var subject in department.Subjects.ToList())
            {
                subject.DepartmentId = null;
            }

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
