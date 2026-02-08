using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Reports;

[Authorize]
public class EditModel : PageModel
{
    private readonly ReportService _reportService;
    private readonly IWebHostEnvironment _env;

    public EditModel(ReportService reportService, IWebHostEnvironment env)
    {
        _reportService = reportService;
        _env = env;
    }

    [BindProperty]
    public Report Report { get; set; } = null!;

    [BindProperty]
    public List<IFormFile>? NewAttachments { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Revise { get; set; }

    public bool IsRevision { get; set; }
    public Report? OriginalReport { get; set; }
    public List<SelectListItem> CommitteeOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id, bool revise = false)
    {
        var userId = GetUserId();
        var report = await _reportService.GetReportByIdAsync(id);
        if (report == null) return NotFound();

        Revise = revise;

        if (revise && report.Status == ReportStatus.FeedbackRequested)
        {
            // Creating a revision — pre-fill from original
            IsRevision = true;
            OriginalReport = report;
            Report = new Report
            {
                Title = report.Title,
                ReportType = report.ReportType,
                CommitteeId = report.CommitteeId,
                BodyContent = report.BodyContent,
                SuggestedAction = report.SuggestedAction,
                NeededResources = report.NeededResources,
                NeededSupport = report.NeededSupport,
                SpecialRemarks = report.SpecialRemarks,
                IsConfidential = report.IsConfidential
            };
        }
        else if (report.Status == ReportStatus.Draft && report.AuthorId == userId)
        {
            Report = report;
        }
        else
        {
            TempData["ErrorMessage"] = "This report cannot be edited in its current state.";
            return RedirectToPage("Details", new { id });
        }

        var committees = await _reportService.GetUserCommitteesAsync(userId);
        CommitteeOptions = committees.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var userId = GetUserId();

        ModelState.Remove("Report.Author");
        ModelState.Remove("Report.Committee");

        if (!ModelState.IsValid)
        {
            var committees = await _reportService.GetUserCommitteesAsync(userId);
            CommitteeOptions = committees.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
            return Page();
        }

        if (Revise)
        {
            // Create a revision
            var revised = await _reportService.ReviseReportAsync(id, Report, userId);
            if (revised == null)
            {
                TempData["ErrorMessage"] = "Unable to create revision.";
                return RedirectToPage("Details", new { id });
            }

            // Handle new attachments
            await HandleAttachments(revised.Id, userId);

            TempData["SuccessMessage"] = $"Revision v{revised.Version} created.";
            return RedirectToPage("Details", new { id = revised.Id });
        }
        else
        {
            // Update existing draft
            var existing = await _reportService.GetReportByIdAsync(id);
            if (existing == null || existing.Status != ReportStatus.Draft || existing.AuthorId != userId)
            {
                TempData["ErrorMessage"] = "Cannot edit this report.";
                return RedirectToPage("Details", new { id });
            }

            existing.Title = Report.Title;
            existing.ReportType = Report.ReportType;
            existing.CommitteeId = Report.CommitteeId;
            existing.BodyContent = Report.BodyContent;
            existing.SuggestedAction = Report.SuggestedAction;
            existing.NeededResources = Report.NeededResources;
            existing.NeededSupport = Report.NeededSupport;
            existing.SpecialRemarks = Report.SpecialRemarks;
            existing.IsConfidential = Report.IsConfidential;

            await _reportService.UpdateReportAsync(existing, userId);

            // Handle new attachments
            await HandleAttachments(id, userId);

            TempData["SuccessMessage"] = "Report updated.";
            return RedirectToPage("Details", new { id });
        }
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

    private async Task HandleAttachments(int reportId, int userId)
    {
        if (NewAttachments != null)
        {
            foreach (var file in NewAttachments.Where(f => f.Length > 0))
            {
                var storagePath = await SaveFileAsync(file);
                await _reportService.AddAttachmentAsync(
                    reportId, file.FileName, storagePath, file.ContentType, file.Length, userId);
            }
        }
    }

    private async Task<string> SaveFileAsync(IFormFile file)
    {
        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "attachments");
        Directory.CreateDirectory(uploadsDir);
        var uniqueName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsDir, uniqueName);
        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);
        return Path.Combine("uploads", "attachments", uniqueName);
    }
}
