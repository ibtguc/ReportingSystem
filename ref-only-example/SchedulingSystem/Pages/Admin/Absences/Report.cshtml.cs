using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;
using SchedulingSystem.Services;

namespace SchedulingSystem.Pages.Admin.Absences;

public class ReportModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly SubstitutionService _substitutionService;
    private readonly ILogger<ReportModel> _logger;

    public ReportModel(
        ApplicationDbContext context,
        SubstitutionService substitutionService,
        ILogger<ReportModel> logger)
    {
        _context = context;
        _substitutionService = substitutionService;
        _logger = logger;
    }

    [BindProperty]
    public AbsenceInput Input { get; set; } = new();

    public List<SelectListItem> Teachers { get; set; } = new();
    public List<SelectListItem> AbsenceTypes { get; set; } = new();
    public List<ScheduledLessonInfo> TeacherSchedule { get; set; } = new();
    public List<SupervisionDutyInfo> TeacherSupervisionDuties { get; set; } = new();

    public class ScheduledLessonInfo
    {
        public int PeriodNumber { get; set; }
        public string PeriodTime { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string SubjectName { get; set; } = "";
        public string RoomNumber { get; set; } = "";
    }

    public class SupervisionDutyInfo
    {
        public int PeriodNumber { get; set; }
        public string Location { get; set; } = "";
    }

    public class AbsenceInput
    {
        [Required(ErrorMessage = "Please select a teacher")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a teacher")]
        public int TeacherId { get; set; }

        [Required(ErrorMessage = "Date is required")]
        public DateTime Date { get; set; } = DateTime.Today;

        public bool IsAllDay { get; set; } = true;
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }

        [Required(ErrorMessage = "Absence type is required")]
        public AbsenceType Type { get; set; }

        public string? Notes { get; set; }
    }

    public async Task OnGetAsync(int? teacherId = null, DateTime? date = null)
    {
        await LoadDataAsync();

        // Load teacher schedule if parameters provided
        if (teacherId.HasValue && date.HasValue)
        {
            Input.TeacherId = teacherId.Value;
            Input.Date = date.Value;
            await LoadTeacherScheduleAsync(teacherId.Value, date.Value);
            await LoadTeacherSupervisionDutiesAsync(teacherId.Value, date.Value);
        }
    }

    public async Task<IActionResult> OnGetScheduleAsync(int teacherId, string date)
    {
        if (!DateTime.TryParse(date, out var parsedDate))
        {
            return new JsonResult(new { error = "Invalid date format" });
        }

        await LoadTeacherScheduleAsync(teacherId, parsedDate);
        await LoadTeacherSupervisionDutiesAsync(teacherId, parsedDate);

        return new JsonResult(new {
            success = true,
            schedule = TeacherSchedule,
            supervisionDuties = TeacherSupervisionDuties
        });
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadDataAsync();
            return Page();
        }

        try
        {
            // Validate time range if partial day
            if (!Input.IsAllDay)
            {
                if (!Input.StartTime.HasValue || !Input.EndTime.HasValue)
                {
                    ModelState.AddModelError("", "Start time and end time are required for partial day absence.");
                    await LoadDataAsync();
                    return Page();
                }

                if (Input.StartTime >= Input.EndTime)
                {
                    ModelState.AddModelError("", "End time must be after start time.");
                    await LoadDataAsync();
                    return Page();
                }
            }

            // Get current user ID if authenticated
            string? currentUserId = null;
            // if (User.Identity?.IsAuthenticated == true)
            // {
            //     // Get user ID from claims
            //     currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            // }

            // Create the absence
            var absence = await _substitutionService.CreateAbsenceAsync(
                Input.TeacherId,
                Input.Date,
                Input.IsAllDay ? null : Input.StartTime,
                Input.IsAllDay ? null : Input.EndTime,
                Input.Type,
                Input.Notes,
                currentUserId // Pass actual user ID or null
            );

            TempData["SuccessMessage"] = $"Absence reported successfully. {absence.TotalHours:F1} hours recorded.";

            // Redirect to Find Substitutes page
            return RedirectToPage("/Admin/Absences/FindSubstitutes", new { id = absence.Id });
        }
        catch (Exception ex)
        {
            // Log detailed error information
            _logger.LogError(ex, "Error creating absence for TeacherId={TeacherId}, Date={Date}, Type={Type}. " +
                "Exception: {ExceptionType}, Message: {Message}, InnerException: {InnerException}",
                Input.TeacherId, Input.Date, Input.Type,
                ex.GetType().Name, ex.Message, ex.InnerException?.Message ?? "None");

            // Show detailed error to user in development, generic in production
            var errorMessage = $"Error: {ex.Message}";
            if (ex.InnerException != null)
            {
                errorMessage += $" | Inner: {ex.InnerException.Message}";
            }

            ModelState.AddModelError("", $"An error occurred while reporting the absence: {errorMessage}");
            await LoadDataAsync();
            return Page();
        }
    }

    private async Task LoadDataAsync()
    {
        // Load teachers
        var teachers = await _context.Teachers
            .OrderBy(t => t.FirstName)
            .ThenBy(t => t.LastName)
            .Select(t => new { t.Id, t.FirstName, t.LastName })
            .ToListAsync();

        Teachers = teachers.Select(t => new SelectListItem
        {
            Value = t.Id.ToString(),
            Text = !string.IsNullOrEmpty(t.LastName) ? $"{t.FirstName} {t.LastName}" : t.FirstName
        }).ToList();

        // Load absence types
        AbsenceTypes = Enum.GetValues<AbsenceType>()
            .Select(at => new SelectListItem
            {
                Value = ((int)at).ToString(),
                Text = at.ToString()
            })
            .ToList();
    }

    private async Task LoadTeacherScheduleAsync(int teacherId, DateTime date)
    {
        // Get the day of week
        var dayOfWeek = date.DayOfWeek;

        // Get the latest published timetable
        var timetable = await _context.Timetables
            .Where(t => t.Status == TimetableStatus.Published)
            .OrderByDescending(t => t.CreatedDate)
            .FirstOrDefaultAsync();

        if (timetable == null)
        {
            TeacherSchedule = new List<ScheduledLessonInfo>();
            return;
        }

        // Query scheduled lessons for this teacher on this day from the published timetable
        var lessons = await _context.ScheduledLessons
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l != null ? l.LessonSubjects : null)
                    .ThenInclude(ls => ls.Subject)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l != null ? l.LessonClasses : null)
                    .ThenInclude(lc => lc.Class)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l != null ? l.LessonTeachers : null)
            .Include(sl => sl.Period)
            .Include(sl => sl.Room)
            .Include(sl => sl.ScheduledLessonRooms)
                .ThenInclude(slr => slr.Room)
            .Where(sl => sl.TimetableId == timetable.Id &&
                         sl.Lesson != null &&
                         sl.Lesson.LessonTeachers.Any(lt => lt.TeacherId == teacherId) &&
                         sl.DayOfWeek == dayOfWeek)
            .OrderBy(sl => sl.Period != null ? sl.Period.PeriodNumber : 0)
            .ToListAsync();

        TeacherSchedule = lessons.Select(sl => new ScheduledLessonInfo
        {
            PeriodNumber = sl.Period?.PeriodNumber ?? 0,
            PeriodTime = sl.Period != null
                ? $"{sl.Period.StartTime:hh\\:mm} - {sl.Period.EndTime:hh\\:mm}"
                : "N/A",
            ClassName = sl.Lesson?.LessonClasses.FirstOrDefault()?.Class?.Name ?? "Unknown",
            SubjectName = sl.Lesson?.LessonSubjects.FirstOrDefault()?.Subject?.Name ?? "Unknown",
            RoomNumber = sl.Room?.RoomNumber ??
                        (sl.ScheduledLessonRooms.Any()
                            ? string.Join(", ", sl.ScheduledLessonRooms.Select(slr => slr.Room?.RoomNumber))
                            : "TBA")
        }).ToList();
    }

    private async Task LoadTeacherSupervisionDutiesAsync(int teacherId, DateTime date)
    {
        var dayOfWeek = date.DayOfWeek;

        var duties = await _context.BreakSupervisionDuties
            .Include(d => d.Room)
            .Where(d => d.TeacherId == teacherId &&
                       d.DayOfWeek == dayOfWeek &&
                       d.IsActive)
            .OrderBy(d => d.PeriodNumber)
            .ToListAsync();

        TeacherSupervisionDuties = duties.Select(d => new SupervisionDutyInfo
        {
            PeriodNumber = d.PeriodNumber,
            Location = d.Room?.RoomNumber ?? "Unknown"
        }).ToList();
    }
}
