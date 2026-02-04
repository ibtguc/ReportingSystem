using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Classes;

public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DetailsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public Class Class { get; set; } = new();
    public List<ClassAvailability> Availabilities { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var cls = await _context.Classes
            .Include(c => c.ParentClass)
            .Include(c => c.SubClasses)
            .Include(c => c.Students)
            .Include(c => c.LessonClasses)
                .ThenInclude(lc => lc.Lesson)
                    .ThenInclude(l => l.LessonSubjects)
                        .ThenInclude(ls => ls.Subject)
            .Include(c => c.LessonClasses)
                .ThenInclude(lc => lc.Lesson)
                    .ThenInclude(l => l.LessonTeachers)
                        .ThenInclude(lt => lt.Teacher)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (cls == null)
        {
            return NotFound();
        }

        Class = cls;

        // Load availability constraints
        Availabilities = await _context.ClassAvailabilities
            .Include(a => a.Period)
            .Where(a => a.ClassId == id)
            .OrderBy(a => a.DayOfWeek)
            .ThenBy(a => a.Period!.PeriodNumber)
            .ToListAsync();

        return Page();
    }
}
