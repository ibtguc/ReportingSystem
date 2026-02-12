using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Reports;

[Authorize]
public class CreateModel : PageModel
{
    private readonly ReportService _reportService;
    private readonly ReportTemplateService _templateService;
    private readonly IWebHostEnvironment _env;

    public CreateModel(ReportService reportService, ReportTemplateService templateService, IWebHostEnvironment env)
    {
        _reportService = reportService;
        _templateService = templateService;
        _env = env;
    }

    [BindProperty]
    public Report Report { get; set; } = new();

    [BindProperty]
    public List<IFormFile>? Attachments { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? TemplateId { get; set; }

    public List<SelectListItem> CommitteeOptions { get; set; } = new();
    public List<ReportTemplate> AvailableTemplates { get; set; } = new();
    public ReportTemplate? SelectedTemplate { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetUserId();
        var committees = await _reportService.GetUserCommitteesAsync(userId);
        if (!committees.Any())
        {
            TempData["ErrorMessage"] = "You are not a member of any committee. Contact an administrator.";
            return RedirectToPage("Index");
        }
        CommitteeOptions = committees.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();

        // Load all active templates for the template picker
        AvailableTemplates = await _templateService.GetTemplatesAsync();

        // If a template was selected, pre-fill the form
        if (TemplateId.HasValue)
        {
            SelectedTemplate = await _templateService.GetTemplateByIdAsync(TemplateId.Value);
            if (SelectedTemplate != null)
            {
                Report.TemplateId = SelectedTemplate.Id;
                if (!string.IsNullOrEmpty(SelectedTemplate.BodyTemplate))
                    Report.BodyContent = SelectedTemplate.BodyTemplate;
                if (SelectedTemplate.ReportType.HasValue)
                    Report.ReportType = SelectedTemplate.ReportType.Value;
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = GetUserId();
        var committees = await _reportService.GetUserCommitteesAsync(userId);
        CommitteeOptions = committees.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
        AvailableTemplates = await _templateService.GetTemplatesAsync();

        // Load selected template for validation
        if (Report.TemplateId.HasValue)
            SelectedTemplate = await _templateService.GetTemplateByIdAsync(Report.TemplateId.Value);

        // Verify user is member of selected committee
        if (!await _reportService.IsUserMemberOfCommitteeAsync(userId, Report.CommitteeId))
        {
            ModelState.AddModelError("", "You are not a member of the selected committee.");
            return Page();
        }

        // Remove navigation property validations
        ModelState.Remove("Report.Author");
        ModelState.Remove("Report.Committee");
        ModelState.Remove("Report.Template");

        // Template-specific required field validation
        if (SelectedTemplate != null)
        {
            if (SelectedTemplate.RequireSuggestedAction && string.IsNullOrWhiteSpace(Report.SuggestedAction))
                ModelState.AddModelError("Report.SuggestedAction", "Suggested Action is required by this template.");
            if (SelectedTemplate.RequireNeededResources && string.IsNullOrWhiteSpace(Report.NeededResources))
                ModelState.AddModelError("Report.NeededResources", "Needed Resources is required by this template.");
            if (SelectedTemplate.RequireNeededSupport && string.IsNullOrWhiteSpace(Report.NeededSupport))
                ModelState.AddModelError("Report.NeededSupport", "Needed Support is required by this template.");
            if (SelectedTemplate.RequireSpecialRemarks && string.IsNullOrWhiteSpace(Report.SpecialRemarks))
                ModelState.AddModelError("Report.SpecialRemarks", "Special Remarks is required by this template.");
        }

        if (!ModelState.IsValid)
            return Page();

        var report = await _reportService.CreateReportAsync(Report, userId);

        // Handle file attachments
        if (Attachments != null)
        {
            foreach (var file in Attachments.Where(f => f.Length > 0))
            {
                var storagePath = await SaveFileAsync(file);
                await _reportService.AddAttachmentAsync(
                    report.Id, file.FileName, storagePath, file.ContentType, file.Length, userId);
            }
        }

        TempData["SuccessMessage"] = $"Report \"{report.Title}\" created as draft.";
        return RedirectToPage("Details", new { id = report.Id });
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

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
