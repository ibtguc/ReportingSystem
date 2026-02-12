using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Archives;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ConfidentialityService _confidentialityService;
    private readonly ReportService _reportService;

    public IndexModel(ApplicationDbContext context, ConfidentialityService confidentialityService, ReportService reportService)
    {
        _context = context;
        _confidentialityService = confidentialityService;
        _reportService = reportService;
    }

    [BindProperty(SupportsGet = true)]
    public string? ContentType { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CommitteeId { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    public List<ArchiveItem> ArchivedItems { get; set; } = new();
    public List<SelectListItem> Committees { get; set; } = new();

    // Stats
    public int ArchivedReports { get; set; }
    public int ClosedDirectives { get; set; }
    public int FinalizedMeetings { get; set; }

    public async Task OnGetAsync()
    {
        var userId = GetUserId();

        // Load stats
        ArchivedReports = await _context.Reports.CountAsync(r => r.Status == ReportStatus.Summarized);
        ClosedDirectives = await _context.Directives.CountAsync(d => d.Status == DirectiveStatus.Closed);
        FinalizedMeetings = await _context.Meetings.CountAsync(m => m.Status == MeetingStatus.Finalized);

        // Load committees for filter
        Committees = await _context.Committees
            .Where(c => c.IsActive)
            .OrderBy(c => c.HierarchyLevel).ThenBy(c => c.Name)
            .Select(c => new SelectListItem($"[{c.HierarchyLevel}] {c.Name}", c.Id.ToString()))
            .ToListAsync();

        // Load archived items based on filters
        if (ContentType == null || ContentType == "Report")
            await LoadArchivedReportsAsync(userId);
        if (ContentType == null || ContentType == "Directive")
            await LoadClosedDirectivesAsync(userId);
        if (ContentType == null || ContentType == "Meeting")
            await LoadFinalizedMeetingsAsync(userId);

        ArchivedItems = ArchivedItems.OrderByDescending(a => a.ArchivedDate).ToList();
    }

    private async Task LoadArchivedReportsAsync(int userId)
    {
        // Apply visibility filtering — only show reports from visible committees
        var visibleCommitteeIds = await _reportService.GetVisibleCommitteeIdsAsync(userId);
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
        var isGlobal = role is "Chairman" or "ChairmanOffice" or "Admin";

        var query = _context.Reports
            .Include(r => r.Author)
            .Include(r => r.Committee)
            .Where(r => r.Status == ReportStatus.Summarized);

        if (!isGlobal)
            query = query.Where(r => visibleCommitteeIds.Contains(r.CommitteeId));

        if (CommitteeId.HasValue)
            query = query.Where(r => r.CommitteeId == CommitteeId.Value);
        if (FromDate.HasValue)
            query = query.Where(r => r.CreatedAt >= FromDate.Value);
        if (ToDate.HasValue)
            query = query.Where(r => r.CreatedAt <= ToDate.Value);

        var reports = await query.Take(100).ToListAsync();
        reports = await _confidentialityService.FilterAccessibleReportsAsync(reports, userId);

        foreach (var r in reports)
        {
            ArchivedItems.Add(new ArchiveItem
            {
                ItemType = "Report",
                ItemId = r.Id,
                Title = r.Title,
                Author = r.Author?.Name ?? "Unknown",
                Committee = r.Committee?.Name ?? "Unknown",
                ArchivedDate = r.UpdatedAt ?? r.CreatedAt,
                CreatedDate = r.CreatedAt,
                Url = $"/Reports/Details/{r.Id}",
                SubType = r.ReportType.ToString()
            });
        }
    }

    private async Task LoadClosedDirectivesAsync(int userId)
    {
        var query = _context.Directives
            .Include(d => d.Issuer)
            .Include(d => d.TargetCommittee)
            .Where(d => d.Status == DirectiveStatus.Closed);

        if (CommitteeId.HasValue)
            query = query.Where(d => d.TargetCommitteeId == CommitteeId.Value);
        if (FromDate.HasValue)
            query = query.Where(d => d.CreatedAt >= FromDate.Value);
        if (ToDate.HasValue)
            query = query.Where(d => d.CreatedAt <= ToDate.Value);

        var directives = await query.Take(100).ToListAsync();
        directives = await _confidentialityService.FilterAccessibleDirectivesAsync(directives, userId);

        foreach (var d in directives)
        {
            ArchivedItems.Add(new ArchiveItem
            {
                ItemType = "Directive",
                ItemId = d.Id,
                Title = d.Title,
                Author = d.Issuer?.Name ?? "Unknown",
                Committee = d.TargetCommittee?.Name ?? "Unknown",
                ArchivedDate = d.UpdatedAt ?? d.CreatedAt,
                CreatedDate = d.CreatedAt,
                Url = $"/Directives/Details/{d.Id}",
                SubType = d.DirectiveType.ToString()
            });
        }
    }

    private async Task LoadFinalizedMeetingsAsync(int userId)
    {
        var query = _context.Meetings
            .Include(m => m.Committee)
            .Include(m => m.Moderator)
            .Where(m => m.Status == MeetingStatus.Finalized);

        if (CommitteeId.HasValue)
            query = query.Where(m => m.CommitteeId == CommitteeId.Value);
        if (FromDate.HasValue)
            query = query.Where(m => m.ScheduledAt >= FromDate.Value);
        if (ToDate.HasValue)
            query = query.Where(m => m.ScheduledAt <= ToDate.Value);

        var meetings = await query.Take(100).ToListAsync();
        meetings = await _confidentialityService.FilterAccessibleMeetingsAsync(meetings, userId);

        foreach (var m in meetings)
        {
            ArchivedItems.Add(new ArchiveItem
            {
                ItemType = "Meeting",
                ItemId = m.Id,
                Title = m.Title,
                Author = m.Moderator?.Name ?? "Unknown",
                Committee = m.Committee?.Name ?? "Unknown",
                ArchivedDate = m.MinutesFinalizedAt ?? m.ScheduledAt,
                CreatedDate = m.CreatedAt,
                Url = $"/Meetings/Details/{m.Id}",
                SubType = m.MeetingType.ToString()
            });
        }
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
}

public class ArchiveItem
{
    public string ItemType { get; set; } = string.Empty;
    public int ItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Committee { get; set; } = string.Empty;
    public DateTime ArchivedDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public string Url { get; set; } = string.Empty;
    public string SubType { get; set; } = string.Empty;
}
