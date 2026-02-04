using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Timetables;

public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DeleteModel> _logger;

    public DeleteModel(ApplicationDbContext context, ILogger<DeleteModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Timetable Timetable { get; set; } = null!;
    public int ScheduledLessonsCount { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var timetable = await _context.Timetables
            .Include(t => t.SchoolYear)
            .Include(t => t.ScheduledLessons)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (timetable == null)
        {
            return NotFound();
        }

        Timetable = timetable;
        ScheduledLessonsCount = timetable.ScheduledLessons.Count;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var timetable = await _context.Timetables
            .Include(t => t.ScheduledLessons)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (timetable == null)
        {
            return NotFound();
        }

        try
        {
            _logger.LogInformation("Deleting timetable {Id}: {Name}", id, timetable.Name);

            // Delete all scheduled lessons first (cascade should handle this, but being explicit)
            _context.ScheduledLessons.RemoveRange(timetable.ScheduledLessons);

            // Delete the timetable
            _context.Timetables.Remove(timetable);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Timetable {Id} deleted successfully", id);

            TempData["SuccessMessage"] = $"Timetable '{timetable.Name}' has been deleted successfully.";

            return RedirectToPage("./Generate");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting timetable {Id}", id);
            ModelState.AddModelError(string.Empty, "An error occurred while deleting the timetable.");

            // Reload the timetable for display
            Timetable = timetable;
            ScheduledLessonsCount = timetable.ScheduledLessons.Count;

            return Page();
        }
    }
}
