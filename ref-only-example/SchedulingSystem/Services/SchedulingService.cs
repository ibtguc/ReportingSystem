using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;
using SchedulingSystem.Services.Constraints;

namespace SchedulingSystem.Services;

/// <summary>
/// Service for automatic timetable generation using a simple greedy algorithm
/// </summary>
public class SchedulingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SchedulingService> _logger;
    private readonly IConstraintValidator _constraintValidator;

    public SchedulingService(
        ApplicationDbContext context,
        ILogger<SchedulingService> logger,
        IConstraintValidator constraintValidator)
    {
        _context = context;
        _logger = logger;
        _constraintValidator = constraintValidator;
    }

    /// <summary>
    /// Generates a complete timetable for a given school year using a greedy algorithm
    /// </summary>
    /// <param name="existingTimetableId">Optional: If provided, preserves locked lessons from this timetable</param>
    public async Task<SchedulingResult> GenerateTimetableAsync(int schoolYearId, string timetableName, int? existingTimetableId = null)
    {
        var result = new SchedulingResult { Success = false };

        try
        {
            _logger.LogInformation("Starting timetable generation for school year {SchoolYearId}", schoolYearId);

            // Load all required data with UNTIS constraint fields
            var lessons = await _context.Lessons
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
                .Where(l => l.IsActive)
                .AsSplitQuery()
                .ToListAsync();

            var periods = await _context.Periods
                .Where(p => !p.IsBreak)
                .OrderBy(p => p.PeriodNumber)
                .ToListAsync();

            var rooms = await _context.Rooms.ToListAsync();

            // Load availability constraints for all entities
            var teacherAvailabilities = await _context.TeacherAvailabilities.ToListAsync();
            var classAvailabilities = await _context.ClassAvailabilities.ToListAsync();
            var roomAvailabilities = await _context.RoomAvailabilities.ToListAsync();
            var subjectAvailabilities = await _context.SubjectAvailabilities.ToListAsync();

            if (!lessons.Any())
            {
                result.Errors.Add("No active lessons found to schedule");
                return result;
            }

            if (!periods.Any())
            {
                result.Errors.Add("No periods configured. Please configure periods first.");
                return result;
            }

            // Create new timetable
            var timetable = new Timetable
            {
                Name = timetableName,
                SchoolYearId = schoolYearId,
                CreatedDate = DateTime.Now,
                Status = TimetableStatus.Draft
            };

            _context.Timetables.Add(timetable);
            await _context.SaveChangesAsync();

            // Initialize tracking structures
            var scheduledLessons = new List<ScheduledLesson>();

            // Load locked lessons from existing timetable if regenerating
            if (existingTimetableId.HasValue)
            {
                var lockedLessons = await _context.ScheduledLessons
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
                    .Include(sl => sl.ScheduledLessonRooms)
                        .ThenInclude(slr => slr.Room)
                    .Include(sl => sl.ScheduledLessonRooms)
                        .ThenInclude(slr => slr.RoomAssignments)
                            .ThenInclude(ra => ra.LessonAssignment)
                    .Where(sl => sl.TimetableId == existingTimetableId.Value && sl.IsLocked)
                    .AsSplitQuery()
                    .ToListAsync();

                // Copy locked lessons to new timetable
                foreach (var lockedLesson in lockedLessons)
                {
                    var newLockedLesson = new ScheduledLesson
                    {
                        LessonId = lockedLesson.LessonId,
                        Lesson = lockedLesson.Lesson,
                        DayOfWeek = lockedLesson.DayOfWeek,
                        PeriodId = lockedLesson.PeriodId,
                        RoomId = lockedLesson.RoomId,
                        TimetableId = timetable.Id,
                        WeekNumber = lockedLesson.WeekNumber,
                        IsLocked = true // Keep it locked in the new timetable
                    };
                    scheduledLessons.Add(newLockedLesson);
                }

                _logger.LogInformation("Preserved {Count} locked lessons from existing timetable", lockedLessons.Count);
            }

            var lessonsToSchedule = CreateLessonInstances(lessons, scheduledLessons);

            // Sort lessons by constraint priority (greedy heuristic)
            // Prioritize: 1) lessons with specific room requirements, 2) higher frequency
            lessonsToSchedule = lessonsToSchedule
                .OrderByDescending(l => !string.IsNullOrEmpty(l.Lesson.RequiredRoomType))
                .ThenByDescending(l => l.Lesson.FrequencyPerWeek)
                .ToList();

            _logger.LogInformation("Attempting to schedule {Count} lesson instances", lessonsToSchedule.Count);

            // Try to schedule each lesson instance
            int successCount = 0;
            foreach (var lessonInstance in lessonsToSchedule)
            {
                var scheduled = await TryScheduleLessonAsync(
                    lessonInstance,
                    periods,
                    rooms,
                    scheduledLessons,
                    timetable.Id,
                    teacherAvailabilities,
                    classAvailabilities,
                    roomAvailabilities,
                    subjectAvailabilities);

                if (scheduled != null)
                {
                    scheduledLessons.Add(scheduled);
                    successCount++;
                }
                else
                {
                    result.Warnings.Add(
                        $"Could not schedule: {lessonInstance.Lesson.LessonSubjects.FirstOrDefault()?.Subject?.Name} for {lessonInstance.Lesson.LessonClasses.FirstOrDefault()?.Class?.Name} " +
                        $"(Teacher: {lessonInstance.Lesson.LessonTeachers.FirstOrDefault()?.Teacher?.FullName})");
                }
            }

            // Save all scheduled lessons
            _context.ScheduledLessons.AddRange(scheduledLessons);
            await _context.SaveChangesAsync();

            result.Success = true;
            result.TimetableId = timetable.Id;
            result.ScheduledCount = successCount;
            result.TotalCount = lessonsToSchedule.Count;

            _logger.LogInformation(
                "Timetable generation completed. Scheduled {Success}/{Total} lessons",
                successCount,
                lessonsToSchedule.Count);

            if (result.Warnings.Any())
            {
                result.Success = successCount > 0; // Partial success if some lessons scheduled
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating timetable");
            result.Errors.Add($"Error: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Creates individual lesson instances based on frequency per week
    /// E.g., if a lesson has FrequencyPerWeek = 3, create 3 instances to schedule
    /// Excludes instances already satisfied by locked lessons
    /// </summary>
    private List<LessonInstance> CreateLessonInstances(List<Lesson> lessons, List<ScheduledLesson> existingSchedule)
    {
        var instances = new List<LessonInstance>();

        foreach (var lesson in lessons)
        {
            // Count how many instances of this lesson are already locked
            var lockedCount = existingSchedule.Count(sl => sl.LessonId == lesson.Id && sl.IsLocked);

            // Only create instances for the remaining frequency
            var instancesToCreate = Math.Max(0, lesson.FrequencyPerWeek - lockedCount);

            for (int i = 0; i < instancesToCreate; i++)
            {
                instances.Add(new LessonInstance
                {
                    Lesson = lesson,
                    InstanceNumber = lockedCount + i + 1 // Continue numbering after locked instances
                });
            }

            if (lockedCount > 0)
            {
                _logger.LogDebug("Lesson {Subject} for {Class}: {Locked} locked, creating {ToCreate} more instances",
                    lesson.LessonSubjects.FirstOrDefault()?.Subject?.Name, lesson.LessonClasses.FirstOrDefault()?.Class?.Name, lockedCount, instancesToCreate);
            }
        }

        return instances;
    }

    /// <summary>
    /// Attempts to schedule a lesson instance using availability-aware greedy approach
    /// Evaluates all possible time slots, scores them based on soft constraints,
    /// and picks the best valid one
    /// </summary>
    private async Task<ScheduledLesson?> TryScheduleLessonAsync(
        LessonInstance lessonInstance,
        List<Period> periods,
        List<Room> rooms,
        List<ScheduledLesson> existingSchedule,
        int timetableId,
        List<TeacherAvailability> teacherAvailabilities,
        List<ClassAvailability> classAvailabilities,
        List<RoomAvailability> roomAvailabilities,
        List<SubjectAvailability> subjectAvailabilities)
    {
        var lesson = lessonInstance.Lesson;
        var candidateSlots = new List<(DayOfWeek day, Period period, Room? room, int score)>();

        // Try each day of the week (Sunday through Thursday, excluding Friday and Saturday)
        foreach (var day in Enum.GetValues<DayOfWeek>().Where(d => d != DayOfWeek.Friday && d != DayOfWeek.Saturday))
        {
            // Try each period
            foreach (var period in periods)
            {
                // Check if we can schedule at this time slot using centralized validation
                var (canSchedule, assignedRoom) = await CanScheduleAtAsync(
                    lesson, day, period, rooms, existingSchedule, timetableId);

                if (canSchedule)
                {
                    // Calculate soft constraint score for this time slot
                    int score = await CalculateAvailabilityScoreAsync(
                        lesson,
                        day,
                        period,
                        assignedRoom,
                        existingSchedule,
                        timetableId);

                    candidateSlots.Add((day, period, assignedRoom, score));
                }
            }
        }

        // If no valid slots found, return null
        if (!candidateSlots.Any())
        {
            return null;
        }

        // Pick the slot with the highest score (fewest soft constraint violations)
        var bestSlot = candidateSlots.OrderByDescending(s => s.score).First();

        // Create the scheduled lesson
        var scheduledLesson = new ScheduledLesson
        {
            LessonId = lesson.Id,
            Lesson = lesson, // Set navigation property for constraint checking
            DayOfWeek = bestSlot.day,
            PeriodId = bestSlot.period.Id,
            RoomId = bestSlot.room?.Id,
            TimetableId = timetableId,
            WeekNumber = 1 // Single week for now
        };

        return scheduledLesson;
    }

    /// <summary>
    /// Checks if a lesson can be scheduled at a specific day/period using centralized constraint validation
    /// Validates all hard constraints
    /// </summary>
    private async Task<(bool canSchedule, Room? assignedRoom)> CanScheduleAtAsync(
        Lesson lesson,
        DayOfWeek day,
        Period period,
        List<Room> rooms,
        List<ScheduledLesson> existingSchedule,
        int timetableId)
    {
        // Filter rooms by required type (if specified)
        var eligibleRooms = rooms.AsEnumerable();
        if (!string.IsNullOrEmpty(lesson.RequiredRoomType))
        {
            eligibleRooms = eligibleRooms.Where(r => r.RoomType == lesson.RequiredRoomType);
        }

        // Try scheduling with each eligible room (including no room)
        var roomsToTry = eligibleRooms.ToList();
        if (string.IsNullOrEmpty(lesson.RequiredRoomType))
        {
            roomsToTry.Insert(0, null!); // Try without room first if not required
        }

        foreach (var room in roomsToTry)
        {
            // Create temporary scheduled lesson for validation
            var scheduledLesson = new ScheduledLesson
            {
                LessonId = lesson.Id,
                Lesson = lesson,
                DayOfWeek = day,
                PeriodId = period.Id,
                Period = period,
                RoomId = room?.Id,
                TimetableId = timetableId,
                WeekNumber = 1
            };

            // Use centralized constraint validator to check all hard constraints
            var validationResult = await _constraintValidator.ValidateHardConstraintsAsync(
                scheduledLesson,
                existingSchedule);

            // If no hard constraint violations, this is a valid slot
            if (validationResult.IsValid)
            {
                return (true, room);
            }
        }

        // No valid room found
        return (false, null);
    }

    /// <summary>
    /// Calculates a quality score for a specific time slot based on soft constraint violations
    /// Higher score = better time slot (fewer violations and lower severity)
    /// Uses centralized constraint validation
    /// </summary>
    private async Task<int> CalculateAvailabilityScoreAsync(
        Lesson lesson,
        DayOfWeek day,
        Period period,
        Room? assignedRoom,
        List<ScheduledLesson> existingSchedule,
        int timetableId)
    {
        // Create temporary scheduled lesson for validation
        var scheduledLesson = new ScheduledLesson
        {
            LessonId = lesson.Id,
            Lesson = lesson,
            DayOfWeek = day,
            PeriodId = period.Id,
            Period = period,
            RoomId = assignedRoom?.Id,
            TimetableId = timetableId,
            WeekNumber = 1
        };

        // Use centralized constraint validator to check soft constraints
        var validationResult = await _constraintValidator.ValidateSoftConstraintsAsync(
            scheduledLesson,
            existingSchedule);

        // Calculate score based on violations
        // Start with high score (1000) and deduct points for each violation
        int score = 1000;

        // Deduct points for each soft constraint violation
        // Weight by priority: Critical = -100, High = -50, Normal = -20, Low = -10
        foreach (var violation in validationResult.SoftViolations)
        {
            // Deduct more points for higher priority constraints
            var constraintDef = ConstraintDefinitions.GetAllConstraints().FirstOrDefault(
                c => c.Code == violation.ConstraintCode);

            if (constraintDef != null)
            {
                score -= constraintDef.Priority switch
                {
                    ConstraintPriority.Critical => 100,
                    ConstraintPriority.High => 50,
                    ConstraintPriority.Normal => 20,
                    ConstraintPriority.Low => 10,
                    _ => 15
                };
            }
            else
            {
                score -= 15; // Default penalty
            }
        }

        return score;
    }

    /// <summary>
    /// Validates an existing timetable and returns conflicts using centralized constraint validation
    /// </summary>
    public async Task<SchedulingValidationResult> ValidateTimetableAsync(int timetableId)
    {
        // Use centralized constraint validator
        var constraintValidationResult = await _constraintValidator.ValidateTimetableAsync(timetableId);

        // Map to SchedulingValidationResult for backward compatibility
        var result = new SchedulingValidationResult
        {
            IsValid = constraintValidationResult.IsValid
        };

        // Map conflicts
        foreach (var conflict in constraintValidationResult.Conflicts)
        {
            var affectedLessons = await _context.ScheduledLessons
                .Include(sl => sl.Lesson)
                .Include(sl => sl.Period)
                .Where(sl => sl.Id == conflict.ScheduledLessonId)
                .ToListAsync();

            // Determine conflict type from constraint codes
            var conflictType = DetermineConflictType(conflict.ConstraintCodes);

            result.Conflicts.Add(new Conflict
            {
                Type = conflictType,
                Description = string.Join("; ", conflict.Messages),
                AffectedLessons = affectedLessons
            });
        }

        return result;
    }

    /// <summary>
    /// Maps constraint codes to legacy conflict types
    /// </summary>
    private ConflictType DetermineConflictType(List<string> constraintCodes)
    {
        // Check for specific constraint patterns
        if (constraintCodes.Any(c => c == "HC-1" || c == "HC-2"))
            return ConflictType.TeacherDoubleBooking;

        if (constraintCodes.Any(c => c == "HC-3"))
            return ConflictType.ClassDoubleBooking;

        if (constraintCodes.Any(c => c == "HC-7" || c == "HC-8"))
            return ConflictType.RoomDoubleBooking;

        if (constraintCodes.Any(c => c.StartsWith("SC-1") || c.StartsWith("SC-2")))
            return ConflictType.TeacherUnavailable;

        if (constraintCodes.Any(c => c.StartsWith("SC-5")))
            return ConflictType.RoomUnavailable;

        // Default to teacher unavailable for other conflicts
        return ConflictType.TeacherUnavailable;
    }
}

/// <summary>
/// Represents an instance of a lesson to be scheduled
/// </summary>
public class LessonInstance
{
    public Lesson Lesson { get; set; } = null!;
    public int InstanceNumber { get; set; }
}

/// <summary>
/// Result of timetable generation
/// </summary>
public class SchedulingResult
{
    public bool Success { get; set; }
    public int TimetableId { get; set; }
    public int ScheduledCount { get; set; }
    public int TotalCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public QualityMetrics? QualityMetrics { get; set; }
}

/// <summary>
/// Result of timetable validation (scheduling service specific)
/// </summary>
public class SchedulingValidationResult
{
    public bool IsValid { get; set; }
    public List<Conflict> Conflicts { get; set; } = new();
}

/// <summary>
/// Represents a scheduling conflict
/// </summary>
public class Conflict
{
    public ConflictType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<ScheduledLesson> AffectedLessons { get; set; } = new();
}

/// <summary>
/// Types of scheduling conflicts
/// </summary>
public enum ConflictType
{
    TeacherDoubleBooking,
    ClassDoubleBooking,
    RoomDoubleBooking,
    TeacherUnavailable,
    RoomUnavailable
}
