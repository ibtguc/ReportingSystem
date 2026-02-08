using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Substitutions;

public class ReportsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public ReportsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public ReportFilter Filter { get; set; } = new();

    public ReportSummary Summary { get; set; } = new();
    public List<TeacherWorkloadReport> TeacherWorkloads { get; set; } = new();
    public List<SubjectCoverageReport> SubjectCoverages { get; set; } = new();
    public List<AbsenceTypeReport> AbsenceTypes { get; set; } = new();
    public List<DailyTrendReport> DailyTrends { get; set; } = new();
    public Dictionary<string, int> AbsencesByDayOfWeek { get; set; } = new();
    public List<BreakSupervisionReport> BreakSupervisionStats { get; set; } = new();
    public BreakSupervisionSummary SupervisionSummary { get; set; } = new();

    public class ReportFilter
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? TeacherId { get; set; }
        public int? SubjectId { get; set; }
        public int? DepartmentId { get; set; }
    }

    public class ReportSummary
    {
        public int TotalAbsences { get; set; }
        public int TotalSubstitutions { get; set; }
        public int CoveredLessons { get; set; }
        public int UncoveredLessons { get; set; }
        public decimal CoverageRate { get; set; }
        public decimal TotalHoursSubstituted { get; set; }
        public decimal TotalPayrollCost { get; set; }
        public int UniqueSubstitutes { get; set; }
        public int UniqueAbsentTeachers { get; set; }
        public decimal AverageSubstitutionsPerDay { get; set; }
    }

    public class BreakSupervisionSummary
    {
        public int TotalAffectedDuties { get; set; }
        public int TotalLocations { get; set; }
        public int TotalAffectedTeachers { get; set; }
        public Dictionary<string, int> DutiesByLocation { get; set; } = new();
    }

    public class BreakSupervisionReport
    {
        public string Location { get; set; } = string.Empty;
        public int TotalDuties { get; set; }
        public int AffectedByAbsence { get; set; }
        public List<string> AffectedTeachers { get; set; } = new();
        public Dictionary<int, int> DutiesByPeriod { get; set; } = new();
    }

    public class TeacherWorkloadReport
    {
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public int SubstitutionCount { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalPay { get; set; }
        public int UniqueAbsentTeachers { get; set; }
        public int UniqueSubjects { get; set; }
    }

    public class SubjectCoverageReport
    {
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public int TotalAbsences { get; set; }
        public int CoveredCount { get; set; }
        public int UncoveredCount { get; set; }
        public decimal CoverageRate { get; set; }
    }

    public class AbsenceTypeReport
    {
        public AbsenceType Type { get; set; }
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    public class DailyTrendReport
    {
        public DateTime Date { get; set; }
        public int AbsenceCount { get; set; }
        public int SubstitutionCount { get; set; }
        public int SupervisionDutiesAffected { get; set; }
    }

    public async Task OnGetAsync()
    {
        // Set default date range if not provided
        if (!Filter.FromDate.HasValue)
            Filter.FromDate = DateTime.Today.AddMonths(-1);
        if (!Filter.ToDate.HasValue)
            Filter.ToDate = DateTime.Today;

        await LoadReportDataAsync();
    }

    private async Task LoadReportDataAsync()
    {
        // Build base query for absences
        var absencesQuery = _context.Absences
            .Include(a => a.Teacher)
                .ThenInclude(t => t.Department)
            .Include(a => a.Substitutions)
                .ThenInclude(s => s.SubstituteTeacher)
            .Include(a => a.Substitutions)
                .ThenInclude(s => s.ScheduledLesson)
                    .ThenInclude(sl => sl.Lesson)
                        .ThenInclude(l => l.LessonSubjects)
                            .ThenInclude(ls => ls.Subject)
            .AsQueryable();

        // Apply filters
        if (Filter.FromDate.HasValue)
            absencesQuery = absencesQuery.Where(a => a.Date >= Filter.FromDate.Value);
        if (Filter.ToDate.HasValue)
            absencesQuery = absencesQuery.Where(a => a.Date <= Filter.ToDate.Value);
        if (Filter.TeacherId.HasValue)
            absencesQuery = absencesQuery.Where(a => a.TeacherId == Filter.TeacherId.Value);
        if (Filter.DepartmentId.HasValue)
            absencesQuery = absencesQuery.Where(a => a.Teacher != null && a.Teacher.DepartmentId == Filter.DepartmentId.Value);

        var absences = await absencesQuery.ToListAsync();

        // Calculate summary statistics
        var allSubstitutions = absences.SelectMany(a => a.Substitutions).ToList();
        var coveredLessons = allSubstitutions.Count(s => s.SubstituteTeacherId.HasValue);
        var uncoveredLessons = allSubstitutions.Count(s => !s.SubstituteTeacherId.HasValue);
        var totalLessons = coveredLessons + uncoveredLessons;

        var daysDifference = (Filter.ToDate!.Value - Filter.FromDate!.Value).Days + 1;

        Summary = new ReportSummary
        {
            TotalAbsences = absences.Count,
            TotalSubstitutions = allSubstitutions.Count,
            CoveredLessons = coveredLessons,
            UncoveredLessons = uncoveredLessons,
            CoverageRate = totalLessons > 0 ? (decimal)coveredLessons / totalLessons * 100 : 0,
            TotalHoursSubstituted = allSubstitutions.Sum(s => s.HoursWorked),
            TotalPayrollCost = allSubstitutions.Sum(s => s.TotalPay ?? 0),
            UniqueSubstitutes = allSubstitutions.Where(s => s.SubstituteTeacherId.HasValue)
                .Select(s => s.SubstituteTeacherId).Distinct().Count(),
            UniqueAbsentTeachers = absences.Select(a => a.TeacherId).Distinct().Count(),
            AverageSubstitutionsPerDay = daysDifference > 0 ? (decimal)allSubstitutions.Count / daysDifference : 0
        };

        // Teacher workload report
        var teacherWorkloads = allSubstitutions
            .Where(s => s.SubstituteTeacherId.HasValue)
            .GroupBy(s => new { s.SubstituteTeacherId, s.SubstituteTeacher })
            .Select(g => new TeacherWorkloadReport
            {
                TeacherId = g.Key.SubstituteTeacherId!.Value,
                TeacherName = g.Key.SubstituteTeacher!.Name,
                DepartmentName = g.Key.SubstituteTeacher?.Department?.Name,
                SubstitutionCount = g.Count(),
                TotalHours = g.Sum(s => s.HoursWorked),
                TotalPay = g.Sum(s => s.TotalPay ?? 0),
                UniqueAbsentTeachers = g.Select(s => s.Absence != null ? s.Absence.TeacherId : 0).Distinct().Count(),
                UniqueSubjects = g.Select(s => s.ScheduledLesson?.Lesson?.LessonSubjects.FirstOrDefault()?.SubjectId ?? 0).Distinct().Count()
            })
            .OrderByDescending(t => t.SubstitutionCount)
            .ToList();

        TeacherWorkloads = teacherWorkloads;

        // Subject coverage report
        var subjectStats = allSubstitutions
            .Where(s => s.ScheduledLesson?.Lesson?.LessonSubjects.Any() == true)
            .SelectMany(s => s.ScheduledLesson!.Lesson!.LessonSubjects.Select(ls => new { Substitution = s, Subject = ls.Subject }))
            .Where(x => x.Subject != null)
            .GroupBy(x => new { x.Subject!.Id, x.Subject.Name })
            .Select(g => new SubjectCoverageReport
            {
                SubjectId = g.Key.Id,
                SubjectName = g.Key.Name,
                TotalAbsences = g.Count(),
                CoveredCount = g.Count(x => x.Substitution.SubstituteTeacherId.HasValue),
                UncoveredCount = g.Count(x => !x.Substitution.SubstituteTeacherId.HasValue),
                CoverageRate = g.Count() > 0 ? (decimal)g.Count(x => x.Substitution.SubstituteTeacherId.HasValue) / g.Count() * 100 : 0
            })
            .OrderByDescending(s => s.TotalAbsences)
            .ToList();

        SubjectCoverages = subjectStats;

        // Absence type breakdown
        var totalAbsenceCount = absences.Count;
        var absenceTypeStats = absences
            .GroupBy(a => a.Type)
            .Select(g => new AbsenceTypeReport
            {
                Type = g.Key,
                Count = g.Count(),
                Percentage = totalAbsenceCount > 0 ? (decimal)g.Count() / totalAbsenceCount * 100 : 0
            })
            .OrderByDescending(a => a.Count)
            .ToList();

        AbsenceTypes = absenceTypeStats;

        // Daily trends
        var dailyStats = absences
            .GroupBy(a => a.Date.Date)
            .Select(g => new DailyTrendReport
            {
                Date = g.Key,
                AbsenceCount = g.Count(),
                SubstitutionCount = g.SelectMany(a => a.Substitutions).Count()
            })
            .OrderBy(d => d.Date)
            .ToList();

        DailyTrends = dailyStats;

        // Absences by day of week
        AbsencesByDayOfWeek = absences
            .GroupBy(a => a.Date.DayOfWeek.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // Break Supervision Statistics
        await LoadBreakSupervisionStatsAsync(absences);
    }

    private async Task LoadBreakSupervisionStatsAsync(List<Absence> absences)
    {
        // Get all break supervision duties
        var allDuties = await _context.BreakSupervisionDuties
            .Include(d => d.Room)
            .Include(d => d.Teacher)
            .Where(d => d.IsActive)
            .ToListAsync();

        if (!allDuties.Any())
        {
            SupervisionSummary = new BreakSupervisionSummary();
            BreakSupervisionStats = new List<BreakSupervisionReport>();
            return;
        }

        // For each absence, find the supervision duties that would be affected
        var affectedDuties = new List<(BreakSupervisionDuty Duty, Absence Absence)>();

        foreach (var absence in absences)
        {
            // Get the day of week for this absence
            var dayOfWeek = absence.Date.DayOfWeek;

            // Find supervision duties for this teacher on this day
            var teacherDuties = allDuties
                .Where(d => d.TeacherId == absence.TeacherId && d.DayOfWeek == dayOfWeek)
                .ToList();

            foreach (var duty in teacherDuties)
            {
                affectedDuties.Add((duty, absence));
            }
        }

        // Calculate summary
        SupervisionSummary = new BreakSupervisionSummary
        {
            TotalAffectedDuties = affectedDuties.Count,
            TotalLocations = affectedDuties.Select(d => d.Duty.RoomId).Distinct().Count(),
            TotalAffectedTeachers = affectedDuties.Select(d => d.Duty.TeacherId).Distinct().Count(),
            DutiesByLocation = affectedDuties
                .GroupBy(d => d.Duty.Room?.RoomNumber ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count())
        };

        // Break supervision by location report
        BreakSupervisionStats = allDuties
            .GroupBy(d => d.Room?.RoomNumber ?? "Unknown")
            .Select(g => new BreakSupervisionReport
            {
                Location = g.Key,
                TotalDuties = g.Count(),
                AffectedByAbsence = affectedDuties.Count(a => (a.Duty.Room?.RoomNumber ?? "Unknown") == g.Key),
                AffectedTeachers = affectedDuties
                    .Where(a => (a.Duty.Room?.RoomNumber ?? "Unknown") == g.Key)
                    .Select(a => a.Duty.Teacher?.Name ?? "Unknown")
                    .Distinct()
                    .ToList(),
                DutiesByPeriod = g.GroupBy(d => d.PeriodNumber).ToDictionary(pg => pg.Key, pg => pg.Count())
            })
            .Where(r => r.AffectedByAbsence > 0) // Only show locations with affected duties
            .OrderByDescending(r => r.AffectedByAbsence)
            .ToList();

        // Update daily trends to include supervision data
        foreach (var trend in DailyTrends)
        {
            var dayOfWeek = trend.Date.DayOfWeek;
            var absentTeacherIds = absences
                .Where(a => a.Date.Date == trend.Date.Date)
                .Select(a => a.TeacherId)
                .ToList();

            trend.SupervisionDutiesAffected = allDuties
                .Count(d => d.DayOfWeek == dayOfWeek && absentTeacherIds.Contains(d.TeacherId ?? 0));
        }
    }

    public async Task<IActionResult> OnPostExportCsvAsync()
    {
        await LoadReportDataAsync();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Teacher Workload Report");
        csv.AppendLine("Teacher,Department,Substitutions,Hours,Total Pay");
        foreach (var teacher in TeacherWorkloads)
        {
            csv.AppendLine($"{teacher.TeacherName},{teacher.DepartmentName},{teacher.SubstitutionCount},{teacher.TotalHours:F2},{teacher.TotalPay:F2}");
        }

        csv.AppendLine();
        csv.AppendLine("Subject Coverage Report");
        csv.AppendLine("Subject,Total,Covered,Uncovered,Coverage Rate");
        foreach (var subject in SubjectCoverages)
        {
            csv.AppendLine($"{subject.SubjectName},{subject.TotalAbsences},{subject.CoveredCount},{subject.UncoveredCount},{subject.CoverageRate:F1}%");
        }

        csv.AppendLine();
        csv.AppendLine("Break Supervision Report");
        csv.AppendLine("Location,Total Duties,Affected by Absence,Affected Teachers");
        foreach (var supervision in BreakSupervisionStats)
        {
            csv.AppendLine($"{supervision.Location},{supervision.TotalDuties},{supervision.AffectedByAbsence},\"{string.Join("; ", supervision.AffectedTeachers)}\"");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"substitution-report-{DateTime.Now:yyyy-MM-dd}.csv");
    }
}
