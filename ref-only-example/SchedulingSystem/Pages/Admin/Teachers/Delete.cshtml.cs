using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Teachers;

public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DeleteModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Teacher Teacher { get; set; } = new();

    public int LessonCount { get; set; }
    public int AvailabilityCount { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var teacher = await _context.Teachers
            .Include(t => t.Department)
            .Include(t => t.LessonTeachers)
            .Include(t => t.Availabilities)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (teacher == null)
        {
            return NotFound();
        }

        Teacher = teacher;
        LessonCount = teacher.LessonTeachers.Count;
        AvailabilityCount = teacher.Availabilities.Count;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var teacher = await _context.Teachers
            .Include(t => t.LessonTeachers)
            .Include(t => t.Availabilities)
            .Include(t => t.TeacherSubjects)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (teacher != null)
        {
            // Remove related entities first
            _context.LessonTeachers.RemoveRange(teacher.LessonTeachers);
            _context.TeacherAvailabilities.RemoveRange(teacher.Availabilities);
            _context.TeacherSubjects.RemoveRange(teacher.TeacherSubjects);

            // Remove the teacher
            _context.Teachers.Remove(teacher);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Teacher '{teacher.FullName}' deleted successfully!";
        }

        return RedirectToPage("./Index");
    }
}
