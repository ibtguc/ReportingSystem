using SchedulingSystem.Data;
using SchedulingSystem.Models;
using SchedulingSystem.Services.Constraints;
using Microsoft.EntityFrameworkCore;

namespace SchedulingSystem.Services;

/// <summary>
/// Service for checking timetable conflicts using centralized constraint validation
/// </summary>
public class TimetableConflictService
{
    private readonly ApplicationDbContext _context;
    private readonly IConstraintValidator _constraintValidator;

    public TimetableConflictService(
        ApplicationDbContext context,
        IConstraintValidator constraintValidator)
    {
        _context = context;
        _constraintValidator = constraintValidator;
    }

    /// <summary>
    /// Check for all conflicts and warnings when scheduling or moving a lesson
    /// </summary>
    public async Task<ConflictCheckResult> CheckConflictsAsync(
        int timetableId,
        int lessonId,
        DayOfWeek dayOfWeek,
        int periodId,
        int? roomId,
        int? excludeScheduledLessonId = null,
        List<string>? ignoredConstraintCodes = null)
    {
        var result = new ConflictCheckResult();

        // Load the lesson with all related data via junction tables
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
            .AsSplitQuery()
            .FirstOrDefaultAsync(l => l.Id == lessonId);

        if (lesson == null)
        {
            result.Errors.Add("Lesson not found");
            return result;
        }

        // Load period info
        var period = await _context.Periods.FindAsync(periodId);
        if (period == null)
        {
            result.Errors.Add("Period not found");
            return result;
        }

        // Get all scheduled lessons for this timetable (excluding the current one if editing)
        var scheduledLessons = await _context.ScheduledLessons
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonSubjects)
                    .ThenInclude(ls => ls.Subject)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonClasses)
                    .ThenInclude(lc => lc.Class)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonTeachers)
                    .ThenInclude(lt => lt.Teacher)
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
            .Where(sl => sl.TimetableId == timetableId)
            .Where(sl => !excludeScheduledLessonId.HasValue || sl.Id != excludeScheduledLessonId.Value)
            .AsSplitQuery()
            .ToListAsync();

        // Create a temporary scheduled lesson for validation
        var scheduledLesson = new ScheduledLesson
        {
            Id = excludeScheduledLessonId ?? 0,
            LessonId = lessonId,
            Lesson = lesson,
            TimetableId = timetableId,
            DayOfWeek = dayOfWeek,
            PeriodId = periodId,
            Period = period,
            RoomId = roomId
        };

        // Create validation context with ignored constraints
        var validationContext = ignoredConstraintCodes != null && ignoredConstraintCodes.Count > 0
            ? new ValidationContext { ConstraintCodesToSkip = ignoredConstraintCodes }
            : null;

        // Use centralized constraint validator
        var validationResult = await _constraintValidator.ValidateAllConstraintsAsync(
            scheduledLesson,
            scheduledLessons,
            validationContext);

        // Map validation results to legacy format
        // HasErrors, HasWarnings, and IsValid are computed properties based on Errors/Warnings collections
        result.Errors = validationResult.GetErrorMessages();
        result.Warnings = validationResult.GetWarningMessages();

        return result;
    }

    /// <summary>
    /// Validate an entire timetable and return all conflicts
    /// </summary>
    public async Task<TimetableValidationResult> ValidateTimetableAsync(int timetableId)
    {
        var result = new TimetableValidationResult();
        var timetable = await _context.Timetables.FindAsync(timetableId);

        if (timetable == null)
        {
            result.Errors.Add("Timetable not found");
            return result;
        }

        var scheduledLessons = await _context.ScheduledLessons
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonSubjects)
                    .ThenInclude(ls => ls.Subject)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonClasses)
                    .ThenInclude(lc => lc.Class)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonTeachers)
                    .ThenInclude(lt => lt.Teacher)
            .Include(sl => sl.Period)
            .Include(sl => sl.Room)
            .Include(sl => sl.ScheduledLessonRooms)
                .ThenInclude(slr => slr.Room)
            .Where(sl => sl.TimetableId == timetableId)
            .ToListAsync();

        // Check each scheduled lesson for conflicts
        foreach (var scheduledLesson in scheduledLessons)
        {
            var checkResult = await CheckConflictsAsync(
                timetableId,
                scheduledLesson.LessonId,
                scheduledLesson.DayOfWeek,
                scheduledLesson.PeriodId,
                scheduledLesson.RoomId,
                scheduledLesson.Id);

            if (checkResult.Errors.Any())
            {
                result.Conflicts.Add(new TimetableConflict
                {
                    ScheduledLessonId = scheduledLesson.Id,
                    Type = ConflictSeverity.Error,
                    Messages = checkResult.Errors
                });
            }

            if (checkResult.Warnings.Any())
            {
                result.Conflicts.Add(new TimetableConflict
                {
                    ScheduledLessonId = scheduledLesson.Id,
                    Type = ConflictSeverity.Warning,
                    Messages = checkResult.Warnings
                });
            }
        }

        result.IsValid = !result.Conflicts.Any(c => c.Type == ConflictSeverity.Error);
        return result;
    }
}

public class ConflictCheckResult
{
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public bool HasErrors => Errors.Any();
    public bool HasWarnings => Warnings.Any();
    public bool IsValid => !HasErrors;
}

public class TimetableValidationResult
{
    public bool IsValid { get; set; }
    public List<TimetableConflict> Conflicts { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class TimetableConflict
{
    public int ScheduledLessonId { get; set; }
    public ConflictSeverity Type { get; set; }
    public List<string> Messages { get; set; } = new();
}

public enum ConflictSeverity
{
    Error,
    Warning
}
