using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;
using SchedulingSystem.Services;

namespace SchedulingSystem.Pages.Admin.Substitutions;

public class DailyModel : PageModel
{
    private readonly SubstitutionService _substitutionService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DailyModel> _logger;

    public DailyModel(SubstitutionService substitutionService, ApplicationDbContext context, ILogger<DailyModel> logger)
    {
        _substitutionService = substitutionService;
        _context = context;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime? Date { get; set; }

    public DateTime SelectedDate { get; set; }
    public List<SubstitutionViewModel> Substitutions { get; set; } = new();
    public Dictionary<int, List<SubstitutionViewModel>> SubstitutionsByPeriod { get; set; } = new();
    public Dictionary<string, List<SubstitutionViewModel>> SubstitutionsByTeacher { get; set; } = new();
    public Dictionary<string, List<SubstitutionViewModel>> SubstitutionsBySubstitute { get; set; } = new();
    public List<UncoveredLessonViewModel> UncoveredLessons { get; set; } = new();
    public List<AbsenceViewModel> Absences { get; set; } = new();
    public int? PublishedTimetableId { get; set; }

    public class SubstitutionViewModel
    {
        public int Id { get; set; }
        public string PeriodTime { get; set; } = string.Empty;
        public int PeriodNumber { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public string Room { get; set; } = string.Empty;
        public string OriginalTeacher { get; set; } = string.Empty;
        public string? SubstituteTeacher { get; set; }
        public SubstitutionType Type { get; set; }
        public string TypeDisplay { get; set; } = string.Empty;
        public bool EmailSent { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
    }

    public class UncoveredLessonViewModel
    {
        public int AbsenceId { get; set; }
        public int ScheduledLessonId { get; set; }
        public string PeriodTime { get; set; } = string.Empty;
        public int PeriodNumber { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string SubjectColor { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public string Room { get; set; } = string.Empty;
        public string AbsentTeacher { get; set; } = string.Empty;
        public string AbsenceType { get; set; } = string.Empty;
    }

    public class AbsenceViewModel
    {
        public int Id { get; set; }
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public AbsenceStatus StatusEnum { get; set; }
        public string? Notes { get; set; }
        public int TotalLessons { get; set; }
        public int CoveredLessons { get; set; }
        public int UncoveredLessons { get; set; }
        public List<AffectedLessonViewModel> AffectedLessons { get; set; } = new();
    }

    public class AffectedLessonViewModel
    {
        public int ScheduledLessonId { get; set; }
        public string PeriodName { get; set; } = string.Empty;
        public string PeriodTime { get; set; } = string.Empty;
        public int PeriodNumber { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string SubjectColor { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public string Room { get; set; } = string.Empty;
        public bool HasSubstitution { get; set; }
        public string? SubstituteName { get; set; }
        public string? SubstitutionType { get; set; }
    }

    public class UncoveredSupervisionViewModel
    {
        public int DutyId { get; set; }
        public int AbsenceId { get; set; }
        public string AbsentTeacher { get; set; } = string.Empty;
        public int PeriodNumber { get; set; }
        public string Location { get; set; } = string.Empty;
        public string AbsenceType { get; set; } = string.Empty;
    }

    public class CoveredSupervisionViewModel
    {
        public int SubstitutionId { get; set; }
        public int DutyId { get; set; }
        public string AbsentTeacher { get; set; } = string.Empty;
        public int PeriodNumber { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? SubstituteTeacher { get; set; }
        public SupervisionSubstitutionType Type { get; set; }
        public string TypeDisplay { get; set; } = string.Empty;
    }

    public List<UncoveredSupervisionViewModel> UncoveredSupervisionDuties { get; set; } = new();
    public List<CoveredSupervisionViewModel> CoveredSupervisionDuties { get; set; } = new();
    public List<int> SupervisionPeriods { get; set; } = new();

    public async Task OnGetAsync()
    {
        SelectedDate = Date ?? DateTime.Today;
        await LoadDataAsync();
    }

    /// <summary>
    /// Helper method to get room display string (supports multi-room lessons)
    /// </summary>
    private string GetRoomDisplay(ScheduledLesson? scheduledLesson)
    {
        if (scheduledLesson == null) return "TBA";

        // Check legacy RoomId first
        if (scheduledLesson.Room != null)
            return scheduledLesson.Room.RoomNumber;

        // Check multi-room assignments
        if (scheduledLesson.ScheduledLessonRooms.Any())
            return string.Join(", ", scheduledLesson.ScheduledLessonRooms.Select(slr => slr.Room?.RoomNumber));

        return "TBA";
    }

    private async Task LoadDataAsync()
    {
        var substitutions = await _substitutionService.GetDailySubstitutionsAsync(SelectedDate);

        Substitutions = substitutions.Select(s => new SubstitutionViewModel
        {
            Id = s.Id,
            PeriodTime = s.ScheduledLesson?.Period != null ? $"{s.ScheduledLesson.Period.StartTime:hh\\:mm} - {s.ScheduledLesson.Period.EndTime:hh\\:mm}" : "N/A",
            PeriodNumber = s.ScheduledLesson?.Period?.PeriodNumber ?? 0,
            Subject = s.ScheduledLesson?.Lesson?.LessonSubjects.FirstOrDefault()?.Subject?.Name ?? "Unknown",
            Class = s.ScheduledLesson?.Lesson?.LessonClasses.FirstOrDefault()?.Class?.Name ?? "Unknown",
            Room = GetRoomDisplay(s.ScheduledLesson),
            OriginalTeacher = s.Absence?.Teacher?.Name ?? "Unknown",
            SubstituteTeacher = s.SubstituteTeacher?.Name,
            Type = s.Type,
            TypeDisplay = s.Type switch
            {
                SubstitutionType.TeacherSubstitute => s.SubstituteTeacher?.Name ?? "Teacher",
                SubstitutionType.SelfStudy => "Self-Study",
                SubstitutionType.Cancelled => "Cancelled",
                SubstitutionType.RoomChange => "Room Change",
                SubstitutionType.Rescheduled => "Rescheduled",
                SubstitutionType.ClassMerger => "Class Merger",
                _ => s.Type.ToString()
            },
            EmailSent = s.EmailSent,
            DayOfWeek = s.ScheduledLesson.DayOfWeek.ToString()
        }).ToList();

        // Group by period
        SubstitutionsByPeriod = Substitutions
            .GroupBy(s => s.PeriodNumber)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Group by absent teacher (original teacher), sorted by teacher name, then by period
        SubstitutionsByTeacher = Substitutions
            .GroupBy(s => s.OriginalTeacher)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.PeriodNumber).ToList());

        // Group by substitute teacher, sorted by substitute name, then by period
        // Use "Self-Study", "Cancelled", etc. for non-teacher substitutions
        SubstitutionsBySubstitute = Substitutions
            .GroupBy(s => s.SubstituteTeacher ?? s.TypeDisplay)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.PeriodNumber).ToList());

        // Load uncovered lessons (absences without substitutions)
        await LoadUncoveredLessonsAsync();

        // Load absences for the selected date
        await LoadAbsencesAsync();

        // Load uncovered break supervision duties
        await LoadUncoveredSupervisionDutiesAsync();
    }

    private async Task LoadUncoveredLessonsAsync()
    {
        var dayOfWeek = SelectedDate.DayOfWeek;

        // Get all absences for the selected date
        var absences = await _context.Absences
            .Include(a => a.Teacher)
            .Include(a => a.Substitutions)
            .Where(a => a.Date.Date == SelectedDate.Date)
            .ToListAsync();

        if (!absences.Any())
        {
            return;
        }

        // Get the published timetable
        var timetable = await _context.Timetables
            .Where(t => t.Status == TimetableStatus.Published)
            .OrderByDescending(t => t.CreatedDate)
            .FirstOrDefaultAsync();

        if (timetable == null)
        {
            return;
        }

        var uncoveredList = new List<UncoveredLessonViewModel>();

        foreach (var absence in absences)
        {
            // Get affected lessons for this absence
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
                .Where(sl => sl.TimetableId == timetable.Id &&
                            sl.Lesson!.LessonTeachers.Any(lt => lt.TeacherId == absence.TeacherId) &&
                            sl.DayOfWeek == dayOfWeek)
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

            // Get substituted lesson IDs for this absence
            var substitutedLessonIds = absence.Substitutions
                .Select(s => s.ScheduledLessonId)
                .ToHashSet();

            // Find lessons without substitutions
            var uncovered = affectedLessons
                .Where(sl => !substitutedLessonIds.Contains(sl.Id))
                .Select(sl => new UncoveredLessonViewModel
                {
                    AbsenceId = absence.Id,
                    ScheduledLessonId = sl.Id,
                    PeriodTime = sl.Period != null ? $"{sl.Period.StartTime:hh\\:mm} - {sl.Period.EndTime:hh\\:mm}" : "N/A",
                    PeriodNumber = sl.Period?.PeriodNumber ?? 0,
                    Subject = sl.Lesson?.LessonSubjects.FirstOrDefault()?.Subject?.Name ?? "Unknown",
                    SubjectColor = sl.Lesson?.LessonSubjects.FirstOrDefault()?.Subject?.Color ?? "#007bff",
                    Class = sl.Lesson?.LessonClasses.FirstOrDefault()?.Class?.Name ?? "Unknown",
                    Room = GetRoomDisplay(sl),
                    AbsentTeacher = absence.Teacher.Name,
                    AbsenceType = absence.Type.ToString()
                });

            uncoveredList.AddRange(uncovered);
        }

        UncoveredLessons = uncoveredList.OrderBy(u => u.PeriodNumber).ToList();
    }

    private async Task LoadAbsencesAsync()
    {
        var dayOfWeek = SelectedDate.DayOfWeek;

        // Get the latest published timetable
        var timetable = await _context.Timetables
            .Where(t => t.Status == TimetableStatus.Published)
            .OrderByDescending(t => t.CreatedDate)
            .FirstOrDefaultAsync();

        // Store the timetable ID for links
        PublishedTimetableId = timetable?.Id;

        // Get all absences for the selected date
        var absences = await _context.Absences
            .Include(a => a.Teacher)
            .Include(a => a.Substitutions)
            .Where(a => a.Date.Date == SelectedDate.Date)
            .OrderBy(a => a.Teacher.FirstName)
            .ThenBy(a => a.Teacher.LastName)
            .ToListAsync();

        if (!absences.Any())
        {
            return;
        }

        var absenceViewModels = new List<AbsenceViewModel>();

        foreach (var absence in absences)
        {
            int totalLessons = 0;
            int coveredLessons = absence.Substitutions.Count;
            var affectedLessonViewModels = new List<AffectedLessonViewModel>();

            if (timetable != null)
            {
                // Get affected lessons for this absence
                var affectedLessons = await _context.ScheduledLessons
                    .Include(sl => sl.Period)
                    .Include(sl => sl.Lesson)
                        .ThenInclude(l => l!.LessonSubjects)
                            .ThenInclude(ls => ls.Subject)
                    .Include(sl => sl.Lesson)
                        .ThenInclude(l => l!.LessonClasses)
                            .ThenInclude(lc => lc.Class)
                    .Include(sl => sl.Lesson)
                        .ThenInclude(l => l!.LessonTeachers)
                            .ThenInclude(lt => lt.Teacher)
                    .Include(sl => sl.Room)
                    .Where(sl => sl.TimetableId == timetable.Id &&
                                sl.Lesson != null &&
                                sl.Lesson.LessonTeachers.Any(lt => lt.TeacherId == absence.TeacherId) &&
                                sl.DayOfWeek == dayOfWeek)
                    .OrderBy(sl => sl.Period!.PeriodNumber)
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

                totalLessons = affectedLessons.Count;

                // Create affected lesson view models with substitution info
                foreach (var lesson in affectedLessons)
                {
                    var substitution = absence.Substitutions
                        .FirstOrDefault(s => s.ScheduledLessonId == lesson.Id);

                    // Load substitute teacher info if exists
                    string? substituteName = null;
                    string? substitutionType = null;

                    if (substitution != null)
                    {
                        if (substitution.SubstituteTeacherId.HasValue)
                        {
                            var subTeacher = await _context.Teachers
                                .Where(t => t.Id == substitution.SubstituteTeacherId.Value)
                                .Select(t => t.Name)
                                .FirstOrDefaultAsync();
                            substituteName = subTeacher;
                        }
                        substitutionType = substitution.Type.ToString();
                    }

                    affectedLessonViewModels.Add(new AffectedLessonViewModel
                    {
                        ScheduledLessonId = lesson.Id,
                        PeriodName = lesson.Period?.Name ?? "Unknown",
                        PeriodTime = lesson.Period != null
                            ? $"{lesson.Period.StartTime:hh\\:mm} - {lesson.Period.EndTime:hh\\:mm}"
                            : "N/A",
                        PeriodNumber = lesson.Period?.PeriodNumber ?? 0,
                        Subject = lesson.Lesson?.LessonSubjects.FirstOrDefault()?.Subject?.Name ?? "Unknown",
                        SubjectColor = lesson.Lesson?.LessonSubjects.FirstOrDefault()?.Subject?.Color ?? "#007bff",
                        Class = lesson.Lesson?.LessonClasses.FirstOrDefault()?.Class?.Name ?? "Unknown",
                        Room = GetRoomDisplay(lesson),
                        HasSubstitution = substitution != null,
                        SubstituteName = substituteName,
                        SubstitutionType = substitutionType
                    });
                }
            }

            absenceViewModels.Add(new AbsenceViewModel
            {
                Id = absence.Id,
                TeacherId = absence.TeacherId,
                TeacherName = absence.Teacher.Name,
                StartTime = absence.StartTime,
                EndTime = absence.EndTime,
                Type = absence.Type.ToString(),
                Status = absence.Status.ToString(),
                StatusEnum = absence.Status,
                Notes = absence.Notes,
                TotalLessons = totalLessons,
                CoveredLessons = coveredLessons,
                UncoveredLessons = totalLessons - coveredLessons,
                AffectedLessons = affectedLessonViewModels
            });
        }

        Absences = absenceViewModels;
    }

    /// <summary>
    /// Loads break supervision duties for absent teachers that need coverage.
    /// </summary>
    private async Task LoadUncoveredSupervisionDutiesAsync()
    {
        var dayOfWeek = SelectedDate.DayOfWeek;

        // Get all absences for the selected date
        var absences = await _context.Absences
            .Include(a => a.Teacher)
            .Include(a => a.SupervisionSubstitutions)
                .ThenInclude(ss => ss.SubstituteTeacher)
            .Include(a => a.SupervisionSubstitutions)
                .ThenInclude(ss => ss.BreakSupervisionDuty)
                    .ThenInclude(d => d.Room)
            .Where(a => a.Date.Date == SelectedDate.Date)
            .ToListAsync();

        if (!absences.Any())
        {
            return;
        }

        var uncoveredList = new List<UncoveredSupervisionViewModel>();
        var coveredList = new List<CoveredSupervisionViewModel>();

        foreach (var absence in absences)
        {
            // Get substituted duty IDs for this absence
            var substitutedDutyIds = absence.SupervisionSubstitutions
                .Select(s => s.BreakSupervisionDutyId)
                .ToHashSet();

            // Add covered supervision duties
            foreach (var sub in absence.SupervisionSubstitutions)
            {
                coveredList.Add(new CoveredSupervisionViewModel
                {
                    SubstitutionId = sub.Id,
                    DutyId = sub.BreakSupervisionDutyId,
                    AbsentTeacher = absence.Teacher.Name,
                    PeriodNumber = sub.BreakSupervisionDuty?.PeriodNumber ?? 0,
                    Location = sub.BreakSupervisionDuty?.Room?.RoomNumber ?? "Unknown",
                    SubstituteTeacher = sub.SubstituteTeacher?.Name,
                    Type = sub.Type,
                    TypeDisplay = sub.Type switch
                    {
                        SupervisionSubstitutionType.TeacherSubstitute => sub.SubstituteTeacher?.Name ?? "Teacher",
                        SupervisionSubstitutionType.Cancelled => "Cancelled",
                        SupervisionSubstitutionType.CombinedArea => "Combined Area",
                        _ => sub.Type.ToString()
                    }
                });
            }

            // Get break supervision duties for this teacher on this day (filtered by published timetable)
            var duties = await _context.BreakSupervisionDuties
                .Include(d => d.Room)
                .Where(d => d.TeacherId == absence.TeacherId &&
                           d.DayOfWeek == dayOfWeek &&
                           d.IsActive &&
                           d.TimetableId == PublishedTimetableId)
                .ToListAsync();

            // Filter by time if partial day absence
            // Note: Supervision periods are designated by period number
            // For now, include all duties for the day if there's any overlap with absence time
            // Future enhancement: Map periods to times and filter accordingly

            foreach (var duty in duties)
            {
                // Skip if this duty has been covered
                if (substitutedDutyIds.Contains(duty.Id))
                    continue;

                uncoveredList.Add(new UncoveredSupervisionViewModel
                {
                    DutyId = duty.Id,
                    AbsenceId = absence.Id,
                    AbsentTeacher = absence.Teacher.Name,
                    PeriodNumber = duty.PeriodNumber,
                    Location = duty.Room?.RoomNumber ?? "Unknown",
                    AbsenceType = absence.Type.ToString()
                });
            }
        }

        UncoveredSupervisionDuties = uncoveredList
            .OrderBy(d => d.PeriodNumber)
            .ThenBy(d => d.Location)
            .ToList();

        CoveredSupervisionDuties = coveredList
            .OrderBy(d => d.PeriodNumber)
            .ThenBy(d => d.Location)
            .ToList();

        // Build list of unique supervision periods for GetBreakLabel
        SupervisionPeriods = UncoveredSupervisionDuties.Select(d => d.PeriodNumber)
            .Concat(CoveredSupervisionDuties.Select(d => d.PeriodNumber))
            .Distinct()
            .OrderBy(p => p)
            .ToList();

        // If no duties loaded, load from DB to get all possible periods for this timetable
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

    // Convert supervision period number to "Break 1", "Break 2", etc.
    public string GetBreakLabel(int periodNumber)
    {
        var index = SupervisionPeriods.IndexOf(periodNumber);
        return index >= 0 ? $"Break {index + 1}" : "Break";
    }

    /// <summary>
    /// Deletes a substitution assignment and updates the related absence status.
    /// This allows users to undo assignments and reassign different substitutes.
    /// </summary>
    public async Task<IActionResult> OnPostDeleteSubstitutionAsync(int substitutionId, DateTime? selectedDate = null)
    {
        try
        {
            var substitution = await _context.Substitutions
                .Include(s => s.Absence)
                .FirstOrDefaultAsync(s => s.Id == substitutionId);

            if (substitution == null)
            {
                TempData["ErrorMessage"] = "Substitution not found.";
                return RedirectToPage(new { selectedDate });
            }

            _context.Substitutions.Remove(substitution);
            await _context.SaveChangesAsync();

            // Update absence status using substitution service
            var substitutionService = HttpContext.RequestServices.GetRequiredService<SubstitutionService>();
            await substitutionService.UpdateAbsenceStatusAsync(substitution.AbsenceId);

            TempData["SuccessMessage"] = "Substitution deleted successfully.";
            return RedirectToPage(new { selectedDate });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting substitution {SubstitutionId}", substitutionId);
            TempData["ErrorMessage"] = $"An error occurred while deleting the substitution: {ex.Message}";
            return RedirectToPage(new { selectedDate });
        }
    }
}
