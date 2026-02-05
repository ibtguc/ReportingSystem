using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.Templates;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<ReportTemplate> Templates { get; set; } = new();
    public Dictionary<int, int> FieldCounts { get; set; } = new();
    public Dictionary<int, int> PeriodCounts { get; set; } = new();
    public Dictionary<int, int> ReportCounts { get; set; } = new();
    public Dictionary<int, int> AssignmentCounts { get; set; } = new();

    public async Task OnGetAsync()
    {
        Templates = await _context.ReportTemplates
            .Include(t => t.CreatedBy)
            .OrderByDescending(t => t.IsActive)
            .ThenBy(t => t.Name)
            .ToListAsync();

        var templateIds = Templates.Select(t => t.Id).ToList();

        FieldCounts = await _context.ReportFields
            .Where(f => templateIds.Contains(f.ReportTemplateId) && f.IsActive)
            .GroupBy(f => f.ReportTemplateId)
            .ToDictionaryAsync(g => g.Key, g => g.Count());

        PeriodCounts = await _context.ReportPeriods
            .Where(p => templateIds.Contains(p.ReportTemplateId) && p.IsActive)
            .GroupBy(p => p.ReportTemplateId)
            .ToDictionaryAsync(g => g.Key, g => g.Count());

        ReportCounts = await _context.Reports
            .Where(r => templateIds.Contains(r.ReportTemplateId))
            .GroupBy(r => r.ReportTemplateId)
            .ToDictionaryAsync(g => g.Key, g => g.Count());

        AssignmentCounts = await _context.ReportTemplateAssignments
            .Where(a => templateIds.Contains(a.ReportTemplateId))
            .GroupBy(a => a.ReportTemplateId)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }
}
