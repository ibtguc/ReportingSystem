using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.BreakSupervision;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Room> Locations { get; set; } = new();
    public List<BreakSupervisionDuty> Duties { get; set; } = new();
    public List<int> Periods { get; set; } = new();
    public List<DayOfWeek> Days { get; set; } = new()
    {
        DayOfWeek.Sunday,
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday
    };

    public SelectList TeacherList { get; set; } = new SelectList(new List<object>());
    public SelectList TimetableList { get; set; } = new SelectList(new List<object>());

    [BindProperty(SupportsGet = true)]
    public int? TimetableId { get; set; }

    public Timetable? SelectedTimetable { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public bool IsSuccess { get; set; }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        // Load timetables for dropdown
        var timetables = await _context.Timetables
            .OrderByDescending(t => t.Status == TimetableStatus.Published)
            .ThenByDescending(t => t.CreatedDate)
            .Select(t => new { t.Id, DisplayName = t.Name + (t.Status == TimetableStatus.Published ? " (Published)" : t.Status == TimetableStatus.Draft ? " (Draft)" : " (Archived)") })
            .ToListAsync();

        TimetableList = new SelectList(timetables, "Id", "DisplayName", TimetableId);

        // If no timetable selected, try to get the published one or first available
        if (!TimetableId.HasValue && timetables.Any())
        {
            var publishedTimetable = await _context.Timetables
                .Where(t => t.Status == TimetableStatus.Published)
                .FirstOrDefaultAsync();
            TimetableId = publishedTimetable?.Id ?? timetables.First().Id;
        }

        // Load selected timetable
        if (TimetableId.HasValue)
        {
            SelectedTimetable = await _context.Timetables.FindAsync(TimetableId.Value);
        }

        // Load break supervision duties filtered by timetable
        var dutiesQuery = _context.BreakSupervisionDuties
            .Include(d => d.Room)
            .Include(d => d.Teacher)
            .Where(d => d.IsActive);

        if (TimetableId.HasValue)
        {
            dutiesQuery = dutiesQuery.Where(d => d.TimetableId == TimetableId.Value);
        }
        else
        {
            // Show duties without timetable (legacy/unassigned)
            dutiesQuery = dutiesQuery.Where(d => d.TimetableId == null);
        }

        Duties = await dutiesQuery
            .OrderBy(d => d.Room!.RoomNumber)
            .ThenBy(d => d.DayOfWeek)
            .ThenBy(d => d.PeriodNumber)
            .ToListAsync();

        // Get distinct locations (rooms) used in supervision for this timetable
        var locationIds = Duties.Select(d => d.RoomId).Distinct().ToList();
        Locations = await _context.Rooms
            .Where(r => locationIds.Contains(r.Id))
            .OrderBy(r => r.RoomNumber)
            .ToListAsync();

        // Get distinct periods
        Periods = Duties.Select(d => d.PeriodNumber).Distinct().OrderBy(p => p).ToList();
        if (!Periods.Any())
        {
            Periods = new List<int> { 3, 5 }; // Default periods from GPU009
        }

        // Load teachers for dropdown
        var teachers = await _context.Teachers
            .Where(t => t.IsActive)
            .OrderBy(t => t.FirstName)
            .Select(t => new { t.Id, t.FirstName })
            .ToListAsync();

        TeacherList = new SelectList(teachers, "Id", "FirstName");
    }

    public BreakSupervisionDuty? GetDuty(int roomId, DayOfWeek day, int periodNumber)
    {
        return Duties.FirstOrDefault(d =>
            d.RoomId == roomId &&
            d.DayOfWeek == day &&
            d.PeriodNumber == periodNumber);
    }

    // Convert supervision period number to "Break 1", "Break 2", etc.
    public string GetBreakLabel(int periodNumber)
    {
        var index = Periods.IndexOf(periodNumber);
        return index >= 0 ? $"Break {index + 1}" : "Break";
    }

    public async Task<IActionResult> OnPostUpdateDutyAsync(int roomId, int dayOfWeek, int periodNumber, int? teacherId, int? timetableId)
    {
        var day = (DayOfWeek)dayOfWeek;

        var duty = await _context.BreakSupervisionDuties
            .FirstOrDefaultAsync(d =>
                d.RoomId == roomId &&
                d.DayOfWeek == day &&
                d.PeriodNumber == periodNumber &&
                d.TimetableId == timetableId);

        if (duty != null)
        {
            duty.TeacherId = teacherId;
            await _context.SaveChangesAsync();
            StatusMessage = "Duty updated successfully";
            IsSuccess = true;
        }
        else if (teacherId.HasValue && timetableId.HasValue)
        {
            // Create new duty
            duty = new BreakSupervisionDuty
            {
                RoomId = roomId,
                DayOfWeek = day,
                PeriodNumber = periodNumber,
                TeacherId = teacherId,
                Points = 30,
                IsActive = true,
                TimetableId = timetableId.Value
            };
            _context.BreakSupervisionDuties.Add(duty);
            await _context.SaveChangesAsync();
            StatusMessage = "Duty created successfully";
            IsSuccess = true;
        }
        else if (!timetableId.HasValue)
        {
            StatusMessage = "Please select a timetable first";
            IsSuccess = false;
        }

        return RedirectToPage(new { timetableId });
    }

    public async Task<IActionResult> OnPostAddLocationAsync(string locationCode, int? timetableId)
    {
        if (string.IsNullOrWhiteSpace(locationCode))
        {
            StatusMessage = "Please enter a location code";
            IsSuccess = false;
            return RedirectToPage(new { timetableId });
        }

        if (!timetableId.HasValue)
        {
            StatusMessage = "Please select a timetable first";
            IsSuccess = false;
            return RedirectToPage();
        }

        // Check if room exists
        var room = await _context.Rooms
            .FirstOrDefaultAsync(r => r.RoomNumber.ToLower() == locationCode.ToLower());

        if (room == null)
        {
            StatusMessage = $"Room '{locationCode}' not found. Please add it to Rooms first.";
            IsSuccess = false;
            return RedirectToPage(new { timetableId });
        }

        // Create empty duties for all days and periods for this timetable
        var existingDuties = await _context.BreakSupervisionDuties
            .Where(d => d.RoomId == room.Id && d.TimetableId == timetableId)
            .ToListAsync();

        int added = 0;
        foreach (var day in Days)
        {
            foreach (var periodNumber in new[] { 3, 5 }) // Default periods from GPU009
            {
                if (!existingDuties.Any(d => d.DayOfWeek == day && d.PeriodNumber == periodNumber))
                {
                    _context.BreakSupervisionDuties.Add(new BreakSupervisionDuty
                    {
                        RoomId = room.Id,
                        DayOfWeek = day,
                        PeriodNumber = periodNumber,
                        TeacherId = null,
                        Points = 30,
                        IsActive = true,
                        TimetableId = timetableId.Value
                    });
                    added++;
                }
            }
        }

        if (added > 0)
        {
            await _context.SaveChangesAsync();
            StatusMessage = $"Added {added} supervision slots for location '{locationCode}'";
            IsSuccess = true;
        }
        else
        {
            StatusMessage = $"Location '{locationCode}' already has all supervision slots for this timetable";
            IsSuccess = false;
        }

        return RedirectToPage(new { timetableId });
    }
}
