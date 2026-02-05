using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.Templates;

public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public ReportTemplate Template { get; set; } = null!;

    [BindProperty]
    public bool BumpVersion { get; set; }

    [BindProperty]
    public string? NewVersionNotes { get; set; }

    public List<SelectListItem> ScheduleOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var template = await _context.ReportTemplates.FindAsync(id);
        if (template == null)
        {
            return NotFound();
        }

        Template = template;
        LoadDropdowns();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            LoadDropdowns();
            return Page();
        }

        var existing = await _context.ReportTemplates.FindAsync(Template.Id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.Name = Template.Name;
        existing.Description = Template.Description;
        existing.Schedule = Template.Schedule;
        existing.IncludeSuggestedActions = Template.IncludeSuggestedActions;
        existing.IncludeNeededResources = Template.IncludeNeededResources;
        existing.IncludeNeededSupport = Template.IncludeNeededSupport;
        existing.AutoSaveIntervalSeconds = Template.AutoSaveIntervalSeconds;
        existing.AllowPrePopulation = Template.AllowPrePopulation;
        existing.AllowBulkImport = Template.AllowBulkImport;
        existing.MaxAttachmentSizeMb = Template.MaxAttachmentSizeMb;
        existing.AllowedFileTypes = Template.AllowedFileTypes;
        existing.IsActive = Template.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        if (BumpVersion)
        {
            existing.Version++;
            existing.VersionNotes = NewVersionNotes;
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Template '{existing.Name}' updated successfully." +
            (BumpVersion ? $" Version bumped to v{existing.Version}." : "");
        return RedirectToPage("Details", new { id = existing.Id });
    }

    private void LoadDropdowns()
    {
        ScheduleOptions = ReportSchedule.All
            .Select(s => new SelectListItem(ReportSchedule.DisplayName(s), s))
            .ToList();
    }
}
