using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;
using System.Security.Claims;

namespace ReportingSystem.Pages.Admin.ReportBuilder;

public class SavedReportsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SavedReportsModel> _logger;

    public SavedReportsModel(ApplicationDbContext context, ILogger<SavedReportsModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public List<SavedReport> MyReports { get; set; } = new();
    public List<SavedReport> SharedReports { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            return;

        MyReports = await _context.SavedReports
            .Include(r => r.CreatedBy)
            .Where(r => r.CreatedById == userId)
            .OrderByDescending(r => r.LastRunAt ?? r.CreatedAt)
            .ToListAsync();

        SharedReports = await _context.SavedReports
            .Include(r => r.CreatedBy)
            .Where(r => r.IsPublic && r.CreatedById != userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            TempData["ErrorMessage"] = "Unable to identify current user.";
            return RedirectToPage();
        }

        var report = await _context.SavedReports.FindAsync(id);
        if (report == null)
        {
            TempData["ErrorMessage"] = "Report not found.";
            return RedirectToPage();
        }

        if (report.CreatedById != userId)
        {
            TempData["ErrorMessage"] = "You can only delete your own saved reports.";
            return RedirectToPage();
        }

        _context.SavedReports.Remove(report);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} deleted saved report: {ReportName}", userId, report.Name);
        TempData["SuccessMessage"] = $"Report '{report.Name}' deleted successfully.";
        return RedirectToPage();
    }
}
