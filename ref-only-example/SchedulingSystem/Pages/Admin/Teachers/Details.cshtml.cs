using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Teachers;

public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DetailsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public Teacher Teacher { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var teacher = await _context.Teachers
            .Include(t => t.LessonTeachers)
                .ThenInclude(lt => lt.Lesson)
                    .ThenInclude(l => l.LessonSubjects)
                        .ThenInclude(ls => ls.Subject)
            .Include(t => t.LessonTeachers)
                .ThenInclude(lt => lt.Lesson)
                    .ThenInclude(l => l.LessonClasses)
                        .ThenInclude(lc => lc.Class)
            .Include(t => t.TeacherSubjects)
                .ThenInclude(ts => ts.Subject)
            .Include(t => t.Availabilities)
                .ThenInclude(a => a.Period)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (teacher == null)
        {
            return NotFound();
        }

        Teacher = teacher;
        return Page();
    }
}
