using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;
using SchedulingSystem.Services;
using SchedulingSystem.Services.Constraints;

namespace SchedulingSystem.Pages.Admin.Timetables;

public class ValidateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly SchedulingService _schedulingService;

    public ValidateModel(ApplicationDbContext context, SchedulingService schedulingService)
    {
        _context = context;
        _schedulingService = schedulingService;
    }

    public int TimetableId { get; set; }
    public Timetable? Timetable { get; set; }
    public SchedulingValidationResult? ValidationResult { get; set; }
    public TimetableStatistics Statistics { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        TimetableId = id;

        Timetable = await _context.Timetables
            .Include(t => t.SchoolYear)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (Timetable == null)
        {
            return NotFound();
        }

        // Run validation
        ValidationResult = await _schedulingService.ValidateTimetableAsync(id);

        // Calculate statistics
        var scheduledLessons = await _context.ScheduledLessons
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonTeachers)
                    .ThenInclude(lt => lt.Teacher)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonClasses)
                    .ThenInclude(lc => lc.Class)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonSubjects)
                    .ThenInclude(ls => ls.Subject)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonAssignments)
                    .ThenInclude(la => la.Teacher)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonAssignments)
                    .ThenInclude(la => la.Subject)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonAssignments)
                    .ThenInclude(la => la.Class)
            .Include(sl => sl.Room)
            .Include(sl => sl.ScheduledLessonRooms)
                .ThenInclude(slr => slr.Room)
            .Include(sl => sl.ScheduledLessonRooms)
                .ThenInclude(slr => slr.RoomAssignments)
                    .ThenInclude(ra => ra.LessonAssignment)
            .Where(sl => sl.TimetableId == id)
            .AsSplitQuery()
            .ToListAsync();

        Statistics.TotalLessons = scheduledLessons.Count;
        Statistics.UniqueTeachers = scheduledLessons
            .SelectMany(sl => sl.Lesson!.LessonTeachers.Select(lt => lt.TeacherId))
            .Distinct()
            .Count();
        Statistics.UniqueClasses = scheduledLessons
            .SelectMany(sl => sl.Lesson!.LessonClasses.Select(lc => lc.ClassId))
            .Distinct()
            .Count();

        // Count unique rooms from both legacy RoomId and new ScheduledLessonRooms
        Statistics.UniqueRooms = scheduledLessons
            .SelectMany(sl => new[] { sl.RoomId }
                .Concat(sl.ScheduledLessonRooms.Select(slr => (int?)slr.RoomId)))
            .Where(roomId => roomId.HasValue)
            .Distinct()
            .Count();

        return Page();
    }
}

public class TimetableStatistics
{
    public int TotalLessons { get; set; }
    public int UniqueTeachers { get; set; }
    public int UniqueClasses { get; set; }
    public int UniqueRooms { get; set; }
}

