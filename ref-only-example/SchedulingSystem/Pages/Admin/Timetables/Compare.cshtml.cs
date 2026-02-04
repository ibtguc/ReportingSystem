using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Timetables;

public class CompareModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CompareModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Timetable> AllTimetables { get; set; } = new();
    public List<TimetableComparison> Timetables { get; set; } = new();
    public List<Period> Periods { get; set; } = new();
    public List<DayOfWeek> DaysWithDifferences { get; set; } = new();
    public int? TempTimetableId { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync([FromQuery] int[] ids, [FromQuery] int? tempId)
    {
        // Store the temp timetable ID if provided
        TempTimetableId = tempId;

        // Load all timetables for selection
        AllTimetables = await _context.Timetables
            .Include(t => t.SchoolYear)
            .Include(t => t.Term)
            .Include(t => t.ScheduledLessons)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();

        // Load periods
        Periods = await _context.Periods
            .Where(p => !p.IsBreak)
            .OrderBy(p => p.PeriodNumber)
            .ToListAsync();

        // If specific timetable IDs are provided, load them for comparison
        if (ids != null && ids.Length >= 2)
        {
            await LoadTimetablesForComparisonAsync(ids);
        }
        else if (ids != null && ids.Length == 1)
        {
            ErrorMessage = "Please select at least 2 timetables to compare.";
        }
    }

    private async Task LoadTimetablesForComparisonAsync(int[] ids)
    {
        // Load each timetable
        foreach (var id in ids)
        {
            var timetable = await _context.Timetables
                .Include(t => t.SchoolYear)
                .Include(t => t.Term)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (timetable == null)
                continue;

            var scheduledLessons = await _context.ScheduledLessons
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
                .Where(sl => sl.TimetableId == id)
                .AsSplitQuery()
                .ToListAsync();

            Timetables.Add(new TimetableComparison
            {
                Timetable = timetable,
                ScheduledLessons = scheduledLessons.DistinctBy(sl => sl.Id).ToList()
            });
        }

        // Perform XOR join to find only different slots
        if (Timetables.Count >= 2)
        {
            PerformXorJoin();

            // Populate days with differences for use in the view
            DaysWithDifferences = Timetables
                .SelectMany(tt => tt.ScheduledLessons.Select(sl => sl.DayOfWeek))
                .Distinct()
                .OrderBy(d => d)
                .ToList();
        }
    }

    /// <summary>
    /// XOR Join: Find scheduled lessons that differ between timetables
    /// Uses FULL OUTER JOIN logic: a scheduled lesson is different if it exists in one timetable
    /// but not in another at the same (LessonId, PeriodId, DayOfWeek) combination
    /// </summary>
    private void PerformXorJoin()
    {
        // For 2-timetable comparison, use SQL-like FULL OUTER JOIN logic
        if (Timetables.Count == 2)
        {
            var tt1 = Timetables[0];
            var tt2 = Timetables[1];

            // Find lessons in tt1 that don't have a match in tt2 (same lesson, period, day)
            var tt1Differences = tt1.ScheduledLessons
                .Where(sl1 => !tt2.ScheduledLessons.Any(sl2 =>
                    sl2.LessonId == sl1.LessonId &&
                    sl2.PeriodId == sl1.PeriodId &&
                    sl2.DayOfWeek == sl1.DayOfWeek))
                .ToList();

            // Find lessons in tt2 that don't have a match in tt1 (same lesson, period, day)
            var tt2Differences = tt2.ScheduledLessons
                .Where(sl2 => !tt1.ScheduledLessons.Any(sl1 =>
                    sl1.LessonId == sl2.LessonId &&
                    sl1.PeriodId == sl2.PeriodId &&
                    sl1.DayOfWeek == sl2.DayOfWeek))
                .ToList();

            // Update each timetable to only show differences
            tt1.ScheduledLessons = tt1Differences;
            tt2.ScheduledLessons = tt2Differences;
        }
        else if (Timetables.Count > 2)
        {
            // For N-timetable comparison, find lessons that don't exist in ALL other timetables
            for (int i = 0; i < Timetables.Count; i++)
            {
                var currentTT = Timetables[i];
                var otherTTs = Timetables.Where((tt, idx) => idx != i).ToList();

                currentTT.ScheduledLessons = currentTT.ScheduledLessons
                    .Where(sl => otherTTs.Any(otherTT =>
                        !otherTT.ScheduledLessons.Any(otherSl =>
                            otherSl.LessonId == sl.LessonId &&
                            otherSl.PeriodId == sl.PeriodId &&
                            otherSl.DayOfWeek == sl.DayOfWeek)))
                    .ToList();
            }
        }
    }

    /// <summary>
    /// Get merged lessons for a slot from all timetables
    /// Since PerformXorJoin already filtered to only show differences,
    /// this method simply returns all lessons at the given slot
    /// </summary>
    public List<(ScheduledLesson Lesson, int TimetableIndex, string TimetableName)> GetMergedLessonsForSlot(DayOfWeek day, int periodId)
    {
        var mergedLessons = new List<(ScheduledLesson, int, string)>();

        // Get lessons from each timetable for this slot
        // ScheduledLessons are already filtered to show only differences by PerformXorJoin
        for (int i = 0; i < Timetables.Count; i++)
        {
            var lessons = Timetables[i].ScheduledLessons
                .Where(sl => sl.DayOfWeek == day && sl.PeriodId == periodId)
                .DistinctBy(sl => sl.Id)
                .ToList();

            foreach (var lesson in lessons)
            {
                mergedLessons.Add((lesson, i, Timetables[i].Timetable.Name));
            }
        }

        // Sort by LessonId for consistent display
        return mergedLessons.OrderBy(ml => ml.Item1.LessonId).ToList();
    }

    /// <summary>
    /// Get all unique days that have differences (returns the cached property)
    /// </summary>
    public List<DayOfWeek> GetDaysWithDifferences()
    {
        return DaysWithDifferences;
    }

    /// <summary>
    /// Get all unique periods that have differences
    /// </summary>
    public async Task<List<Period>> GetPeriodsWithDifferencesAsync()
    {
        if (Timetables.Count < 2) return new List<Period>();

        var periodIds = Timetables
            .SelectMany(tt => tt.ScheduledLessons.Select(sl => sl.PeriodId))
            .Distinct()
            .ToList();

        return await _context.Periods
            .Where(p => periodIds.Contains(p.Id))
            .OrderBy(p => p.PeriodNumber)
            .ToListAsync();
    }

    public List<ScheduledLesson> GetLessonsForSlot(TimetableComparison comparison, DayOfWeek day, int periodId)
    {
        return comparison.ScheduledLessons
            .Where(sl => sl.DayOfWeek == day && sl.PeriodId == periodId)
            .DistinctBy(sl => sl.Id)
            .ToList();
    }

    public string GetLessonDescription(ScheduledLesson lesson)
    {
        var subjects = lesson.Lesson?.LessonSubjects.Select(ls => ls.Subject?.Code ?? ls.Subject?.Name ?? "N/A") ?? Array.Empty<string>();
        var classes = lesson.Lesson?.LessonClasses.Select(lc => lc.Class?.Name ?? "N/A") ?? Array.Empty<string>();
        var teachers = lesson.Lesson?.LessonTeachers.Select(lt => lt.Teacher?.ShortName ?? lt.Teacher?.FullName ?? "N/A") ?? Array.Empty<string>();

        var subjectStr = string.Join(", ", subjects);
        var classStr = string.Join(", ", classes);
        var teacherStr = string.Join(", ", teachers);

        return $"{subjectStr} | {classStr} | {teacherStr}";
    }

    public string GetRoomDescription(ScheduledLesson lesson)
    {
        // Check multi-room first
        if (lesson.ScheduledLessonRooms.Any())
        {
            var rooms = lesson.ScheduledLessonRooms.Select(slr => slr.Room?.RoomNumber ?? "?");
            return string.Join(", ", rooms);
        }

        // Fallback to legacy single room
        return lesson.Room?.RoomNumber ?? "—";
    }

    public ComparisonStats GetComparisonStats()
    {
        if (Timetables.Count < 2)
            return new ComparisonStats();

        var stats = new ComparisonStats
        {
            TotalTimetables = Timetables.Count
        };

        // Calculate lessons per timetable
        foreach (var tt in Timetables)
        {
            stats.LessonsPerTimetable.Add(tt.Timetable.Name, tt.ScheduledLessons.Count);
        }

        // Find common and unique lessons
        var firstTimetable = Timetables[0];
        var otherTimetables = Timetables.Skip(1).ToList();

        // Group lessons by LessonId for comparison
        var firstLessonIds = firstTimetable.ScheduledLessons.Select(sl => sl.LessonId).Distinct().ToHashSet();

        foreach (var tt in otherTimetables)
        {
            var lessonIds = tt.ScheduledLessons.Select(sl => sl.LessonId).Distinct().ToHashSet();
            stats.CommonLessons += firstLessonIds.Intersect(lessonIds).Count();
        }

        // Calculate average
        if (otherTimetables.Count > 0)
        {
            stats.CommonLessons = stats.CommonLessons / otherTimetables.Count;
        }

        return stats;
    }

    public bool AreSlotsDifferent(DayOfWeek day, int periodId)
    {
        if (Timetables.Count < 2)
            return false;

        var slotLessons = Timetables
            .Select(tt => GetLessonsForSlot(tt, day, periodId))
            .ToList();

        // Compare lesson IDs in each slot
        var firstSlot = slotLessons[0].Select(l => l.LessonId).OrderBy(x => x).ToList();

        for (int i = 1; i < slotLessons.Count; i++)
        {
            var currentSlot = slotLessons[i].Select(l => l.LessonId).OrderBy(x => x).ToList();

            if (!firstSlot.SequenceEqual(currentSlot))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Delete temporary timetable and close the comparison
    /// </summary>
    public async Task<IActionResult> OnPostDeleteTempAsync(int tempId)
    {
        try
        {
            var timetable = await _context.Timetables
                .Include(t => t.ScheduledLessons)
                .FirstOrDefaultAsync(t => t.Id == tempId);

            if (timetable == null)
            {
                return new JsonResult(new { success = false, error = "Timetable not found" });
            }

            // Delete all scheduled lessons first
            _context.ScheduledLessons.RemoveRange(timetable.ScheduledLessons);

            // Delete the timetable
            _context.Timetables.Remove(timetable);

            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, message = "Temporary timetable deleted successfully" });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }
}

public class TimetableComparison
{
    public Timetable Timetable { get; set; } = default!;
    public List<ScheduledLesson> ScheduledLessons { get; set; } = new();
}

public class ComparisonStats
{
    public int TotalTimetables { get; set; }
    public Dictionary<string, int> LessonsPerTimetable { get; set; } = new();
    public int CommonLessons { get; set; }
}
