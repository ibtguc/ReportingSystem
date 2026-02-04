using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Absences;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<AbsenceViewModel> Absences { get; set; } = new();
    public List<SelectListItem> Teachers { get; set; } = new();
    public List<SelectListItem> Statuses { get; set; } = new();
    public int? PublishedTimetableId { get; set; }

    [BindProperty(SupportsGet = true)]
    public AbsenceFilter Filter { get; set; } = new();

    public class AbsenceFilter
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? TeacherId { get; set; }
        public AbsenceStatus? Status { get; set; }
    }

    public class AbsenceViewModel
    {
        public int Id { get; set; }
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public AbsenceStatus StatusEnum { get; set; }
        public decimal TotalHours { get; set; }
        public string? Notes { get; set; }
        public int AffectedLessons { get; set; }
        public int CoveredLessons { get; set; }
        public int AffectedSupervisionDuties { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var absence = await _context.Absences
            .Include(a => a.Substitutions)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (absence == null)
        {
            return NotFound();
        }

        _context.Absences.Remove(absence);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Absence deleted successfully.";
        return RedirectToPage();
    }

    private async Task LoadDataAsync()
    {
        // Build query
        var query = _context.Absences
            .Include(a => a.Teacher)
            .Include(a => a.Substitutions)
            .AsQueryable();

        // Apply filters
        if (Filter.FromDate.HasValue)
        {
            query = query.Where(a => a.Date >= Filter.FromDate.Value);
        }

        if (Filter.ToDate.HasValue)
        {
            query = query.Where(a => a.Date <= Filter.ToDate.Value);
        }

        if (Filter.TeacherId.HasValue)
        {
            query = query.Where(a => a.TeacherId == Filter.TeacherId.Value);
        }

        if (Filter.Status.HasValue)
        {
            query = query.Where(a => a.Status == Filter.Status.Value);
        }

        // Execute query
        var absences = await query
            .OrderByDescending(a => a.Date)
            .ThenByDescending(a => a.ReportedAt)
            .ToListAsync();

        // Get all active break supervision duties for calculating affected count
        var allSupervisionDuties = await _context.BreakSupervisionDuties
            .Where(d => d.IsActive && d.TeacherId != null)
            .Select(d => new { d.TeacherId, d.DayOfWeek })
            .ToListAsync();

        // Map to view models
        Absences = absences.Select(a => new AbsenceViewModel
        {
            Id = a.Id,
            TeacherId = a.TeacherId,
            TeacherName = a.Teacher.Name,
            Date = a.Date,
            StartTime = a.StartTime,
            EndTime = a.EndTime,
            Type = a.Type.ToString(),
            Status = a.Status.ToString(),
            StatusEnum = a.Status,
            TotalHours = a.TotalHours,
            Notes = a.Notes,
            AffectedLessons = 0, // Will be calculated if needed
            CoveredLessons = a.Substitutions.Count,
            AffectedSupervisionDuties = allSupervisionDuties
                .Count(d => d.TeacherId == a.TeacherId && d.DayOfWeek == a.Date.DayOfWeek)
        }).ToList();

        // Get the latest published timetable
        var timetable = await _context.Timetables
            .Where(t => t.Status == TimetableStatus.Published)
            .OrderByDescending(t => t.CreatedDate)
            .FirstOrDefaultAsync();

        PublishedTimetableId = timetable?.Id;

        // Load filter dropdowns
        var teachers = await _context.Teachers
            .OrderBy(t => t.FirstName)
            .ThenBy(t => t.LastName)
            .Select(t => new { t.Id, t.FirstName, t.LastName })
            .ToListAsync();

        Teachers = new List<SelectListItem> { new SelectListItem("All Teachers", "") }
            .Concat(teachers.Select(t => new SelectListItem(
                !string.IsNullOrEmpty(t.LastName) ? $"{t.FirstName} {t.LastName}" : t.FirstName,
                t.Id.ToString())))
            .ToList();

        Statuses = new List<SelectListItem> { new SelectListItem("All Statuses", "") }
            .Concat(Enum.GetValues<AbsenceStatus>().Select(s => new SelectListItem(s.ToString(), ((int)s).ToString())))
            .ToList();
    }
}
