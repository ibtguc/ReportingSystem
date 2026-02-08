using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Subjects;

public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DetailsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public Subject Subject { get; set; } = new();
    public List<SubjectAvailability> Availabilities { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var subject = await _context.Subjects
            .Include(s => s.LessonSubjects)
                .ThenInclude(ls => ls.Lesson)
                    .ThenInclude(l => l.LessonClasses)
                        .ThenInclude(lc => lc.Class)
            .Include(s => s.LessonSubjects)
                .ThenInclude(ls => ls.Lesson)
                    .ThenInclude(l => l.LessonTeachers)
                        .ThenInclude(lt => lt.Teacher)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (subject == null)
        {
            return NotFound();
        }

        Subject = subject;

        // Load availability constraints
        Availabilities = await _context.SubjectAvailabilities
            .Include(a => a.Period)
            .Where(a => a.SubjectId == id)
            .OrderBy(a => a.DayOfWeek)
            .ThenBy(a => a.Period!.PeriodNumber)
            .ToListAsync();

        return Page();
    }
}
