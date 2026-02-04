using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;
using SchedulingSystem.Services.Constraints;

namespace SchedulingSystem.Services;

/// <summary>
/// Enhanced service for automatic timetable generation with soft constraint optimization
/// </summary>
public class SchedulingServiceEnhanced
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SchedulingServiceEnhanced> _logger;
    private readonly IConstraintValidator _constraintValidator;
    private SoftConstraintWeights _weights;

    public SchedulingServiceEnhanced(
        ApplicationDbContext context,
        ILogger<SchedulingServiceEnhanced> logger,
        IConstraintValidator constraintValidator)
    {
        _context = context;
        _logger = logger;
        _constraintValidator = constraintValidator;
        _weights = SoftConstraintWeights.Default;
    }

    /// <summary>
    /// Generates a complete timetable with soft constraint optimization
    /// </summary>
    public async Task<SchedulingResult> GenerateTimetableAsync(
        int schoolYearId,
        string timetableName,
        SoftConstraintWeights? weights = null)
    {
        _weights = weights ?? SoftConstraintWeights.Default;
        var result = new SchedulingResult { Success = false };

        try
        {
            _logger.LogInformation("Starting enhanced timetable generation for school year {SchoolYearId}", schoolYearId);

            // Load all required data
            var lessons = await _context.Lessons
                .Include(l => l.LessonSubjects)
                    .ThenInclude(ls => ls.Subject)
                .Include(l => l.LessonClasses)
                    .ThenInclude(lc => lc.Class)
                .Include(l => l.LessonTeachers)
                    .ThenInclude(lt => lt.Teacher)
                .Where(l => l.IsActive)
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
            var lessonsToSchedule = CreateLessonInstances(lessons);

            // Sort lessons by priority (most constrained first)
            lessonsToSchedule = PrioritizeLessons(lessonsToSchedule);

            _logger.LogInformation("Attempting to schedule {Count} lesson instances with soft constraints",
                lessonsToSchedule.Count);

            // Try to schedule each lesson instance using scoring
            int successCount = 0;
            foreach (var lessonInstance in lessonsToSchedule)
            {
                var scheduled = await TryScheduleLessonWithScoringAsync(
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

            // Calculate quality metrics
            var metrics = CalculateQualityMetrics(scheduledLessons, lessons);
            result.QualityMetrics = metrics;

            result.Success = true;
            result.TimetableId = timetable.Id;
            result.ScheduledCount = successCount;
            result.TotalCount = lessonsToSchedule.Count;

            _logger.LogInformation(
                "Enhanced timetable generation completed. Scheduled {Success}/{Total} lessons. Quality Score: {Score}",
                successCount,
                lessonsToSchedule.Count,
                metrics.OverallScore);

            if (result.Warnings.Any())
            {
                result.Success = successCount > 0;
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
    /// Prioritize lessons by constraints (most constrained first)
    /// </summary>
    private List<LessonInstance> PrioritizeLessons(List<LessonInstance> lessons)
    {
        return lessons
            .OrderByDescending(l => !string.IsNullOrEmpty(l.Lesson.RequiredRoomType))  // Specific room needs
            .ThenByDescending(l => l.Lesson.FrequencyPerWeek)  // Higher frequency
            .ThenByDescending(l => l.Lesson.Duration)  // Longer duration
            .ToList();
    }

    /// <summary>
    /// Creates individual lesson instances based on frequency per week
    /// </summary>
    private List<LessonInstance> CreateLessonInstances(List<Lesson> lessons)
    {
        var instances = new List<LessonInstance>();

        foreach (var lesson in lessons)
        {
            for (int i = 0; i < lesson.FrequencyPerWeek; i++)
            {
                instances.Add(new LessonInstance
                {
                    Lesson = lesson,
                    InstanceNumber = i + 1
                });
            }
        }

        return instances;
    }

    /// <summary>
    /// Attempts to schedule a lesson using soft constraint scoring
    /// </summary>
#pragma warning disable CS1998 // Async method lacks 'await' operators - designed for future async operations
    private async Task<ScheduledLesson?> TryScheduleLessonWithScoringAsync(
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
        var candidates = new List<TimeSlotCandidate>();

        // Evaluate all possible time slots (Sunday through Thursday, excluding Friday and Saturday)
        foreach (var day in Enum.GetValues<DayOfWeek>().Where(d => d != DayOfWeek.Friday && d != DayOfWeek.Saturday))
        {
            foreach (var period in periods)
            {
                // Check hard constraints first using centralized validation
                var (canSchedule, assignedRoom) = await CanScheduleAtAsync(
                    lesson, day, period, rooms, existingSchedule, timetableId);

                if (canSchedule)
                {
                    // Calculate soft constraint score using centralized validation
                    var score = await CalculateSoftConstraintScoreAsync(
                        lesson,
                        lessonInstance.InstanceNumber,
                        day,
                        period,
                        assignedRoom,
                        existingSchedule,
                        timetableId);

                    candidates.Add(score);
                }
            }
        }

        if (!candidates.Any())
        {
            return null; // No valid slots
        }

        // Pick the slot with highest score
        var bestCandidate = candidates.OrderByDescending(c => c.Score).First();

        _logger.LogDebug(
            "Scheduled {Subject} for {Class} at {Day} Period {Period} (Score: {Score}: {Reasons})",
            lesson.LessonSubjects.FirstOrDefault()?.Subject?.Name,
            lesson.LessonClasses.FirstOrDefault()?.Class?.Name,
            bestCandidate.Day,
            bestCandidate.PeriodId,
            bestCandidate.Score,
            string.Join(", ", bestCandidate.ScoreReasons));

        return new ScheduledLesson
        {
            LessonId = lesson.Id,
            Lesson = lesson, // Set navigation property for constraint checking
            DayOfWeek = bestCandidate.Day,
            PeriodId = bestCandidate.PeriodId,
            RoomId = bestCandidate.RoomId,
            TimetableId = timetableId,
            WeekNumber = 1
        };
    }

    /// <summary>
    /// Calculate soft constraint score for a time slot (including availability constraints)
    /// </summary>
    /// <summary>
    /// Calculates soft constraint score using centralized validation
    /// </summary>
    private async Task<TimeSlotCandidate> CalculateSoftConstraintScoreAsync(
        Lesson lesson,
        int instanceNumber,
        DayOfWeek day,
        Period period,
        Room? room,
        List<ScheduledLesson> existingSchedule,
        int timetableId)
    {
        var candidate = new TimeSlotCandidate
        {
            Day = day,
            PeriodId = period.Id,
            RoomId = room?.Id,
            Score = 0
        };

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

        // Use centralized validator to check soft constraints
        var validationResult = await _constraintValidator.ValidateSoftConstraintsAsync(
            scheduledLesson,
            existingSchedule);

        // Start with base score
        int score = 1000;

        // Deduct points for each soft constraint violation (weighted by priority)
        foreach (var violation in validationResult.SoftViolations)
        {
            var constraintDef = ConstraintDefinitions.GetAllConstraints().FirstOrDefault(
                c => c.Code == violation.ConstraintCode);

            int penalty = constraintDef?.Priority switch
            {
                ConstraintPriority.Critical => 100,
                ConstraintPriority.High => 50,
                ConstraintPriority.Normal => 20,
                ConstraintPriority.Low => 10,
                _ => 15
            };

            score -= penalty;
            candidate.ScoreReasons.Add($"{violation.ConstraintName} (-{penalty})");
        }

        candidate.Score = score;
        return candidate;
    }

    /// <summary>
    /// Legacy soft constraint scoring method - kept for reference, replaced by centralized validation
    /// </summary>
    [Obsolete("Use CalculateSoftConstraintScoreAsync instead")]
    private TimeSlotCandidate CalculateSoftConstraintScore_Legacy(
        Lesson lesson,
        int instanceNumber,
        DayOfWeek day,
        Period period,
        Room? room,
        List<ScheduledLesson> existingSchedule,
        List<TeacherAvailability> teacherAvailabilities,
        List<ClassAvailability> classAvailabilities,
        List<RoomAvailability> roomAvailabilities,
        List<SubjectAvailability> subjectAvailabilities)
    {
        var candidate = new TimeSlotCandidate
        {
            Day = day,
            PeriodId = period.Id,
            RoomId = room?.Id,
            Score = 0
        };

        // AVAILABILITY CONSTRAINTS - Add availability score (weighted)
        int availabilityScore = CalculateAvailabilityScore(
            lesson,
            day,
            period.Id,
            room,
            teacherAvailabilities,
            classAvailabilities,
            roomAvailabilities,
            subjectAvailabilities);

        // Weight availability constraints (each importance point worth 10 points in final score)
        candidate.Score += availabilityScore * _weights.AvailabilityWeight;
        if (availabilityScore != 0)
        {
            candidate.ScoreReasons.Add($"Availability: {availabilityScore * _weights.AvailabilityWeight:+#;-#;0}");
        }

        if (!_weights.Enabled)
        {
            candidate.Score = 100; // Default score when soft constraints disabled
            return candidate;
        }

        // 1. Minimize teacher NTPs
        var primaryTeacherId = lesson.LessonTeachers.FirstOrDefault()?.TeacherId;
        if (primaryTeacherId.HasValue && !CreatesTeacherNTP(primaryTeacherId.Value, day, period.Id, existingSchedule))
        {
            candidate.Score += _weights.MinimizeTeacherNTPs;
            candidate.ScoreReasons.Add($"No teacher gap (+{_weights.MinimizeTeacherNTPs})");
        }

        // 2. Minimize student NTPs
        var classId = lesson.LessonClasses.FirstOrDefault()?.ClassId;
        if (classId.HasValue && !CreatesStudentNTP(classId.Value, day, period.Id, existingSchedule))
        {
            candidate.Score += _weights.MinimizeStudentNTPs;
            candidate.ScoreReasons.Add($"No student gap (+{_weights.MinimizeStudentNTPs})");
        }

        // 3. Even distribution across week
        if (IsEvenlyDistributed(lesson, instanceNumber, day, existingSchedule))
        {
            candidate.Score += _weights.EvenDistribution;
            candidate.ScoreReasons.Add($"Even distribution (+{_weights.EvenDistribution})");
        }

        // 4. Preferred time slot
        var subjectCategory = lesson.LessonSubjects.FirstOrDefault()?.Subject?.Category ?? "";
        var preferredTime = SubjectPreferences.GetPreferredTime(subjectCategory);
        if (SubjectPreferences.IsPreferredPeriod(period.PeriodNumber, preferredTime))
        {
            candidate.Score += _weights.PreferredTimeSlot;
            candidate.ScoreReasons.Add($"Preferred time (+{_weights.PreferredTimeSlot})");
        }

        // 5. Minimize room changes
        if (room != null && primaryTeacherId.HasValue && SameRoomAsPrevious(primaryTeacherId.Value, day, period, room.Id, existingSchedule))
        {
            candidate.Score += _weights.MinimizeRoomChanges;
            candidate.ScoreReasons.Add($"Same room (+{_weights.MinimizeRoomChanges})");
        }

        // 6. Balanced workload
        if (primaryTeacherId.HasValue && IsBalancedWorkload(primaryTeacherId.Value, day, existingSchedule))
        {
            candidate.Score += _weights.BalancedWorkload;
            candidate.ScoreReasons.Add($"Balanced day (+{_weights.BalancedWorkload})");
        }

        // 7. Block scheduling (consecutive periods)
        if (IsConsecutivePeriod(lesson, day, period, existingSchedule))
        {
            candidate.Score += _weights.BlockScheduling;
            candidate.ScoreReasons.Add($"Block scheduling (+{_weights.BlockScheduling})");
        }

        // 8. Penalize consecutive same subject (avoid same subject back-to-back)
        var subjectId = lesson.LessonSubjects.FirstOrDefault()?.SubjectId;
        if (classId.HasValue && subjectId.HasValue && CreatesConsecutiveSameSubject(classId.Value, subjectId.Value, day, period.Id, existingSchedule))
        {
            candidate.Score -= 100; // Heavy penalty
            candidate.ScoreReasons.Add($"Consecutive same subject (-100)");
        }

        return candidate;
    }

    /// <summary>
    /// Checks if scheduling at this time creates a gap for teacher
    /// </summary>
    private bool CreatesTeacherNTP(int teacherId, DayOfWeek day, int periodId, List<ScheduledLesson> schedule)
    {
        var teacherLessonsToday = schedule
            .Where(sl => sl.DayOfWeek == day && sl.Lesson != null && sl.Lesson.LessonTeachers.Any(lt => lt.TeacherId == teacherId))
            .OrderBy(sl => sl.PeriodId)
            .ToList();

        if (!teacherLessonsToday.Any())
            return false; // First lesson of day - no gap

        // Check if this creates a gap between existing lessons
        var periods = teacherLessonsToday.Select(sl => sl.PeriodId).ToList();
        periods.Add(periodId);
        periods = periods.OrderBy(p => p).ToList();

        // Check for gaps in sequence
        for (int i = 0; i < periods.Count - 1; i++)
        {
            if (periods[i + 1] - periods[i] > 1)
                return true; // Gap found
        }

        return false;
    }

    /// <summary>
    /// Checks if scheduling at this time creates a gap for students
    /// </summary>
    private bool CreatesStudentNTP(int classId, DayOfWeek day, int periodId, List<ScheduledLesson> schedule)
    {
        var classLessonsToday = schedule
            .Where(sl => sl.DayOfWeek == day && sl.Lesson != null && sl.Lesson.LessonClasses.Any(lc => lc.ClassId == classId))
            .OrderBy(sl => sl.PeriodId)
            .ToList();

        if (!classLessonsToday.Any())
            return false;

        var periods = classLessonsToday.Select(sl => sl.PeriodId).ToList();
        periods.Add(periodId);
        periods = periods.OrderBy(p => p).ToList();

        for (int i = 0; i < periods.Count - 1; i++)
        {
            if (periods[i + 1] - periods[i] > 1)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if lesson is evenly distributed across the week
    /// </summary>
    private bool IsEvenlyDistributed(Lesson lesson, int instanceNumber, DayOfWeek proposedDay, List<ScheduledLesson> existingSchedule)
    {
        var existingInstances = existingSchedule
            .Where(sl => sl.Lesson != null && sl.Lesson.Id == lesson.Id)
            .Select(sl => sl.DayOfWeek)
            .ToList();

        if (!existingInstances.Any())
            return true; // First instance

        // Calculate ideal spacing
        int frequency = lesson.FrequencyPerWeek;
        if (frequency <= 1)
            return true;

        // For 2x/week: prefer Mon/Thu or Tue/Fri spacing
        // For 3x/week: prefer Mon/Wed/Fri spacing
        // For 4x+/week: any distribution is okay

        int daysSinceLastLesson = 0;
        var lastDay = existingInstances.LastOrDefault();
        if (lastDay != default)
        {
            daysSinceLastLesson = ((int)proposedDay - (int)lastDay + 7) % 7;
        }

        // Prefer at least 1 day gap
        return daysSinceLastLesson >= 1;
    }

    /// <summary>
    /// Checks if teacher uses same room as previous period
    /// </summary>
    private bool SameRoomAsPrevious(int teacherId, DayOfWeek day, Period period, int roomId, List<ScheduledLesson> schedule)
    {
        var previousPeriod = schedule
            .Where(sl => sl.DayOfWeek == day &&
                        sl.Lesson != null &&
                        sl.Lesson.LessonTeachers.Any(lt => lt.TeacherId == teacherId) &&
                        sl.PeriodId < period.Id)
            .OrderByDescending(sl => sl.PeriodId)
            .FirstOrDefault();

        if (previousPeriod == null)
            return false;

        return previousPeriod.RoomId == roomId ||
               previousPeriod.ScheduledLessonRooms.Any(slr => slr.RoomId == roomId);
    }

    /// <summary>
    /// Checks if teacher's workload is balanced for the day
    /// </summary>
    private bool IsBalancedWorkload(int teacherId, DayOfWeek day, List<ScheduledLesson> schedule)
    {
        var lessonsPerDay = schedule
            .Where(sl => sl.Lesson != null && sl.Lesson.LessonTeachers.Any(lt => lt.TeacherId == teacherId))
            .GroupBy(sl => sl.DayOfWeek)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToList();

        if (!lessonsPerDay.Any())
            return true;

        var currentDayCount = lessonsPerDay.FirstOrDefault(l => l.Day == day)?.Count ?? 0;
        var avgPerDay = lessonsPerDay.Average(l => l.Count);

        // Prefer if current day is below or at average
        return currentDayCount <= avgPerDay;
    }

    /// <summary>
    /// Checks if this creates a block of consecutive periods
    /// </summary>
    private bool IsConsecutivePeriod(Lesson lesson, DayOfWeek day, Period period, List<ScheduledLesson> schedule)
    {
        var sameLessonToday = schedule
            .Where(sl => sl.DayOfWeek == day &&
                        sl.Lesson != null &&
                        sl.Lesson.Id == lesson.Id)
            .Select(sl => sl.PeriodId)
            .ToList();

        if (!sameLessonToday.Any())
            return false;

        // Check if adjacent to any existing period
        return sameLessonToday.Any(p => Math.Abs(p - period.Id) == 1);
    }

    /// <summary>
    /// Checks if a lesson can be scheduled at a specific day/period (hard constraints)
    /// </summary>
    /// <summary>
    /// Checks if a lesson can be scheduled at a specific day/period using centralized constraint validation
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
    /// Checks if scheduling this lesson would create consecutive periods of the same subject for the class
    /// </summary>
    private bool CreatesConsecutiveSameSubject(int classId, int subjectId, DayOfWeek day, int periodId, List<ScheduledLesson> schedule)
    {
        // Check the period immediately before
        var previousPeriod = schedule.FirstOrDefault(sl =>
            sl.DayOfWeek == day &&
            sl.PeriodId == periodId - 1 &&
            sl.Lesson != null &&
            sl.Lesson.LessonClasses.Any(lc => lc.ClassId == classId));

        if (previousPeriod != null && previousPeriod.Lesson!.LessonSubjects.Any(ls => ls.SubjectId == subjectId))
        {
            return true; // Same subject in previous period
        }

        // Check the period immediately after
        var nextPeriod = schedule.FirstOrDefault(sl =>
            sl.DayOfWeek == day &&
            sl.PeriodId == periodId + 1 &&
            sl.Lesson != null &&
            sl.Lesson.LessonClasses.Any(lc => lc.ClassId == classId));

        if (nextPeriod != null && nextPeriod.Lesson!.LessonSubjects.Any(ls => ls.SubjectId == subjectId))
        {
            return true; // Same subject in next period
        }

        return false;
    }

    /// <summary>
    /// Calculate quality metrics for the generated timetable
    /// </summary>
    private QualityMetrics CalculateQualityMetrics(List<ScheduledLesson> scheduledLessons, List<Lesson> allLessons)
    {
        var metrics = new QualityMetrics();

        // Calculate teacher NTPs
        var teacherIds = scheduledLessons
            .Where(sl => sl.Lesson != null)
            .SelectMany(sl => sl.Lesson!.LessonTeachers.Select(lt => lt.TeacherId))
            .Distinct();

        foreach (var teacherId in teacherIds)
        {
            var ntps = CountTeacherNTPs(teacherId, scheduledLessons);
            metrics.TeacherNTPs.Add(teacherId, ntps);
            metrics.TotalTeacherNTPs += ntps;
        }

        // Calculate student NTPs
        var classIds = scheduledLessons
            .Where(sl => sl.Lesson != null)
            .SelectMany(sl => sl.Lesson!.LessonClasses.Select(lc => lc.ClassId))
            .Distinct();

        foreach (var classId in classIds)
        {
            var ntps = CountStudentNTPs(classId, scheduledLessons);
            metrics.StudentNTPs.Add(classId, ntps);
            metrics.TotalStudentNTPs += ntps;
        }

        // Calculate overall score (0-100)
        int maxPossibleNTPs = teacherIds.Count() * 5 * 5; // Assume max 5 lessons/day * 5 days
        int actualNTPs = metrics.TotalTeacherNTPs + metrics.TotalStudentNTPs;
        metrics.OverallScore = Math.Max(0, 100 - (actualNTPs * 100 / Math.Max(1, maxPossibleNTPs)));

        return metrics;
    }

    private int CountTeacherNTPs(int teacherId, List<ScheduledLesson> schedule)
    {
        int totalNTPs = 0;

        foreach (var day in Enum.GetValues<DayOfWeek>().Where(d => d != DayOfWeek.Friday && d != DayOfWeek.Saturday))
        {
            var periods = schedule
                .Where(sl => sl.DayOfWeek == day &&
                            sl.Lesson != null &&
                            sl.Lesson.LessonTeachers.Any(lt => lt.TeacherId == teacherId))
                .Select(sl => sl.PeriodId)
                .OrderBy(p => p)
                .ToList();

            if (periods.Count <= 1)
                continue;

            // Count gaps
            for (int i = 0; i < periods.Count - 1; i++)
            {
                totalNTPs += (periods[i + 1] - periods[i] - 1);
            }
        }

        return totalNTPs;
    }

    private int CountStudentNTPs(int classId, List<ScheduledLesson> schedule)
    {
        int totalNTPs = 0;

        foreach (var day in Enum.GetValues<DayOfWeek>().Where(d => d != DayOfWeek.Friday && d != DayOfWeek.Saturday))
        {
            var periods = schedule
                .Where(sl => sl.DayOfWeek == day &&
                            sl.Lesson != null &&
                            sl.Lesson.LessonClasses.Any(lc => lc.ClassId == classId))
                .Select(sl => sl.PeriodId)
                .OrderBy(p => p)
                .ToList();

            if (periods.Count <= 1)
                continue;

            for (int i = 0; i < periods.Count - 1; i++)
            {
                totalNTPs += (periods[i + 1] - periods[i] - 1);
            }
        }

        return totalNTPs;
    }

    /// <summary>
    /// Validates an existing timetable and returns conflicts
    /// </summary>
    public async Task<SchedulingValidationResult> ValidateTimetableAsync(int timetableId)
    {
        var result = new SchedulingValidationResult { IsValid = true };

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
            .Include(sl => sl.Room)
            .Include(sl => sl.ScheduledLessonRooms)
                .ThenInclude(slr => slr.Room)
            .Include(sl => sl.Period)
            .Where(sl => sl.TimetableId == timetableId)
            .ToListAsync();

        // Check for teacher conflicts
        var teacherConflicts = scheduledLessons
            .SelectMany(sl => sl.Lesson!.LessonTeachers.Select(lt => new { ScheduledLesson = sl, lt.TeacherId }))
            .GroupBy(x => new { x.ScheduledLesson.DayOfWeek, x.ScheduledLesson.PeriodId, x.TeacherId })
            .Where(g => g.Count() > 1)
            .Select(g => new Conflict
            {
                Type = ConflictType.TeacherDoubleBooking,
                Description = $"Teacher conflict on {g.Key.DayOfWeek} Period {g.First().ScheduledLesson.Period?.PeriodNumber}",
                AffectedLessons = g.Select(x => x.ScheduledLesson).ToList()
            });

        result.Conflicts.AddRange(teacherConflicts);

        // Check for class conflicts
        var classConflicts = scheduledLessons
            .SelectMany(sl => sl.Lesson!.LessonClasses.Select(lc => new { ScheduledLesson = sl, lc.ClassId }))
            .GroupBy(x => new { x.ScheduledLesson.DayOfWeek, x.ScheduledLesson.PeriodId, x.ClassId })
            .Where(g => g.Count() > 1)
            .Select(g => new Conflict
            {
                Type = ConflictType.ClassDoubleBooking,
                Description = $"Class conflict on {g.Key.DayOfWeek} Period {g.First().ScheduledLesson.Period?.PeriodNumber}",
                AffectedLessons = g.Select(x => x.ScheduledLesson).ToList()
            });

        result.Conflicts.AddRange(classConflicts);

        // Check for room conflicts (including multi-room assignments)
        var roomAssignments = scheduledLessons
            .SelectMany(sl => new[] { new { ScheduledLesson = sl, RoomId = sl.RoomId, Room = sl.Room } }
                .Concat(sl.ScheduledLessonRooms.Select(slr => new { ScheduledLesson = sl, RoomId = (int?)slr.RoomId, Room = slr.Room })))
            .Where(ra => ra.RoomId.HasValue)
            .ToList();

        var roomConflicts = roomAssignments
            .GroupBy(ra => new { ra.ScheduledLesson.DayOfWeek, ra.ScheduledLesson.PeriodId, ra.RoomId })
            .Where(g => g.Select(ra => ra.ScheduledLesson.Id).Distinct().Count() > 1)
            .Select(g => new Conflict
            {
                Type = ConflictType.RoomDoubleBooking,
                Description = $"Room {g.First().Room?.Name} conflict on {g.Key.DayOfWeek} Period {g.First().ScheduledLesson.Period?.PeriodNumber}",
                AffectedLessons = g.Select(ra => ra.ScheduledLesson).Distinct().ToList()
            });

        result.Conflicts.AddRange(roomConflicts);

        result.IsValid = !result.Conflicts.Any();

        return result;
    }

    /// <summary>
    /// Calculates an availability score for a specific time slot based on constraints
    /// Negative values = AVOID scheduling (penalty), Positive values = TRY TO schedule (bonus)
    /// Higher score = better time slot
    /// </summary>
    private int CalculateAvailabilityScore(
        Lesson lesson,
        DayOfWeek day,
        int periodId,
        Room? assignedRoom,
        List<TeacherAvailability> teacherAvailabilities,
        List<ClassAvailability> classAvailabilities,
        List<RoomAvailability> roomAvailabilities,
        List<SubjectAvailability> subjectAvailabilities)
    {
        int score = 0; // Start with neutral score

        // Teacher availability constraint
        var primaryTeacherId = lesson.LessonTeachers.FirstOrDefault()?.TeacherId;
        if (primaryTeacherId.HasValue)
        {
            var teacherConstraint = teacherAvailabilities.FirstOrDefault(ta =>
                ta.TeacherId == primaryTeacherId.Value &&
                ta.DayOfWeek == day &&
                ta.PeriodId == periodId);
            if (teacherConstraint != null)
            {
                score += teacherConstraint.Importance; // Negative = avoid, Positive = prefer
            }
        }

        // Second teacher availability constraint (if exists)
        var secondaryTeacherId = lesson.LessonTeachers.Skip(1).FirstOrDefault()?.TeacherId;
        if (secondaryTeacherId.HasValue)
        {
            var secondTeacherConstraint = teacherAvailabilities.FirstOrDefault(ta =>
                ta.TeacherId == secondaryTeacherId.Value &&
                ta.DayOfWeek == day &&
                ta.PeriodId == periodId);
            if (secondTeacherConstraint != null)
            {
                score += secondTeacherConstraint.Importance;
            }
        }

        // Class availability constraint
        var classId = lesson.LessonClasses.FirstOrDefault()?.ClassId;
        if (classId.HasValue)
        {
            var classConstraint = classAvailabilities.FirstOrDefault(ca =>
                ca.ClassId == classId.Value &&
                ca.DayOfWeek == day &&
                ca.PeriodId == periodId);
            if (classConstraint != null)
            {
                score += classConstraint.Importance;
            }
        }

        // Subject availability constraint
        var subjectId = lesson.LessonSubjects.FirstOrDefault()?.SubjectId;
        if (subjectId.HasValue)
        {
            var subjectConstraint = subjectAvailabilities.FirstOrDefault(sa =>
                sa.SubjectId == subjectId.Value &&
                sa.DayOfWeek == day &&
                sa.PeriodId == periodId);
            if (subjectConstraint != null)
            {
                score += subjectConstraint.Importance;
            }
        }

        // Room availability constraint (if room is assigned)
        if (assignedRoom != null)
        {
            var roomConstraint = roomAvailabilities.FirstOrDefault(ra =>
                ra.RoomId == assignedRoom.Id &&
                ra.DayOfWeek == day &&
                ra.PeriodId == periodId);
            if (roomConstraint != null)
            {
                score += roomConstraint.Importance;
            }
        }

        return score;
    }
}

/// <summary>
/// Quality metrics for a generated timetable
/// </summary>
public class QualityMetrics
{
    public int TotalTeacherNTPs { get; set; }
    public int TotalStudentNTPs { get; set; }
    public Dictionary<int, int> TeacherNTPs { get; set; } = new();
    public Dictionary<int, int> StudentNTPs { get; set; } = new();
    public int OverallScore { get; set; } // 0-100
}
