using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;
using SchedulingSystem.Services;

namespace SchedulingSystem.Pages.Admin.Absences;

public class FindSubstitutesModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly SubstitutionService _substitutionService;
    private readonly EmailService _emailService;
    private readonly NotificationService _notificationService;
    private readonly ILogger<FindSubstitutesModel> _logger;

    public FindSubstitutesModel(
        ApplicationDbContext context,
        SubstitutionService substitutionService,
        EmailService emailService,
        NotificationService notificationService,
        ILogger<FindSubstitutesModel> logger)
    {
        _context = context;
        _substitutionService = substitutionService;
        _emailService = emailService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public Absence Absence { get; set; } = null!;
    public List<AffectedLessonViewModel> AffectedLessons { get; set; } = new();
    public List<AffectedSupervisionViewModel> AffectedSupervisionDuties { get; set; } = new();
    public int? PublishedTimetableId { get; set; }
    public List<int> SupervisionPeriods { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = "score"; // Default sort by match score

    // Convert supervision period number to "Break 1", "Break 2", etc.
    public string GetBreakLabel(int periodNumber)
    {
        var index = SupervisionPeriods.IndexOf(periodNumber);
        return index >= 0 ? $"Break {index + 1}" : "Break";
    }

    public class AffectedLessonViewModel
    {
        public int ScheduledLessonId { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public int PeriodId { get; set; }
        public string PeriodTime { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public string Room { get; set; } = string.Empty;
        public bool IsAssigned { get; set; }
        public int? SubstitutionId { get; set; }
        public string? AssignedSubstitute { get; set; }
        public List<SubstituteCandidate> AvailableSubstitutes { get; set; } = new();
    }

    public class SubstituteCandidate
    {
        public int TeacherId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public bool IsQualified { get; set; }
        public int SubstitutionsThisWeek { get; set; }
        public int MatchScore { get; set; }
        public List<string> MatchReasons { get; set; } = new();
        public string? DepartmentName { get; set; }
        public bool IsSameDepartment { get; set; }
        public bool IsOnSubstitutionReserve { get; set; }
        public int? AvailabilityImportance { get; set; } // -3 to +3, null if no preference set
        public string? AvailabilityReason { get; set; }
    }

    public class AffectedSupervisionViewModel
    {
        public int DutyId { get; set; }
        public int PeriodNumber { get; set; }
        public string Location { get; set; } = string.Empty;
        public int? RoomId { get; set; }
        public bool IsAssigned { get; set; }
        public int? SubstitutionId { get; set; }
        public string? AssignedSubstituteName { get; set; }
        public SupervisionSubstitutionType? SubstitutionType { get; set; }
        public List<SupervisionSubstituteCandidate> AvailableSubstitutes { get; set; } = new();
    }

    public class SupervisionSubstituteCandidate
    {
        public int TeacherId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? DepartmentName { get; set; }
        public int SupervisionsThisWeek { get; set; }
        public bool HasSupervisionDutyThisPeriod { get; set; } // Already has supervision at this time
        public bool IsFreeThisPeriod { get; set; } // No lessons scheduled
        public int MatchScore { get; set; }
        public List<string> MatchReasons { get; set; } = new();
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        await LoadDataAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAutoAssignAllAsync(int absenceId)
    {
        try
        {
            var (assigned, failed) = await _substitutionService.AutoAssignAllSubstitutesAsync(
                absenceId,
                User.Identity?.Name,
                minimumScore: 100 // Require at least 100 points (qualified or available)
            );

            if (assigned > 0)
            {
                TempData["SuccessMessage"] = $"Auto-assigned {assigned} substitute(s). {failed} lesson(s) could not be covered automatically.";
            }
            else
            {
                TempData["ErrorMessage"] = "Could not auto-assign any substitutes. Please assign manually.";
            }

            return RedirectToPage(new { id = absenceId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during auto-assignment");
            TempData["ErrorMessage"] = "An error occurred during auto-assignment.";
            return RedirectToPage(new { id = absenceId });
        }
    }

    public async Task<IActionResult> OnPostAssignAsync(int absenceId, int scheduledLessonId, int? substituteTeacherId, string assignmentType)
    {
        try
        {
            _logger.LogInformation("OnPostAssignAsync called with absenceId={AbsenceId}, scheduledLessonId={ScheduledLessonId}, substituteTeacherId={SubstituteTeacherId}, assignmentType={AssignmentType}",
                absenceId, scheduledLessonId, substituteTeacherId, assignmentType);

            var substitutionType = assignmentType switch
            {
                "teacher" => SubstitutionType.TeacherSubstitute,
                "selfstudy" => SubstitutionType.SelfStudy,
                "cancelled" => SubstitutionType.Cancelled,
                _ => SubstitutionType.TeacherSubstitute
            };

            // Check if substitution already exists
            var existingSubstitution = await _context.Substitutions
                .FirstOrDefaultAsync(s => s.AbsenceId == absenceId && s.ScheduledLessonId == scheduledLessonId);

            if (existingSubstitution != null)
            {
                TempData["ErrorMessage"] = "A substitution has already been assigned for this lesson.";
                return RedirectToPage(new { id = absenceId });
            }

            // Get lesson and teacher details for email
            var scheduledLesson = await _context.ScheduledLessons
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l.LessonSubjects)
                        .ThenInclude(ls => ls.Subject)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l.LessonClasses)
                        .ThenInclude(lc => lc.Class)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l.LessonTeachers)
                        .ThenInclude(lt => lt.Teacher)
                .Include(sl => sl.Period)
                .Include(sl => sl.Room)
                .Include(sl => sl.ScheduledLessonRooms)
                    .ThenInclude(slr => slr.Room)
                .FirstOrDefaultAsync(sl => sl.Id == scheduledLessonId);

            var absence = await _context.Absences
                .Include(a => a.Teacher)
                .FirstOrDefaultAsync(a => a.Id == absenceId);

            if (scheduledLesson == null || absence == null || scheduledLesson.Lesson == null ||
                scheduledLesson.Period == null)
            {
                TempData["ErrorMessage"] = "Lesson or absence not found.";
                return RedirectToPage(new { id = absenceId });
            }

            // Validate substitute teacher exists if provided
            if (substituteTeacherId.HasValue)
            {
                var teacherExists = await _context.Teachers.AnyAsync(t => t.Id == substituteTeacherId.Value);
                if (!teacherExists)
                {
                    _logger.LogError("Substitute teacher ID {TeacherId} does not exist in database", substituteTeacherId.Value);
                    TempData["ErrorMessage"] = $"Substitute teacher with ID {substituteTeacherId.Value} does not exist.";
                    return RedirectToPage(new { id = absenceId });
                }
            }

            _logger.LogInformation("About to call AssignSubstituteAsync");

            // Assign substitution (pass null for assignedByUserId to avoid FK constraint)
            var substitution = await _substitutionService.AssignSubstituteAsync(
                absenceId,
                scheduledLessonId,
                substituteTeacherId,
                substitutionType,
                null,
                null // Pass null for now to avoid FK constraint - can be fixed later with proper auth
            );

            _logger.LogInformation("AssignSubstituteAsync completed successfully, substitution Id={SubstitutionId}", substitution.Id);

            // Send email notification if teacher substitute
            if (substituteTeacherId.HasValue && substitutionType == SubstitutionType.TeacherSubstitute)
            {
                var substituteTeacher = await _context.Teachers.FindAsync(substituteTeacherId.Value);
                if (substituteTeacher != null && !string.IsNullOrEmpty(substituteTeacher.Email))
                {
                    var emailSent = await _emailService.SendSubstituteAssignmentEmailAsync(
                        substituteTeacher.Email,
                        substituteTeacher.Name,
                        scheduledLesson.Lesson.LessonTeachers.FirstOrDefault()?.Teacher?.Name ?? "Unknown",
                        scheduledLesson.Lesson.LessonClasses.FirstOrDefault()?.Class?.Name ?? "Unknown",
                        scheduledLesson.Lesson.LessonSubjects.FirstOrDefault()?.Subject?.Name ?? "Unknown",
                        scheduledLesson.DayOfWeek,
                        $"{scheduledLesson.Period.StartTime:hh\\:mm} - {scheduledLesson.Period.EndTime:hh\\:mm}",
                        scheduledLesson.Room?.RoomNumber ??
                            (scheduledLesson.ScheduledLessonRooms.Any()
                                ? string.Join(", ", scheduledLesson.ScheduledLessonRooms.Select(slr => slr.Room?.RoomNumber))
                                : "TBA"),
                        absence.Date,
                        null
                    );

                    if (emailSent)
                    {
                        substitution.EmailSent = true;
                        substitution.EmailSentAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }

                    // TODO: Create in-app notification when authentication is implemented
                }
            }

            TempData["SuccessMessage"] = $"Substitute assigned successfully.";
            return RedirectToPage(new { id = absenceId });
        }
        catch (Exception ex)
        {
            var innerMessage = ex.InnerException?.Message ?? "No inner exception";
            var innerInnerMessage = ex.InnerException?.InnerException?.Message ?? "No inner inner exception";

            _logger.LogError(ex, "Error assigning substitute for absence {AbsenceId}, lesson {ScheduledLessonId}, teacher {TeacherId}, type {Type}. Inner: {Inner}. InnerInner: {InnerInner}",
                absenceId, scheduledLessonId, substituteTeacherId, assignmentType, innerMessage, innerInnerMessage);

            TempData["ErrorMessage"] = $"An error occurred while assigning the substitute: {ex.Message}\n\nInner Exception: {innerMessage}\n\nDetails: {innerInnerMessage}";
            return RedirectToPage(new { id = absenceId });
        }
    }

    public async Task<IActionResult> OnPostDeleteSubstitutionAsync(int absenceId, int substitutionId)
    {
        try
        {
            var substitution = await _context.Substitutions
                .Include(s => s.Absence)
                .FirstOrDefaultAsync(s => s.Id == substitutionId);

            if (substitution == null)
            {
                TempData["ErrorMessage"] = "Substitution not found.";
                return RedirectToPage(new { id = absenceId });
            }

            if (substitution.AbsenceId != absenceId)
            {
                TempData["ErrorMessage"] = "Substitution does not belong to this absence.";
                return RedirectToPage(new { id = absenceId });
            }

            _context.Substitutions.Remove(substitution);
            await _context.SaveChangesAsync();

            // Update absence status
            await _substitutionService.UpdateAbsenceStatusAsync(absenceId);

            TempData["SuccessMessage"] = "Substitution deleted successfully. You can now assign a different substitute.";
            return RedirectToPage(new { id = absenceId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting substitution {SubstitutionId}", substitutionId);
            TempData["ErrorMessage"] = $"An error occurred while deleting the substitution: {ex.Message}";
            return RedirectToPage(new { id = absenceId });
        }
    }

    public async Task<IActionResult> OnPostAssignSupervisionAsync(int absenceId, int dutyId, int? substituteTeacherId, string assignmentType)
    {
        try
        {
            _logger.LogInformation("OnPostAssignSupervisionAsync called with absenceId={AbsenceId}, dutyId={DutyId}, substituteTeacherId={SubstituteTeacherId}, assignmentType={AssignmentType}",
                absenceId, dutyId, substituteTeacherId, assignmentType);

            var substitutionType = assignmentType switch
            {
                "teacher" => SupervisionSubstitutionType.TeacherSubstitute,
                "cancelled" => SupervisionSubstitutionType.Cancelled,
                "combined" => SupervisionSubstitutionType.CombinedArea,
                _ => SupervisionSubstitutionType.TeacherSubstitute
            };

            // Get the absence
            var absence = await _context.Absences
                .Include(a => a.Teacher)
                .FirstOrDefaultAsync(a => a.Id == absenceId);

            if (absence == null)
            {
                TempData["ErrorMessage"] = "Absence not found.";
                return RedirectToPage(new { id = absenceId });
            }

            // Get the supervision duty
            var duty = await _context.BreakSupervisionDuties
                .Include(d => d.Room)
                .FirstOrDefaultAsync(d => d.Id == dutyId);

            if (duty == null)
            {
                TempData["ErrorMessage"] = "Supervision duty not found.";
                return RedirectToPage(new { id = absenceId });
            }

            // Check if substitution already exists
            var existingSubstitution = await _context.BreakSupervisionSubstitutions
                .FirstOrDefaultAsync(s => s.AbsenceId == absenceId && s.BreakSupervisionDutyId == dutyId);

            if (existingSubstitution != null)
            {
                TempData["ErrorMessage"] = "A substitution has already been assigned for this supervision duty.";
                return RedirectToPage(new { id = absenceId });
            }

            // Validate substitute teacher exists if provided
            Teacher? substituteTeacher = null;
            if (substituteTeacherId.HasValue)
            {
                substituteTeacher = await _context.Teachers.FindAsync(substituteTeacherId.Value);
                if (substituteTeacher == null)
                {
                    _logger.LogError("Substitute teacher ID {TeacherId} does not exist in database", substituteTeacherId.Value);
                    TempData["ErrorMessage"] = $"Substitute teacher with ID {substituteTeacherId.Value} does not exist.";
                    return RedirectToPage(new { id = absenceId });
                }
            }

            // Create the supervision substitution
            var supervisionSubstitution = new BreakSupervisionSubstitution
            {
                AbsenceId = absenceId,
                BreakSupervisionDutyId = dutyId,
                SubstituteTeacherId = substituteTeacherId,
                Type = substitutionType,
                Date = absence.Date,
                AssignedAt = DateTime.UtcNow,
                AssignedByUserId = null // Can be set when authentication is integrated
            };

            _context.BreakSupervisionSubstitutions.Add(supervisionSubstitution);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Supervision substitution created successfully, Id={SubstitutionId}", supervisionSubstitution.Id);

            // Send email notification if teacher substitute
            if (substituteTeacher != null && !string.IsNullOrEmpty(substituteTeacher.Email) && substitutionType == SupervisionSubstitutionType.TeacherSubstitute)
            {
                // TODO: Send supervision assignment email
                // Could be: await _emailService.SendSupervisionAssignmentEmailAsync(...);
            }

            var assignmentTypeDisplay = substitutionType switch
            {
                SupervisionSubstitutionType.TeacherSubstitute => substituteTeacher?.Name ?? "Unknown",
                SupervisionSubstitutionType.Cancelled => "Cancelled",
                SupervisionSubstitutionType.CombinedArea => "Combined Area",
                _ => "Unknown"
            };

            TempData["SuccessMessage"] = $"Supervision substitute assigned successfully: {assignmentTypeDisplay} for {duty.Room?.RoomNumber ?? "Unknown"} Period {duty.PeriodNumber}.";
            return RedirectToPage(new { id = absenceId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning supervision substitute for absence {AbsenceId}, duty {DutyId}, teacher {TeacherId}, type {Type}",
                absenceId, dutyId, substituteTeacherId, assignmentType);

            TempData["ErrorMessage"] = $"An error occurred while assigning the supervision substitute: {ex.Message}";
            return RedirectToPage(new { id = absenceId });
        }
    }

    public async Task<IActionResult> OnPostDeleteSupervisionSubstitutionAsync(int absenceId, int substitutionId)
    {
        try
        {
            var substitution = await _context.BreakSupervisionSubstitutions
                .FirstOrDefaultAsync(s => s.Id == substitutionId);

            if (substitution == null)
            {
                TempData["ErrorMessage"] = "Supervision substitution not found.";
                return RedirectToPage(new { id = absenceId });
            }

            if (substitution.AbsenceId != absenceId)
            {
                TempData["ErrorMessage"] = "Supervision substitution does not belong to this absence.";
                return RedirectToPage(new { id = absenceId });
            }

            _context.BreakSupervisionSubstitutions.Remove(substitution);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Supervision substitution deleted successfully. You can now assign a different substitute.";
            return RedirectToPage(new { id = absenceId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting supervision substitution {SubstitutionId}", substitutionId);
            TempData["ErrorMessage"] = $"An error occurred while deleting the supervision substitution: {ex.Message}";
            return RedirectToPage(new { id = absenceId });
        }
    }

    private async Task LoadDataAsync(int absenceId)
    {
        Absence = await _context.Absences
            .Include(a => a.Teacher)
            .Include(a => a.Substitutions)
            .FirstOrDefaultAsync(a => a.Id == absenceId) ?? new Absence();

        if (Absence.Id == 0)
            return;

        // Get the published timetable for schedule links
        var publishedTimetable = await _context.Timetables
            .Where(t => t.Status == TimetableStatus.Published)
            .OrderByDescending(t => t.CreatedDate)
            .FirstOrDefaultAsync();
        PublishedTimetableId = publishedTimetable?.Id;

        // Get affected lessons
        var affectedLessons = await _substitutionService.GetAffectedLessonsAsync(absenceId);

        // Get existing substitutions
        var existingSubstitutions = Absence.Substitutions.ToDictionary(s => s.ScheduledLessonId);

        AffectedLessons = new List<AffectedLessonViewModel>();

        foreach (var lesson in affectedLessons)
        {
            var hasSubstitution = existingSubstitutions.ContainsKey(lesson.Id);
            var substitution = hasSubstitution ? existingSubstitutions[lesson.Id] : null;

            // Find available substitutes for this time slot
            var availableSubstitutes = new List<SubstituteCandidate>();

            if (!hasSubstitution)
            {
                // Use the new ranking algorithm
                var rankedCandidates = await _substitutionService.RankSubstitutesAsync(
                    Absence.TeacherId,
                    lesson.Id
                );

                // Get teacher IDs for availability lookup
                var teacherIds = rankedCandidates.Select(c => c.Teacher.Id).ToList();

                // Load availability preferences for this time slot
                var availabilities = await _context.TeacherAvailabilities
                    .Where(a => teacherIds.Contains(a.TeacherId) &&
                               a.DayOfWeek == lesson.DayOfWeek &&
                               a.PeriodId == lesson.PeriodId)
                    .ToDictionaryAsync(a => a.TeacherId, a => a);

                foreach (var candidate in rankedCandidates)
                {
                    availabilities.TryGetValue(candidate.Teacher.Id, out var availability);

                    availableSubstitutes.Add(new SubstituteCandidate
                    {
                        TeacherId = candidate.Teacher.Id,
                        Name = candidate.Teacher.Name,
                        Email = candidate.Teacher.Email,
                        IsQualified = candidate.IsQualified,
                        SubstitutionsThisWeek = candidate.SubstitutionsThisWeek,
                        MatchScore = candidate.MatchScore,
                        MatchReasons = candidate.MatchReasons,
                        DepartmentName = candidate.DepartmentName,
                        IsSameDepartment = candidate.IsSameDepartment,
                        IsOnSubstitutionReserve = candidate.IsOnSubstitutionReserve,
                        AvailabilityImportance = availability?.Importance,
                        AvailabilityReason = availability?.Reason
                    });
                }

                // Apply sorting based on user selection
                availableSubstitutes = SortBy switch
                {
                    "score" => availableSubstitutes.OrderByDescending(c => c.MatchScore).ThenBy(c => c.SubstitutionsThisWeek).ToList(),
                    "workload" => availableSubstitutes.OrderBy(c => c.SubstitutionsThisWeek).ThenByDescending(c => c.MatchScore).ToList(),
                    "name" => availableSubstitutes.OrderBy(c => c.Name).ToList(),
                    "qualified" => availableSubstitutes.OrderByDescending(c => c.IsQualified).ThenByDescending(c => c.MatchScore).ToList(),
                    "reserve" => availableSubstitutes.OrderByDescending(c => c.IsOnSubstitutionReserve).ThenByDescending(c => c.MatchScore).ToList(),
                    _ => availableSubstitutes.OrderByDescending(c => c.MatchScore).ThenBy(c => c.SubstitutionsThisWeek).ToList()
                };
            }

            var viewModel = new AffectedLessonViewModel
            {
                ScheduledLessonId = lesson.Id,
                DayOfWeek = lesson.DayOfWeek.ToString(),
                PeriodId = lesson.PeriodId,
                PeriodTime = lesson.Period != null ? $"{lesson.Period.StartTime:hh\\:mm} - {lesson.Period.EndTime:hh\\:mm}" : "N/A",
                Subject = lesson.Lesson?.LessonSubjects.FirstOrDefault()?.Subject?.Name ?? "Unknown",
                Class = lesson.Lesson?.LessonClasses.FirstOrDefault()?.Class?.Name ?? "Unknown",
                Room = lesson.Room?.RoomNumber ??
                       (lesson.ScheduledLessonRooms.Any()
                           ? string.Join(", ", lesson.ScheduledLessonRooms.Select(slr => slr.Room?.RoomNumber))
                           : "TBA"),
                IsAssigned = hasSubstitution,
                SubstitutionId = substitution?.Id,
                AssignedSubstitute = substitution != null
                    ? (substitution.SubstituteTeacher?.Name ?? substitution.Type.ToString())
                    : null,
                AvailableSubstitutes = availableSubstitutes
            };

            AffectedLessons.Add(viewModel);
        }

        // Sort affected lessons by period number
        AffectedLessons = AffectedLessons.OrderBy(l => l.PeriodId).ToList();

        // Load affected break supervision duties
        await LoadAffectedSupervisionDutiesAsync();
    }

    private async Task LoadAffectedSupervisionDutiesAsync()
    {
        if (Absence.Id == 0) return;

        var dayOfWeek = Absence.Date.DayOfWeek;

        // Get published timetable first for filtering
        var publishedTimetable = await _context.Timetables
            .Where(t => t.Status == TimetableStatus.Published)
            .OrderByDescending(t => t.CreatedDate)
            .FirstOrDefaultAsync();

        if (publishedTimetable == null)
        {
            AffectedSupervisionDuties = new List<AffectedSupervisionViewModel>();
            return;
        }

        // Find supervision duties for the absent teacher on this day (filtered by timetable)
        var affectedDuties = await _context.BreakSupervisionDuties
            .Include(d => d.Room)
            .Where(d => d.TeacherId == Absence.TeacherId &&
                       d.DayOfWeek == dayOfWeek &&
                       d.IsActive &&
                       d.TimetableId == publishedTimetable.Id)
            .OrderBy(d => d.PeriodNumber)
            .ToListAsync();

        if (!affectedDuties.Any())
        {
            AffectedSupervisionDuties = new List<AffectedSupervisionViewModel>();
            return;
        }

        // Get existing supervision substitutions for this absence
        var existingSubstitutions = await _context.BreakSupervisionSubstitutions
            .Include(s => s.SubstituteTeacher)
            .Where(s => s.AbsenceId == Absence.Id)
            .ToDictionaryAsync(s => s.BreakSupervisionDutyId);

        // Get all teachers for finding substitutes (exclude absent teacher)
        var allTeachers = await _context.Teachers
            .Include(t => t.Department)
            .Where(t => t.Id != Absence.TeacherId && t.IsActive)
            .ToListAsync();

        var scheduledLessonsOnDay = await _context.ScheduledLessons
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l.LessonTeachers)
            .Where(sl => sl.TimetableId == publishedTimetable.Id &&
                        sl.DayOfWeek == dayOfWeek)
            .ToListAsync();

        // Get existing supervision duties for all teachers on this day (filtered by timetable)
        var existingDuties = await _context.BreakSupervisionDuties
            .Where(d => d.DayOfWeek == dayOfWeek && d.IsActive && d.TeacherId.HasValue &&
                       d.TimetableId == publishedTimetable.Id)
            .ToListAsync();

        // Get week start for counting this week's supervisions
        var weekStart = Absence.Date.AddDays(-(int)Absence.Date.DayOfWeek);
        var weekEnd = weekStart.AddDays(7);

        AffectedSupervisionDuties = new List<AffectedSupervisionViewModel>();

        foreach (var duty in affectedDuties)
        {
            var hasSubstitution = existingSubstitutions.ContainsKey(duty.Id);
            var substitution = hasSubstitution ? existingSubstitutions[duty.Id] : null;

            var viewModel = new AffectedSupervisionViewModel
            {
                DutyId = duty.Id,
                PeriodNumber = duty.PeriodNumber,
                Location = duty.Room?.RoomNumber ?? "Unknown",
                RoomId = duty.RoomId,
                IsAssigned = hasSubstitution,
                SubstitutionId = substitution?.Id,
                AssignedSubstituteName = substitution != null
                    ? (substitution.SubstituteTeacher?.Name ?? substitution.Type.ToString())
                    : null,
                SubstitutionType = substitution?.Type,
                AvailableSubstitutes = new List<SupervisionSubstituteCandidate>()
            };

            // Find available substitutes for this supervision period
            foreach (var teacher in allTeachers)
            {
                // Check if teacher has a lesson during this period
                var hasLessonThisPeriod = scheduledLessonsOnDay.Any(sl =>
                    sl.PeriodId == duty.PeriodNumber &&
                    sl.Lesson?.LessonTeachers.Any(lt => lt.TeacherId == teacher.Id) == true);

                // Check if teacher already has supervision duty this period
                var hasSupervisionThisPeriod = existingDuties.Any(d =>
                    d.TeacherId == teacher.Id &&
                    d.PeriodNumber == duty.PeriodNumber &&
                    d.Id != duty.Id); // Exclude the current duty

                // Count supervisions this week for workload balancing
                var supervisionsThisWeek = existingDuties.Count(d => d.TeacherId == teacher.Id);

                // Calculate match score
                var matchScore = 0;
                var matchReasons = new List<string>();

                if (!hasLessonThisPeriod && !hasSupervisionThisPeriod)
                {
                    matchScore += 100;
                    matchReasons.Add("Free this period");
                }
                else if (hasLessonThisPeriod)
                {
                    matchReasons.Add("Has lesson");
                }
                else if (hasSupervisionThisPeriod)
                {
                    matchReasons.Add("Already supervising");
                }

                // Prefer teachers with fewer supervisions
                if (supervisionsThisWeek == 0)
                {
                    matchScore += 30;
                    matchReasons.Add("No other supervisions");
                }
                else if (supervisionsThisWeek <= 2)
                {
                    matchScore += 15;
                    matchReasons.Add($"Low workload ({supervisionsThisWeek})");
                }

                // Same department bonus
                if (teacher.DepartmentId == Absence.Teacher?.DepartmentId && teacher.DepartmentId.HasValue)
                {
                    matchScore += 20;
                    matchReasons.Add("Same department");
                }

                viewModel.AvailableSubstitutes.Add(new SupervisionSubstituteCandidate
                {
                    TeacherId = teacher.Id,
                    Name = teacher.Name,
                    Email = teacher.Email,
                    DepartmentName = teacher.Department?.Name,
                    SupervisionsThisWeek = supervisionsThisWeek,
                    HasSupervisionDutyThisPeriod = hasSupervisionThisPeriod,
                    IsFreeThisPeriod = !hasLessonThisPeriod && !hasSupervisionThisPeriod,
                    MatchScore = matchScore,
                    MatchReasons = matchReasons
                });
            }

            // Sort by match score (best candidates first), then by workload
            viewModel.AvailableSubstitutes = viewModel.AvailableSubstitutes
                .OrderByDescending(c => c.MatchScore)
                .ThenBy(c => c.SupervisionsThisWeek)
                .ThenBy(c => c.Name)
                .ToList();

            AffectedSupervisionDuties.Add(viewModel);
        }

        // Populate SupervisionPeriods for GetBreakLabel helper
        SupervisionPeriods = AffectedSupervisionDuties.Select(d => d.PeriodNumber)
            .Distinct()
            .OrderBy(p => p)
            .ToList();

        // If no affected duties, load all possible periods from DB (filtered by timetable)
        if (!SupervisionPeriods.Any() && PublishedTimetableId.HasValue)
        {
            SupervisionPeriods = await _context.BreakSupervisionDuties
                .Where(d => d.TimetableId == PublishedTimetableId)
                .Select(d => d.PeriodNumber)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();
        }
    }
}
