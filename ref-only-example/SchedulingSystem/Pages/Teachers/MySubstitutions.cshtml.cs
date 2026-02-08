// DISABLED: Teachers do not have access to the system
// This page is commented out for future use when/if teacher self-service is implemented
// Administrators manage all substitutions through /Admin routes

/*
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Teachers;

/// <summary>
/// Dashboard for substitute teachers to view their assignments
/// CURRENTLY DISABLED - Teachers do not have system access
/// </summary>
public class MySubstitutionsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public MySubstitutionsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public int? TeacherId { get; set; } // In real app, get from authenticated user
    public string TeacherName { get; set; } = string.Empty;
    public SubstitutionStats Stats { get; set; } = new();
    public List<UpcomingSubstitution> UpcomingSubstitutions { get; set; } = new();
    public List<PastSubstitution> PastSubstitutions { get; set; } = new();

    public class SubstitutionStats
    {
        public int TotalThisWeek { get; set; }
        public int TotalThisMonth { get; set; }
        public int TotalAllTime { get; set; }
        public decimal HoursThisWeek { get; set; }
        public decimal HoursThisMonth { get; set; }
        public decimal EarningsThisMonth { get; set; }
        public int MaxSubstitutionsPerWeek { get; set; }
        public decimal CapacityPercentage { get; set; }
        public bool IsActive { get; set; }
    }

    public class UpcomingSubstitution
    {
        public int SubstitutionId { get; set; }
        public DateTime Date { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public string PeriodTime { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public string Room { get; set; } = string.Empty;
        public string OriginalTeacher { get; set; } = string.Empty;
        public string AbsenceReason { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public decimal Hours { get; set; }
        public decimal? Pay { get; set; }
    }

    public class PastSubstitution
    {
        public DateTime Date { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public decimal Hours { get; set; }
        public decimal? Pay { get; set; }
    }

    public async Task OnGetAsync(int? teacherId = null)
    {
        // In production, get teacherId from authenticated user
        // For demo, allow passing as parameter
        TeacherId = teacherId;

        if (TeacherId.HasValue)
        {
            await LoadDataAsync(TeacherId.Value);
        }
    }

    private async Task LoadDataAsync(int teacherId)
    {
        var teacher = await _context.Teachers.FindAsync(teacherId);
        if (teacher == null)
            return;

        TeacherName = teacher.Name;

        // Calculate stats
        var today = DateTime.Today;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var allSubstitutions = await _context.Substitutions
            .Include(s => s.Absence)
            .Where(s => s.SubstituteTeacherId == teacherId)
            .ToListAsync();

        var thisWeekSubs = allSubstitutions.Where(s => s.Absence.Date >= weekStart).ToList();
        var thisMonthSubs = allSubstitutions.Where(s => s.Absence.Date >= monthStart).ToList();

        Stats = new SubstitutionStats
        {
            TotalThisWeek = thisWeekSubs.Count,
            TotalThisMonth = thisMonthSubs.Count,
            TotalAllTime = allSubstitutions.Count,
            HoursThisWeek = thisWeekSubs.Sum(s => s.HoursWorked),
            HoursThisMonth = thisMonthSubs.Sum(s => s.HoursWorked),
            EarningsThisMonth = thisMonthSubs.Sum(s => s.TotalPay ?? 0),
            MaxSubstitutionsPerWeek = teacher.MaxSubstitutionsPerWeek ?? 10,
            IsActive = teacher.AvailableForSubstitution
        };

        Stats.CapacityPercentage = Stats.MaxSubstitutionsPerWeek > 0
            ? (decimal)Stats.TotalThisWeek / Stats.MaxSubstitutionsPerWeek * 100
            : 0;

        // Load upcoming substitutions (today and future)
        var upcomingData = await _context.Substitutions
            .Include(s => s.Absence)
                .ThenInclude(a => a.Teacher)
            .Include(s => s.ScheduledLesson)
                .ThenInclude(sl => sl.Lesson)
                    .ThenInclude(l => l.Subject)
            .Include(s => s.ScheduledLesson)
                .ThenInclude(sl => sl.Lesson)
                    .ThenInclude(l => l.Class)
            .Include(s => s.ScheduledLesson)
                .ThenInclude(sl => sl.Period)
            .Include(s => s.ScheduledLesson)
                .ThenInclude(sl => sl.Room)
            .Include(s => s.ScheduledLesson)
                .ThenInclude(sl => sl.ScheduledLessonRooms)
                    .ThenInclude(slr => slr.Room)
            .Where(s => s.SubstituteTeacherId == teacherId)
            .Where(s => s.Absence.Date >= today)
            .OrderBy(s => s.Absence.Date)
            .ThenBy(s => s.ScheduledLesson.Period.PeriodNumber)
            .ToListAsync();

        UpcomingSubstitutions = upcomingData.Select(s => new UpcomingSubstitution
        {
            SubstitutionId = s.Id,
            Date = s.Absence.Date,
            DayOfWeek = s.ScheduledLesson.DayOfWeek.ToString(),
            PeriodTime = $"{s.ScheduledLesson.Period.StartTime:hh\\:mm} - {s.ScheduledLesson.Period.EndTime:hh\\:mm}",
            Subject = s.ScheduledLesson.Lesson.Subject.Name,
            Class = s.ScheduledLesson.Lesson.Class.Name,
            Room = s.ScheduledLesson.Room?.Name ??
                   (s.ScheduledLesson.ScheduledLessonRooms.Any()
                       ? string.Join(", ", s.ScheduledLesson.ScheduledLessonRooms.Select(slr => slr.Room?.RoomNumber))
                       : "TBA"),
            OriginalTeacher = s.Absence.Teacher.Name,
            AbsenceReason = s.Absence.Type.ToString(),
            Notes = s.Notes,
            Hours = s.HoursWorked,
            Pay = s.TotalPay
        }).ToList();

        // Load past substitutions (last 30 days)
        var pastData = await _context.Substitutions
            .Include(s => s.Absence)
            .Include(s => s.ScheduledLesson)
                .ThenInclude(sl => sl.Lesson)
                    .ThenInclude(l => l.Subject)
            .Include(s => s.ScheduledLesson)
                .ThenInclude(sl => sl.Lesson)
                    .ThenInclude(l => l.Class)
            .Where(s => s.SubstituteTeacherId == teacherId)
            .Where(s => s.Absence.Date < today)
            .Where(s => s.Absence.Date >= today.AddDays(-30))
            .OrderByDescending(s => s.Absence.Date)
            .ToListAsync();

        PastSubstitutions = pastData.Select(s => new PastSubstitution
        {
            Date = s.Absence.Date,
            Subject = s.ScheduledLesson.Lesson.Subject.Name,
            Class = s.ScheduledLesson.Lesson.Class.Name,
            Hours = s.HoursWorked,
            Pay = s.TotalPay
        }).ToList();
    }
}
*/
