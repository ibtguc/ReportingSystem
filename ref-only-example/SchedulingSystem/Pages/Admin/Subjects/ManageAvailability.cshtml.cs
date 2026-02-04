using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Subjects;

public class ManageAvailabilityModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public ManageAvailabilityModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public Subject Subject { get; set; } = default!;
    public List<SubjectAvailability> Availabilities { get; set; } = new();
    public List<Period> Periods { get; set; } = new();
    public SelectList PeriodList { get; set; } = default!;
    public SelectList DayList { get; set; } = default!;

    [BindProperty]
    public SubjectAvailability NewAvailability { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var subject = await _context.Subjects
            .FirstOrDefaultAsync(m => m.Id == id);

        if (subject == null)
        {
            return NotFound();
        }

        Subject = subject;
        Availabilities = await _context.SubjectAvailabilities
            .Include(a => a.Period)
            .Where(a => a.SubjectId == id)
            .OrderBy(a => a.DayOfWeek)
            .ThenBy(a => a.Period!.PeriodNumber)
            .ToListAsync();

        Periods = await _context.Periods
            .OrderBy(p => p.PeriodNumber)
            .ToListAsync();

        await LoadSelectListsAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAddAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            return await OnGetAsync(id);
        }

        var exists = await _context.SubjectAvailabilities
            .AnyAsync(sa => sa.SubjectId == id
                && sa.DayOfWeek == NewAvailability.DayOfWeek
                && sa.PeriodId == NewAvailability.PeriodId);

        if (exists)
        {
            ModelState.AddModelError("", "An availability constraint already exists for this day and period.");
            return await OnGetAsync(id);
        }

        NewAvailability.SubjectId = id;
        _context.SubjectAvailabilities.Add(NewAvailability);
        await _context.SaveChangesAsync();

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, int availabilityId)
    {
        var availability = await _context.SubjectAvailabilities.FindAsync(availabilityId);

        if (availability != null)
        {
            _context.SubjectAvailabilities.Remove(availability);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostToggleCriticalityAsync(int id, int availabilityId)
    {
        var availability = await _context.SubjectAvailabilities.FindAsync(availabilityId);

        if (availability != null)
        {
            availability.Importance = availability.Importance switch
            {
                -3 => -2,
                -2 => -1,
                -1 => 0,
                0 => 1,
                1 => 2,
                2 => 3,
                3 => -3,
                _ => -3
            };
            await _context.SaveChangesAsync();
        }

        return RedirectToPage(new { id });
    }

    private async Task LoadSelectListsAsync()
    {
        var periods = await _context.Periods
            .OrderBy(p => p.PeriodNumber)
            .ToListAsync();

        PeriodList = new SelectList(periods, "Id", "Name");

        DayList = new SelectList(new[]
        {
            new { Value = (int)DayOfWeek.Sunday, Text = "Sunday" },
            new { Value = (int)DayOfWeek.Monday, Text = "Monday" },
            new { Value = (int)DayOfWeek.Tuesday, Text = "Tuesday" },
            new { Value = (int)DayOfWeek.Wednesday, Text = "Wednesday" },
            new { Value = (int)DayOfWeek.Thursday, Text = "Thursday" }
        }, "Value", "Text");
    }
}
