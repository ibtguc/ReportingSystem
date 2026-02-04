using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Lessons
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Lesson Lesson { get; set; } = default!;
        public int ScheduledLessonsCount { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lesson = await _context.Lessons
                .Include(l => l.LessonSubjects)
                    .ThenInclude(ls => ls.Subject)
                .Include(l => l.LessonClasses)
                    .ThenInclude(lc => lc.Class)
                .Include(l => l.LessonTeachers)
                    .ThenInclude(lt => lt.Teacher)
                .Include(l => l.LessonAssignments)
                    .ThenInclude(la => la.Teacher)
                .Include(l => l.LessonAssignments)
                    .ThenInclude(la => la.Subject)
                .Include(l => l.LessonAssignments)
                    .ThenInclude(la => la.Class)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (lesson == null)
            {
                return NotFound();
            }

            Lesson = lesson;

            // Count scheduled instances
            ScheduledLessonsCount = await _context.ScheduledLessons
                .Where(sl => sl.LessonId == id)
                .CountAsync();

            return Page();
        }
    }
}
