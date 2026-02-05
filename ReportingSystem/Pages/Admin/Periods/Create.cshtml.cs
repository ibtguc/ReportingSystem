using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.Periods;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public ReportTemplate Template { get; set; } = null!;

    [BindProperty]
    public ReportPeriod Period { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int templateId)
    {
        var template = await _context.ReportTemplates.FindAsync(templateId);
        if (template == null) return NotFound();

        Template = template;
        Period.ReportTemplateId = templateId;

        // Auto-suggest dates based on schedule and last period
        var lastPeriod = await _context.ReportPeriods
            .Where(p => p.ReportTemplateId == templateId)
            .OrderByDescending(p => p.EndDate)
            .FirstOrDefaultAsync();

        if (lastPeriod != null)
        {
            Period.StartDate = lastPeriod.EndDate.AddDays(1);
        }
        else
        {
            Period.StartDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        }

        // Suggest end date based on schedule
        Period.EndDate = template.Schedule switch
        {
            ReportSchedule.Daily => Period.StartDate,
            ReportSchedule.Weekly => Period.StartDate.AddDays(6),
            ReportSchedule.BiWeekly => Period.StartDate.AddDays(13),
            ReportSchedule.Monthly => Period.StartDate.AddMonths(1).AddDays(-1),
            ReportSchedule.Quarterly => Period.StartDate.AddMonths(3).AddDays(-1),
            ReportSchedule.Annual => Period.StartDate.AddYears(1).AddDays(-1),
            _ => Period.StartDate.AddMonths(1).AddDays(-1)
        };

        Period.SubmissionDeadline = Period.EndDate.AddDays(5);

        // Auto-suggest name
        Period.Name = GeneratePeriodName(template.Schedule, Period.StartDate, Period.EndDate);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int templateId)
    {
        var template = await _context.ReportTemplates.FindAsync(templateId);
        if (template == null) return NotFound();

        Template = template;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (Period.EndDate <= Period.StartDate)
        {
            ModelState.AddModelError("Period.EndDate", "End date must be after start date.");
            return Page();
        }

        if (Period.SubmissionDeadline < Period.EndDate)
        {
            ModelState.AddModelError("Period.SubmissionDeadline", "Submission deadline should be on or after the end date.");
            return Page();
        }

        // Check for overlapping periods
        var overlapping = await _context.ReportPeriods
            .Where(p => p.ReportTemplateId == templateId && p.IsActive)
            .Where(p => p.StartDate < Period.EndDate && p.EndDate > Period.StartDate)
            .AnyAsync();

        if (overlapping)
        {
            ModelState.AddModelError("", "This period overlaps with an existing period for this template.");
            return Page();
        }

        Period.ReportTemplateId = templateId;
        Period.CreatedAt = DateTime.UtcNow;

        _context.ReportPeriods.Add(Period);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Period '{Period.Name}' created successfully.";
        return RedirectToPage("Index", new { templateId });
    }

    private static string GeneratePeriodName(string schedule, DateTime start, DateTime end)
    {
        return schedule switch
        {
            ReportSchedule.Daily => start.ToString("MMMM dd, yyyy"),
            ReportSchedule.Weekly => $"Week of {start:MMM dd, yyyy}",
            ReportSchedule.BiWeekly => $"Bi-Week {start:MMM dd} - {end:MMM dd, yyyy}",
            ReportSchedule.Monthly => start.ToString("MMMM yyyy"),
            ReportSchedule.Quarterly => $"Q{(start.Month - 1) / 3 + 1} {start.Year}",
            ReportSchedule.Annual => $"FY {start.Year}-{end.Year}",
            _ => $"{start:MMM dd} - {end:MMM dd, yyyy}"
        };
    }
}
