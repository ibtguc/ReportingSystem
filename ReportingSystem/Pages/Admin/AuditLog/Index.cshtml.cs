using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Admin.AuditLog;

[Authorize]
public class IndexModel : PageModel
{
    private readonly AuditService _auditService;
    private readonly ApplicationDbContext _context;

    public IndexModel(AuditService auditService, ApplicationDbContext context)
    {
        _auditService = auditService;
        _context = context;
    }

    public List<Models.AuditLog> AuditLogs { get; set; } = new();
    public int TotalCount { get; set; }
    public (int total, int today, int thisWeek) Stats { get; set; }
    public List<SelectListItem> Users { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? FilterUserId { get; set; }

    [BindProperty(SupportsGet = true)]
    public AuditActionType? ActionType { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ItemType { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public async Task OnGetAsync()
    {
        var (logs, totalCount) = await _auditService.GetAuditLogsAsync(
            userId: FilterUserId,
            actionType: ActionType,
            itemType: ItemType,
            fromDate: FromDate,
            toDate: ToDate,
            page: Page,
            pageSize: PageSize);

        AuditLogs = logs;
        TotalCount = totalCount;
        Stats = await _auditService.GetAuditStatsAsync();

        Users = await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .Select(u => new SelectListItem(u.Name, u.Id.ToString()))
            .ToListAsync();
    }

    public async Task<IActionResult> OnGetExportCsvAsync()
    {
        var csv = await _auditService.ExportToCsvAsync(
            userId: FilterUserId,
            actionType: ActionType,
            itemType: ItemType,
            fromDate: FromDate,
            toDate: ToDate);

        var userId = GetUserId();
        await _auditService.LogAsync(
            AuditActionType.Export,
            "AuditLog",
            userId: userId,
            userName: User.Identity?.Name,
            details: "Exported audit log to CSV");

        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", $"audit-log-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
}
