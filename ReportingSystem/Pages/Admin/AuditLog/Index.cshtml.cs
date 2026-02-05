using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.AuditLog;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Models.AuditLog> AuditLogs { get; set; } = new();
    public int TotalCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ActionFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? EntityTypeFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? UserId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ReportId { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateTo { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; } = 50;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public List<SelectListItem> ActionOptions { get; set; } = new();
    public List<SelectListItem> EntityTypeOptions { get; set; } = new();
    public List<SelectListItem> UserOptions { get; set; } = new();

    // Statistics
    public int TodayCount { get; set; }
    public int ThisWeekCount { get; set; }
    public Dictionary<string, int> ActionCounts { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadFilterOptions();
        await LoadStatistics();
        await LoadAuditLogs();
    }

    private async Task LoadFilterOptions()
    {
        ActionOptions = AuditAction.All
            .Select(a => new SelectListItem { Value = a, Text = AuditAction.DisplayName(a) })
            .ToList();

        EntityTypeOptions = AuditEntityType.All
            .Select(e => new SelectListItem { Value = e, Text = e })
            .ToList();

        UserOptions = await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.Name })
            .ToListAsync();
    }

    private async Task LoadStatistics()
    {
        var today = DateTime.UtcNow.Date;
        var weekAgo = today.AddDays(-7);

        TodayCount = await _context.AuditLogs
            .Where(l => l.Timestamp >= today)
            .CountAsync();

        ThisWeekCount = await _context.AuditLogs
            .Where(l => l.Timestamp >= weekAgo)
            .CountAsync();

        ActionCounts = await _context.AuditLogs
            .Where(l => l.Timestamp >= weekAgo)
            .GroupBy(l => l.Action)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Action, x => x.Count);
    }

    private async Task LoadAuditLogs()
    {
        var query = _context.AuditLogs
            .Include(l => l.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(ActionFilter))
        {
            query = query.Where(l => l.Action == ActionFilter);
        }

        if (!string.IsNullOrEmpty(EntityTypeFilter))
        {
            query = query.Where(l => l.EntityType == EntityTypeFilter);
        }

        if (UserId.HasValue)
        {
            query = query.Where(l => l.UserId == UserId.Value);
        }

        if (ReportId.HasValue)
        {
            query = query.Where(l => l.ReportId == ReportId.Value);
        }

        if (DateFrom.HasValue)
        {
            query = query.Where(l => l.Timestamp >= DateFrom.Value);
        }

        if (DateTo.HasValue)
        {
            var endDate = DateTo.Value.AddDays(1); // Include entire day
            query = query.Where(l => l.Timestamp < endDate);
        }

        if (!string.IsNullOrEmpty(SearchTerm))
        {
            var term = SearchTerm.ToLower();
            query = query.Where(l =>
                (l.EntityName != null && l.EntityName.ToLower().Contains(term)) ||
                (l.FieldName != null && l.FieldName.ToLower().Contains(term)) ||
                (l.OldValue != null && l.OldValue.ToLower().Contains(term)) ||
                (l.NewValue != null && l.NewValue.ToLower().Contains(term)) ||
                (l.UserName != null && l.UserName.ToLower().Contains(term)));
        }

        TotalCount = await query.CountAsync();

        AuditLogs = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostExportAsync()
    {
        var logs = await _context.AuditLogs
            .Include(l => l.User)
            .OrderByDescending(l => l.Timestamp)
            .Take(10000)
            .ToListAsync();

        var csv = "Timestamp,Action,EntityType,EntityId,EntityName,Field,OldValue,NewValue,UserName,UserEmail,Reason\n";
        foreach (var log in logs)
        {
            csv += $"\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{log.Action}\",\"{log.EntityType}\",{log.EntityId},\"{Escape(log.EntityName)}\",\"{Escape(log.FieldName)}\",\"{Escape(log.OldValue)}\",\"{Escape(log.NewValue)}\",\"{Escape(log.UserName)}\",\"{Escape(log.UserEmail)}\",\"{Escape(log.Reason)}\"\n";
        }

        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"audit_log_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", "");
    }
}
