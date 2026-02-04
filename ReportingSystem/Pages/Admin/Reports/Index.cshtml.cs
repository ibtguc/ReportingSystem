using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.Reports;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Report> Reports { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? TemplateId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? PeriodId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    public List<SelectListItem> TemplateOptions { get; set; } = new();
    public List<SelectListItem> PeriodOptions { get; set; } = new();
    public List<SelectListItem> StatusOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        var query = _context.Reports
            .Include(r => r.ReportTemplate)
            .Include(r => r.ReportPeriod)
            .Include(r => r.SubmittedBy)
                .ThenInclude(u => u.OrganizationalUnit)
            .Include(r => r.AssignedReviewer)
            .AsQueryable();

        if (TemplateId.HasValue)
        {
            query = query.Where(r => r.ReportTemplateId == TemplateId.Value);
        }

        if (PeriodId.HasValue)
        {
            query = query.Where(r => r.ReportPeriodId == PeriodId.Value);
        }

        if (!string.IsNullOrEmpty(StatusFilter))
        {
            query = query.Where(r => r.Status == StatusFilter);
        }

        Reports = await query
            .OrderByDescending(r => r.CreatedAt)
            .Take(100)
            .ToListAsync();

        await LoadDropdownsAsync();
    }

    private async Task LoadDropdownsAsync()
    {
        TemplateOptions = await _context.ReportTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new SelectListItem(t.Name, t.Id.ToString()))
            .ToListAsync();

        if (TemplateId.HasValue)
        {
            PeriodOptions = await _context.ReportPeriods
                .Where(p => p.ReportTemplateId == TemplateId.Value)
                .OrderByDescending(p => p.StartDate)
                .Select(p => new SelectListItem(p.Name, p.Id.ToString()))
                .ToListAsync();
        }

        StatusOptions = ReportStatus.All
            .Select(s => new SelectListItem(ReportStatus.DisplayName(s), s))
            .ToList();
    }
}
