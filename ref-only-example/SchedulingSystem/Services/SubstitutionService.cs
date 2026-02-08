using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Services;

/// <summary>
/// Service for managing teacher absences and substitutions
/// </summary>
public class SubstitutionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SubstitutionService> _logger;

    /// <summary>
    /// Represents a ranked substitute candidate with match scoring
    /// </summary>
    public class SubstituteCandidate
    {
        public Teacher Teacher { get; set; } = null!;
        public int MatchScore { get; set; }
        public List<string> MatchReasons { get; set; } = new();
        public bool IsQualified { get; set; }
        public bool IsSameDepartment { get; set; }
        public int SubstitutionsThisWeek { get; set; }
        public int SubstitutionsThisMonth { get; set; }
        public decimal TotalHoursThisMonth { get; set; }
        public decimal? HourlyRate { get; set; }
        public string? DepartmentName { get; set; }
        public string? Preferences { get; set; }
        public string? QualificationNotes { get; set; }
        public int ExperienceYears { get; set; }
        public bool IsOnSubstitutionReserve { get; set; }

        // Additional scoring details
        public int SubstitutionsLast7Days { get; set; }
        public decimal ActualTeachingHoursThisWeek { get; set; }
        public int PeriodsOnSameDay { get; set; }
        public int? AvailabilityPreference { get; set; } // 1-5 scale, null if no preference set

        // Score breakdown components
        public int SubstitutionReservePoints { get; set; }
        public int QualificationPoints { get; set; }
        public int DepartmentPoints { get; set; }
        public int WorkloadPoints { get; set; }
        public int AvailabilityPoints { get; set; }
        public int ExperiencePoints { get; set; }
    }

    public SubstitutionService(
        ApplicationDbContext context,
        ILogger<SubstitutionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Create a new teacher absence record
    /// </summary>
    public async Task<Absence> CreateAbsenceAsync(
        int teacherId,
        DateTime date,
        TimeSpan? startTime,
        TimeSpan? endTime,
        AbsenceType type,
        string? notes,
        string? reportedByUserId)
    {
        // Validate teacher exists
        var teacherExists = await _context.Teachers.AnyAsync(t => t.Id == teacherId);
        if (!teacherExists)
        {
            throw new ArgumentException($"Teacher with ID {teacherId} not found", nameof(teacherId));
        }

        _logger.LogInformation("Creating absence for TeacherId={TeacherId}, Date={Date}, ReportedByUserId={UserId}",
            teacherId, date, reportedByUserId ?? "NULL");

        var absence = new Absence
        {
            TeacherId = teacherId,
            Date = date.Date, // Ensure date only
            StartTime = startTime,
            EndTime = endTime,
            Type = type,
            Status = AbsenceStatus.Reported,
            Notes = notes,
            ReportedAt = DateTime.UtcNow,
            ReportedByUserId = reportedByUserId,
            TotalHours = CalculateAbsenceHours(startTime, endTime)
        };

        _context.Absences.Add(absence);

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully created absence {AbsenceId} for teacher {TeacherId} on {Date}",
                absence.Id, teacherId, date);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save absence to database. TeacherId={TeacherId}, ReportedByUserId={UserId}",
                teacherId, reportedByUserId ?? "NULL");
            throw;
        }

        return absence;
    }

    /// <summary>
    /// Get all lessons affected by a teacher absence
    /// </summary>
    public async Task<List<ScheduledLesson>> GetAffectedLessonsAsync(int absenceId)
    {
        var absence = await _context.Absences
            .Include(a => a.Teacher)
            .FirstOrDefaultAsync(a => a.Id == absenceId);

        if (absence == null)
            return new List<ScheduledLesson>();

        // Get the day of week for the absence date
        var dayOfWeek = absence.Date.DayOfWeek;

        // Get the latest published timetable
        var timetable = await _context.Timetables
            .Where(t => t.Status == TimetableStatus.Published)
            .OrderByDescending(t => t.CreatedDate)
            .FirstOrDefaultAsync();

        if (timetable == null)
            return new List<ScheduledLesson>();

        // Find all scheduled lessons for this teacher on this day of week
        var affectedLessons = await _context.ScheduledLessons
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonSubjects)
                    .ThenInclude(ls => ls.Subject)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonClasses)
                    .ThenInclude(lc => lc.Class)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonTeachers)
            .Include(sl => sl.Period)
            .Include(sl => sl.Room)
            .Include(sl => sl.ScheduledLessonRooms)
                .ThenInclude(slr => slr.Room)
            .Where(sl => sl.TimetableId == timetable.Id)
            .Where(sl => sl.Lesson!.LessonTeachers.Any(lt => lt.TeacherId == absence.TeacherId))
            .Where(sl => sl.DayOfWeek == dayOfWeek)
            .ToListAsync();

        // Filter by time if partial day absence
        if (absence.StartTime.HasValue && absence.EndTime.HasValue)
        {
            affectedLessons = affectedLessons
                .Where(sl => sl.Period != null &&
                            sl.Period.StartTime < absence.EndTime.Value &&
                            sl.Period.EndTime > absence.StartTime.Value)
                .ToList();
        }

        _logger.LogInformation("Found {Count} affected lessons for absence {AbsenceId} on {DayOfWeek}",
            affectedLessons.Count, absenceId, dayOfWeek);

        return affectedLessons;
    }

    /// <summary>
    /// Find available substitute teachers for a specific time slot
    /// </summary>
    public async Task<List<Teacher>> FindAvailableSubstitutesAsync(
        DayOfWeek dayOfWeek,
        int periodId,
        int? subjectId = null)
    {
        // Get all active teachers available for substitution
        var availableForSubstitution = await _context.Teachers
            .Include(t => t.TeacherSubjects)
                .ThenInclude(ts => ts.Subject)
            .Include(t => t.Department)
            .Where(t => t.IsActive && t.AvailableForSubstitution)
            .ToListAsync();

        var availableTeachers = new List<Teacher>();

        foreach (var teacher in availableForSubstitution)
        {
            // Check if teacher is already teaching at this time
            var isBusy = await IsTeacherBusyAsync(teacher.Id, dayOfWeek, periodId);
            if (isBusy)
                continue;

            // If subject specified, check qualification (optional - can be relaxed)
            if (subjectId.HasValue)
            {
                var isQualified = await IsTeacherQualifiedAsync(teacher.Id, subjectId.Value);
                // For MVP, we'll include unqualified teachers but rank them lower
                // In Phase 4B, this will be part of the ranking algorithm
            }

            // Check substitution workload this week
            var subsThisWeek = await GetSubstitutionCountThisWeekAsync(teacher.Id);
            var maxSubsPerWeek = teacher.MaxSubstitutionsPerWeek ?? 5;
            if (subsThisWeek >= maxSubsPerWeek)
                continue;

            availableTeachers.Add(teacher);
        }

        _logger.LogInformation("Found {Count} available substitutes for {Day} period {PeriodId}",
            availableTeachers.Count, dayOfWeek, periodId);

        return availableTeachers;
    }

    /// <summary>
    /// Rank available substitutes using intelligent scoring algorithm
    /// Scoring criteria:
    /// - Co-teacher on same lesson: +250 points (HIGHEST - already teaching this class/subject)
    /// - Substitution reserve duty: +200 points (has "substitution" lesson at this time)
    /// - Subject qualification: +100 points
    /// - Same department: +50 points
    /// - Low workload (fewer subs this week): +40 points (scaled)
    /// - Availability: +30 points
    /// - Previous experience in subject: +20 points (scaled)
    /// Maximum possible score: 490 points (co-teacher + all bonuses), 440 points (substitution reserve), 240 points (regular)
    /// </summary>
    public async Task<List<SubstituteCandidate>> RankSubstitutesAsync(
        int absentTeacherId,
        int scheduledLessonId)
    {
        var scheduledLesson = await _context.ScheduledLessons
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l.LessonSubjects)
                    .ThenInclude(ls => ls.Subject)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l.LessonTeachers)
                    .ThenInclude(lt => lt.Teacher)
                        .ThenInclude(t => t.Department)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l.LessonTeachers)
                    .ThenInclude(lt => lt.Teacher)
                        .ThenInclude(t => t.TeacherSubjects)
                            .ThenInclude(ts => ts.Subject)
            .Include(sl => sl.Period)
            .FirstOrDefaultAsync(sl => sl.Id == scheduledLessonId);

        if (scheduledLesson == null || scheduledLesson.Lesson == null)
            return new List<SubstituteCandidate>();

        var subjectId = scheduledLesson.Lesson.LessonSubjects.FirstOrDefault()?.SubjectId ?? 0;
        var absentTeacher = scheduledLesson.Lesson.LessonTeachers.FirstOrDefault()?.Teacher;
        var dayOfWeek = scheduledLesson.DayOfWeek;
        var periodId = scheduledLesson.PeriodId;

        // Get co-teachers from the same lesson (other teachers assigned to teach this lesson)
        var coTeachers = scheduledLesson.Lesson.LessonTeachers
            .Where(lt => lt.TeacherId != absentTeacherId && lt.Teacher != null && lt.Teacher.IsActive)
            .Select(lt => lt.Teacher!)
            .ToList();
        var coTeacherIds = coTeachers.Select(t => t.Id).ToHashSet();

        // Get all active teachers available for substitution
        var availableTeachers = await _context.Teachers
            .Include(t => t.TeacherSubjects)
                .ThenInclude(ts => ts.Subject)
            .Include(t => t.Department)
            .Where(t => t.IsActive && t.AvailableForSubstitution)
            .ToListAsync();

        // Add co-teachers even if they don't have AvailableForSubstitution flag
        // They're already assigned to this lesson and can continue teaching alone
        foreach (var coTeacher in coTeachers)
        {
            if (!availableTeachers.Any(t => t.Id == coTeacher.Id))
            {
                // Reload co-teacher with full includes for consistency
                var coTeacherWithIncludes = await _context.Teachers
                    .Include(t => t.TeacherSubjects)
                        .ThenInclude(ts => ts.Subject)
                    .Include(t => t.Department)
                    .FirstOrDefaultAsync(t => t.Id == coTeacher.Id);
                if (coTeacherWithIncludes != null)
                {
                    availableTeachers.Add(coTeacherWithIncludes);
                }
            }
        }

        var candidates = new List<SubstituteCandidate>();

        // Get latest published timetable for checking substitution reserve lessons
        var timetable = await _context.Timetables
            .Where(t => t.Status == TimetableStatus.Published)
            .OrderByDescending(t => t.CreatedDate)
            .FirstOrDefaultAsync();

        foreach (var teacher in availableTeachers)
        {
            // Skip if same as absent teacher
            if (teacher.Id == absentTeacherId)
                continue;

            // Check if this teacher is a co-teacher on the same lesson
            var isCoTeacher = coTeacherIds.Contains(teacher.Id);

            // Check if teacher is already teaching a regular lesson at this time
            // Co-teachers are allowed even if "busy" - they're teaching the same lesson
            if (!isCoTeacher)
            {
                var isBusy = await IsTeacherBusyAsync(teacher.Id, dayOfWeek, periodId);
                if (isBusy)
                    continue;
            }

            // Check substitution workload this week (skip for co-teachers - they're not doing extra work)
            if (!isCoTeacher)
            {
                var subsThisWeek = await GetSubstitutionCountThisWeekAsync(teacher.Id);
                var maxSubsPerWeek = teacher.MaxSubstitutionsPerWeek ?? 5;
                if (subsThisWeek >= maxSubsPerWeek)
                    continue;
            }

            // Get full stats
            var (thisWeek, thisMonth, totalHours) = await GetSubstitutionStatsAsync(teacher.Id);

            // Calculate match score
            int matchScore = 0;
            var matchReasons = new List<string>();

            // 0a. HIGHEST PRIORITY: Co-teacher on the same lesson (+250 points)
            // Co-teachers are already assigned to teach this lesson with the absent teacher
            // They can continue teaching alone - no extra work, same subject, same class
            if (isCoTeacher)
            {
                matchScore += 250;
                matchReasons.Add("⭐⭐ CO-TEACHER - Already teaching this lesson!");
            }

            // 0b. HIGH PRIORITY: Substitution Reserve Teacher (+200 points)
            // Check if teacher has a "sub" (substitution) lesson at this time (substitution reserve duty)
            bool isOnSubstitutionReserve = false;
            if (timetable != null)
            {
                isOnSubstitutionReserve = await _context.ScheduledLessons
                    .Include(sl => sl.Lesson)
                        .ThenInclude(l => l.LessonSubjects)
                            .ThenInclude(ls => ls.Subject)
                    .Include(sl => sl.Lesson)
                        .ThenInclude(l => l.LessonTeachers)
                    .AnyAsync(sl => sl.TimetableId == timetable.Id &&
                                   sl.Lesson != null &&
                                   sl.Lesson.LessonTeachers.Any(lt => lt.TeacherId == teacher.Id) &&
                                   sl.DayOfWeek == dayOfWeek &&
                                   sl.PeriodId == periodId &&
                                   sl.Lesson.LessonSubjects.Any(ls => ls.Subject != null &&
                                       !string.IsNullOrWhiteSpace(ls.Subject.Code) &&
                                       ls.Subject.Code.Trim().ToLower() == "sub"));

                if (isOnSubstitutionReserve)
                {
                    // Check if already assigned to another substitution at this exact time
                    var alreadyAssignedToSubstitution = await _context.Substitutions
                        .Include(s => s.Absence)
                        .Include(s => s.ScheduledLesson)
                        .AnyAsync(s => s.SubstituteTeacherId == teacher.Id &&
                                      s.ScheduledLesson != null &&
                                      s.ScheduledLesson.DayOfWeek == dayOfWeek &&
                                      s.ScheduledLesson.PeriodId == periodId);

                    if (!alreadyAssignedToSubstitution)
                    {
                        matchScore += 200;
                        matchReasons.Add("⭐ ON SUBSTITUTION RESERVE - Perfect match!");
                    }
                    else
                    {
                        // Already covering another absence, skip this teacher
                        continue;
                    }
                }
            }

            // 1. Subject Qualification (+100 points)
            var isQualified = await IsTeacherQualifiedAsync(teacher.Id, subjectId);
            if (isQualified)
            {
                matchScore += 100;
                matchReasons.Add($"Qualified to teach {scheduledLesson.Lesson.LessonSubjects.FirstOrDefault()?.Subject?.Name ?? "subject"}");
            }
            // 1b. Subject Qualification Notes (unofficial/informal qualifications) (+50 points if not formally qualified)
            else if (!string.IsNullOrWhiteSpace(teacher.SubstitutionQualificationNotes))
            {
                matchScore += 50;
                matchReasons.Add($"Has informal qualifications: {teacher.SubstitutionQualificationNotes}");
            }

            // 2. Same Department (+50 points)
            bool isSameDepartment = false;
            if (teacher.DepartmentId.HasValue && absentTeacher != null && absentTeacher.DepartmentId.HasValue &&
                teacher.DepartmentId == absentTeacher.DepartmentId)
            {
                isSameDepartment = true;
                matchScore += 50;
                matchReasons.Add($"Same department as {absentTeacher.Name}");
            }

            // 3. Low Workload (+40 points maximum, scaled)
            // 0 subs = 40 points, 5 subs = 20 points, 10+ subs = 0 points
            var workloadScore = Math.Max(0, 40 - (thisWeek * 4));
            matchScore += workloadScore;
            if (workloadScore > 20)
                matchReasons.Add($"Low workload ({thisWeek} substitutions this week)");
            else if (workloadScore > 0)
                matchReasons.Add($"Moderate workload ({thisWeek} substitutions this week)");

            // 4. Availability (+30 points - already checked above)
            matchScore += 30;
            matchReasons.Add("Available at this time");

            // 5. Previous substitution success (+20 points for experience in this subject)
            var previousSubsInSubject = await GetPreviousSubstitutionCountAsync(teacher.Id, subjectId);
            if (previousSubsInSubject > 0)
            {
                var experienceScore = Math.Min(20, previousSubsInSubject * 5); // Cap at 20 points
                matchScore += experienceScore;
                matchReasons.Add($"Previously substituted {previousSubsInSubject}× in this subject");
            }

            candidates.Add(new SubstituteCandidate
            {
                Teacher = teacher,
                MatchScore = matchScore,
                MatchReasons = matchReasons,
                IsQualified = isQualified,
                IsSameDepartment = isSameDepartment,
                SubstitutionsThisWeek = thisWeek,
                SubstitutionsThisMonth = thisMonth,
                TotalHoursThisMonth = totalHours,
                HourlyRate = teacher.SubstitutionHourlyRate,
                DepartmentName = teacher.Department?.Name,
                Preferences = teacher.SubstitutionPreferences,
                QualificationNotes = teacher.SubstitutionQualificationNotes,
                ExperienceYears = 0, // Not tracked in current model
                IsOnSubstitutionReserve = isOnSubstitutionReserve
            });
        }

        // Sort by match score (highest first)
        var rankedCandidates = candidates
            .OrderByDescending(c => c.MatchScore)
            .ThenBy(c => c.SubstitutionsThisWeek)
            .ThenByDescending(c => c.IsQualified)
            .ToList();

        _logger.LogInformation("Ranked {Count} substitute candidates for lesson {LessonId}, top score: {TopScore}",
            rankedCandidates.Count, scheduledLessonId, rankedCandidates.FirstOrDefault()?.MatchScore ?? 0);

        return rankedCandidates;
    }

    /// <summary>
    /// Auto-assign the best matching substitute to a lesson
    /// </summary>
    public async Task<Substitution?> AutoAssignBestSubstituteAsync(
        int absenceId,
        int scheduledLessonId,
        string? assignedByUserId,
        int minimumScore = 100)
    {
        var absence = await _context.Absences
            .Include(a => a.Teacher)
            .FirstOrDefaultAsync(a => a.Id == absenceId);

        if (absence == null)
            return null;

        var rankedCandidates = await RankSubstitutesAsync(absence.TeacherId, scheduledLessonId);

        var bestCandidate = rankedCandidates.FirstOrDefault();
        if (bestCandidate == null || bestCandidate.MatchScore < minimumScore)
        {
            _logger.LogWarning("No suitable substitute found for lesson {LessonId} (minimum score: {MinScore})",
                scheduledLessonId, minimumScore);
            return null;
        }

        // Assign the best candidate
        var substitution = await AssignSubstituteAsync(
            absenceId,
            scheduledLessonId,
            bestCandidate.Teacher.Id,
            SubstitutionType.TeacherSubstitute,
            $"Auto-assigned (match score: {bestCandidate.MatchScore})",
            assignedByUserId,
            bestCandidate.HourlyRate
        );

        _logger.LogInformation("Auto-assigned teacher {TeacherId} ({Name}) to lesson {LessonId} with score {Score}",
            bestCandidate.Teacher.Id, bestCandidate.Teacher.Name, scheduledLessonId, bestCandidate.MatchScore);

        return substitution;
    }

    /// <summary>
    /// Auto-assign substitutes to all uncovered lessons for an absence
    /// </summary>
    public async Task<(int Assigned, int Failed)> AutoAssignAllSubstitutesAsync(
        int absenceId,
        string? assignedByUserId,
        int minimumScore = 100)
    {
        var affectedLessons = await GetAffectedLessonsAsync(absenceId);
        var existingSubstitutions = await _context.Substitutions
            .Where(s => s.AbsenceId == absenceId)
            .Select(s => s.ScheduledLessonId)
            .ToListAsync();

        var uncoveredLessons = affectedLessons
            .Where(l => !existingSubstitutions.Contains(l.Id))
            .ToList();

        int assigned = 0;
        int failed = 0;

        foreach (var lesson in uncoveredLessons)
        {
            var substitution = await AutoAssignBestSubstituteAsync(
                absenceId,
                lesson.Id,
                assignedByUserId,
                minimumScore
            );

            if (substitution != null)
                assigned++;
            else
                failed++;
        }

        _logger.LogInformation("Auto-assignment for absence {AbsenceId}: {Assigned} assigned, {Failed} failed",
            absenceId, assigned, failed);

        return (assigned, failed);
    }

    /// <summary>
    /// Assign a substitute teacher to a lesson
    /// </summary>
    public async Task<Substitution> AssignSubstituteAsync(
        int absenceId,
        int scheduledLessonId,
        int? substituteTeacherId,
        SubstitutionType type,
        string? notes,
        string? assignedByUserId,
        decimal? payRate = null)
    {
        var scheduledLesson = await _context.ScheduledLessons
            .Include(sl => sl.Period)
            .Include(sl => sl.Lesson)
            .FirstOrDefaultAsync(sl => sl.Id == scheduledLessonId);

        if (scheduledLesson == null || scheduledLesson.Period == null)
            throw new ArgumentException($"Scheduled lesson {scheduledLessonId} not found");

        // Calculate hours worked (period duration)
        var hoursWorked = scheduledLesson.Period.DurationMinutes / 60m;

        // Get substitute teacher's pay rate if not provided
        if (payRate == null && substituteTeacherId.HasValue)
        {
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.Id == substituteTeacherId.Value);
            payRate = teacher?.SubstitutionHourlyRate;
        }

        var substitution = new Substitution
        {
            AbsenceId = absenceId,
            ScheduledLessonId = scheduledLessonId,
            SubstituteTeacherId = substituteTeacherId,
            Type = type,
            Notes = notes,
            AssignedAt = DateTime.UtcNow,
            AssignedByUserId = assignedByUserId,
            HoursWorked = hoursWorked,
            PayRate = payRate,
            TotalPay = payRate.HasValue ? hoursWorked * payRate.Value : null
        };

        _context.Substitutions.Add(substitution);

        // Update absence status
        await UpdateAbsenceStatusAsync(absenceId);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Assigned substitute {SubstituteId} to lesson {LessonId} for absence {AbsenceId}",
            substituteTeacherId, scheduledLessonId, absenceId);

        return substitution;
    }

    /// <summary>
    /// Get all substitutions for a specific date
    /// </summary>
    public async Task<List<Substitution>> GetDailySubstitutionsAsync(DateTime date)
    {
        var substitutions = await _context.Substitutions
            .Include(s => s.Absence)
                .ThenInclude(a => a.Teacher)
            .Include(s => s.SubstituteTeacher)
            .Include(s => s.ScheduledLesson)
                .ThenInclude(sl => sl.Lesson)
                    .ThenInclude(l => l.LessonSubjects)
                        .ThenInclude(ls => ls.Subject)
            .Include(s => s.ScheduledLesson)
                .ThenInclude(sl => sl.Lesson)
                    .ThenInclude(l => l.LessonClasses)
                        .ThenInclude(lc => lc.Class)
            .Include(s => s.ScheduledLesson)
                .ThenInclude(sl => sl.Period)
            .Include(s => s.ScheduledLesson)
                .ThenInclude(sl => sl.Room)
            .Where(s => s.Absence.Date.Date == date.Date)
            .OrderBy(s => s.ScheduledLesson != null && s.ScheduledLesson.Period != null ? s.ScheduledLesson.Period.PeriodNumber : 0)
            .ToListAsync();

        return substitutions;
    }

    /// <summary>
    /// Get substitution statistics for a teacher
    /// </summary>
    public async Task<(int ThisWeek, int ThisMonth, decimal TotalHours)> GetSubstitutionStatsAsync(int teacherId)
    {
        var today = DateTime.Today;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        // Get all substitutions for the month - include Absence for date filtering
        var substitutions = await _context.Substitutions
            .Include(s => s.Absence)
            .Where(s => s.SubstituteTeacherId == teacherId)
            .Where(s => s.Absence != null && s.Absence.Date >= monthStart)
            .ToListAsync();

        // Calculate stats on the client side (SQLite doesn't support Sum on decimal)
        var thisWeek = substitutions.Count(s => s.Absence != null && s.Absence.Date >= weekStart);
        var thisMonth = substitutions.Count;
        var totalHours = substitutions.Sum(s => s.HoursWorked);

        return (thisWeek, thisMonth, totalHours);
    }

    // Helper methods

    private decimal CalculateAbsenceHours(TimeSpan? startTime, TimeSpan? endTime)
    {
        if (!startTime.HasValue || !endTime.HasValue)
        {
            // All day absence - assume standard school day (7 hours)
            return 7.0m;
        }

        var duration = endTime.Value - startTime.Value;
        return (decimal)duration.TotalHours;
    }

    private async Task<bool> IsTeacherBusyAsync(int teacherId, DayOfWeek dayOfWeek, int periodId)
    {
        // Get the latest published timetable
        var timetable = await _context.Timetables
            .Where(t => t.Status == TimetableStatus.Published)
            .OrderByDescending(t => t.CreatedDate)
            .FirstOrDefaultAsync();

        if (timetable == null)
            return false;

        // Check if teacher has a regular lesson at this time
        // Exclude "sub" (substitution) lessons as those are substitution reserve duty (teacher is available)
        // Teacher is busy if they have ANY non-substitution lesson at this time
        return await _context.ScheduledLessons
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l.LessonSubjects)
                    .ThenInclude(ls => ls.Subject)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l.LessonTeachers)
            .AnyAsync(sl => sl.TimetableId == timetable.Id &&
                           sl.Lesson != null &&
                           sl.Lesson.LessonTeachers.Any(lt => lt.TeacherId == teacherId) &&
                           sl.DayOfWeek == dayOfWeek &&
                           sl.PeriodId == periodId &&
                           sl.Lesson.LessonSubjects.Any(ls => ls.Subject != null &&
                               !string.IsNullOrWhiteSpace(ls.Subject.Code) &&
                               ls.Subject.Code.Trim().ToLower() != "sub"));
    }

    private async Task<bool> IsTeacherQualifiedAsync(int teacherId, int subjectId)
    {
        return await _context.TeacherSubjects
            .AnyAsync(ts => ts.TeacherId == teacherId &&
                           ts.SubjectId == subjectId);
    }

    private async Task<int> GetPreviousSubstitutionCountAsync(int teacherId, int subjectId)
    {
        return await _context.Substitutions
            .Include(s => s.ScheduledLesson)
                .ThenInclude(sl => sl != null ? sl.Lesson : null)
                    .ThenInclude(l => l != null ? l.LessonSubjects : null)
            .Where(s => s.SubstituteTeacherId == teacherId &&
                       s.ScheduledLesson != null &&
                       s.ScheduledLesson.Lesson != null &&
                       s.ScheduledLesson.Lesson.LessonSubjects.Any(ls => ls.SubjectId == subjectId))
            .CountAsync();
    }

    private async Task<int> GetSubstitutionCountThisWeekAsync(int teacherId)
    {
        var weekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);

        return await _context.Substitutions
            .Where(s => s.SubstituteTeacherId == teacherId)
            .Where(s => s.Absence.Date >= weekStart)
            .CountAsync();
    }

    public async Task UpdateAbsenceStatusAsync(int absenceId)
    {
        var absence = await _context.Absences
            .Include(a => a.Substitutions)
            .FirstOrDefaultAsync(a => a.Id == absenceId);

        if (absence == null)
            return;

        var affectedLessons = await GetAffectedLessonsAsync(absenceId);
        var totalLessons = affectedLessons.Count;
        var coveredLessons = absence.Substitutions.Count;

        if (coveredLessons == 0)
        {
            absence.Status = AbsenceStatus.Reported;
        }
        else if (coveredLessons < totalLessons)
        {
            absence.Status = AbsenceStatus.PartiallyCovered;
        }
        else
        {
            absence.Status = AbsenceStatus.Covered;
        }
    }
}
