using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Timetables;

public class PrintModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public PrintModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public Timetable? Timetable { get; set; }
    public List<Period> Periods { get; set; } = new();
    public List<ScheduledLesson> ScheduledLessons { get; set; } = new();

    // Print mode: null = single with filters, "all-teachers", "all-subjects", "all-classes"
    public string? PrintMode { get; set; }

    // For filter dropdowns (single mode)
    public List<Teacher> FilterTeachers { get; set; } = new();
    public List<Subject> FilterSubjects { get; set; } = new();
    public List<Class> FilterClasses { get; set; } = new();
    public List<Room> FilterRooms { get; set; } = new();

    // For "print all" modes - sorted alphabetically
    public List<Teacher> AllTeachers { get; set; } = new();
    public List<Subject> AllSubjects { get; set; } = new();
    public List<Class> AllClasses { get; set; } = new();

    // Break Supervision data for display
    public List<BreakSupervisionDuty> BreakSupervisionDuties { get; set; } = new();
    public List<int> SupervisionPeriods { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id, string? mode)
    {
        if (!id.HasValue)
        {
            return RedirectToPage("./Edit");
        }

        PrintMode = mode;

        // Load timetable
        Timetable = await _context.Timetables
            .Include(t => t.SchoolYear)
            .Include(t => t.Term)
            .FirstOrDefaultAsync(t => t.Id == id.Value);

        if (Timetable == null)
        {
            return NotFound();
        }

        // Load periods (excluding breaks)
        Periods = await _context.Periods
            .Where(p => !p.IsBreak)
            .OrderBy(p => p.PeriodNumber)
            .ToListAsync();

        // Load all scheduled lessons for this timetable with all related data
        var scheduledLessonsList = await _context.ScheduledLessons
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonSubjects)
                    .ThenInclude(ls => ls.Subject)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonTeachers)
                    .ThenInclude(lt => lt.Teacher)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonClasses)
                    .ThenInclude(lc => lc.Class)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonAssignments)
                    .ThenInclude(la => la.Teacher)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonAssignments)
                    .ThenInclude(la => la.Subject)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonAssignments)
                    .ThenInclude(la => la.Class)
            .Include(sl => sl.Period)
            .Include(sl => sl.Room)
            .Include(sl => sl.ScheduledLessonRooms)
                .ThenInclude(slr => slr.Room)
            .Include(sl => sl.ScheduledLessonRooms)
                .ThenInclude(slr => slr.RoomAssignments)
                    .ThenInclude(ra => ra.LessonAssignment)
                        .ThenInclude(la => la!.Teacher)
            .Include(sl => sl.ScheduledLessonRooms)
                .ThenInclude(slr => slr.RoomAssignments)
                    .ThenInclude(ra => ra.LessonAssignment)
                        .ThenInclude(la => la!.Subject)
            .Include(sl => sl.ScheduledLessonRooms)
                .ThenInclude(slr => slr.RoomAssignments)
                    .ThenInclude(ra => ra.LessonAssignment)
                        .ThenInclude(la => la!.Class)
            .Where(sl => sl.TimetableId == id.Value)
            .AsSplitQuery()
            .ToListAsync();

        // Deduplicate by ScheduledLesson.Id
        ScheduledLessons = scheduledLessonsList
            .DistinctBy(sl => sl.Id)
            .ToList();

        // Build filter lists from scheduled lessons
        FilterTeachers = ScheduledLessons
            .SelectMany(sl => sl.Lesson.LessonTeachers.Select(lt => lt.Teacher))
            .Where(t => t != null)
            .Distinct()
            .OrderBy(t => t!.FullName)
            .ToList()!;

        FilterSubjects = ScheduledLessons
            .SelectMany(sl => sl.Lesson.LessonSubjects.Select(ls => ls.Subject))
            .Where(s => s != null)
            .Distinct()
            .OrderBy(s => s!.Name)
            .ToList()!;

        FilterClasses = ScheduledLessons
            .SelectMany(sl => sl.Lesson.LessonClasses.Select(lc => lc.Class))
            .Where(c => c != null)
            .Distinct()
            .OrderBy(c => c!.Name)
            .ToList()!;

        FilterRooms = ScheduledLessons
            .Where(sl => sl.Room != null)
            .Select(sl => sl.Room!)
            .Concat(ScheduledLessons.SelectMany(sl => sl.ScheduledLessonRooms.Select(slr => slr.Room!)))
            .Where(r => r != null)
            .Distinct()
            .OrderBy(r => r.RoomNumber)
            .ToList();

        // For "print all" modes, prepare sorted lists
        if (mode == "all-teachers")
        {
            AllTeachers = FilterTeachers.OrderBy(t => t.FullName).ToList();
        }
        else if (mode == "all-subjects")
        {
            AllSubjects = FilterSubjects.OrderBy(s => s.Name).ToList();
        }
        else if (mode == "all-classes")
        {
            AllClasses = FilterClasses.OrderBy(c => c.Name).ToList();
        }

        // Load break supervision data for this timetable
        BreakSupervisionDuties = await _context.BreakSupervisionDuties
            .Include(d => d.Room)
            .Include(d => d.Teacher)
            .Where(d => d.IsActive && d.TimetableId == id.Value)
            .OrderBy(d => d.Room!.RoomNumber)
            .ThenBy(d => d.DayOfWeek)
            .ThenBy(d => d.PeriodNumber)
            .ToListAsync();

        SupervisionPeriods = BreakSupervisionDuties
            .Select(d => d.PeriodNumber)
            .Distinct()
            .OrderBy(p => p)
            .ToList();

        return Page();
    }

    public IEnumerable<ScheduledLesson> GetLessonsForSlot(DayOfWeek day, int periodId)
    {
        return ScheduledLessons.Where(sl => sl.DayOfWeek == day && sl.PeriodId == periodId);
    }

    public IEnumerable<ScheduledLesson> GetLessonsForSlotByTeacher(DayOfWeek day, int periodId, int teacherId)
    {
        return ScheduledLessons.Where(sl =>
            sl.DayOfWeek == day &&
            sl.PeriodId == periodId &&
            sl.Lesson.LessonTeachers.Any(lt => lt.TeacherId == teacherId));
    }

    public IEnumerable<ScheduledLesson> GetLessonsForSlotBySubject(DayOfWeek day, int periodId, int subjectId)
    {
        return ScheduledLessons.Where(sl =>
            sl.DayOfWeek == day &&
            sl.PeriodId == periodId &&
            sl.Lesson.LessonSubjects.Any(ls => ls.SubjectId == subjectId));
    }

    public IEnumerable<ScheduledLesson> GetLessonsForSlotByClass(DayOfWeek day, int periodId, int classId)
    {
        return ScheduledLessons.Where(sl =>
            sl.DayOfWeek == day &&
            sl.PeriodId == periodId &&
            sl.Lesson.LessonClasses.Any(lc => lc.ClassId == classId));
    }

    // Helper method to get break supervision duties for a specific day and period
    public List<BreakSupervisionDuty> GetSupervisionForSlot(DayOfWeek day, int periodNumber)
    {
        return BreakSupervisionDuties
            .Where(d => d.DayOfWeek == day && d.PeriodNumber == periodNumber)
            .OrderBy(d => d.Room?.RoomNumber)
            .ToList();
    }

    // Get supervision duty for a specific teacher on a day/period
    public BreakSupervisionDuty? GetSupervisionForTeacher(DayOfWeek day, int periodNumber, int teacherId)
    {
        return BreakSupervisionDuties
            .FirstOrDefault(d => d.DayOfWeek == day && d.PeriodNumber == periodNumber && d.TeacherId == teacherId);
    }

    // Get the supervision period number that should appear before a given period (after the previous period)
    public int? GetSupervisionPeriodBefore(int periodNumber)
    {
        return SupervisionPeriods.Contains(periodNumber) ? periodNumber : null;
    }

    // Convert supervision period number to "Break 1", "Break 2", etc.
    public string GetBreakLabel(int periodNumber)
    {
        var index = SupervisionPeriods.IndexOf(periodNumber);
        return index >= 0 ? $"Break {index + 1}" : $"Break";
    }
}
