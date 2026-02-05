using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;
using System.Security.Claims;
using System.Text.Json;

namespace ReportingSystem.Pages.Admin.ReportBuilder;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Filter parameters
    [BindProperty(SupportsGet = true)]
    public string ReportType { get; set; } = SavedReportType.Reports;

    [BindProperty(SupportsGet = true)]
    public int? TemplateId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? PeriodId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Role { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Action { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? SavedId { get; set; }

    public SavedReport? LoadedSavedReport { get; set; }

    // Dropdown options
    public List<ReportTemplate> Templates { get; set; } = new();
    public List<ReportPeriod> Periods { get; set; } = new();
    public List<string> AuditActions { get; set; } = new();

    // Results
    public List<Report> ReportsData { get; set; } = new();
    public List<SuggestedAction> SuggestedActionsData { get; set; } = new();
    public List<ResourceRequest> ResourceRequestsData { get; set; } = new();
    public List<User> UsersData { get; set; } = new();
    public List<AuditLog> AuditLogData { get; set; } = new();

    public int TotalCount { get; set; }
    public bool HasResults => TotalCount > 0;

    public async Task OnGetAsync()
    {
        // Load saved report configuration if specified
        if (SavedId.HasValue)
        {
            await LoadSavedReportConfiguration(SavedId.Value);
        }

        await LoadDropdownOptions();
        await LoadReportData();
    }

    private async Task LoadSavedReportConfiguration(int savedId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(userIdClaim, out var userId);

        var savedReport = await _context.SavedReports
            .FirstOrDefaultAsync(r => r.Id == savedId && (r.CreatedById == userId || r.IsPublic));

        if (savedReport == null) return;

        LoadedSavedReport = savedReport;
        ReportType = savedReport.ReportType;

        // Parse filter configuration
        try
        {
            var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(savedReport.FilterConfiguration);
            if (config != null)
            {
                if (config.TryGetValue("templateId", out var templateId) && templateId.TryGetInt32(out var tid))
                    TemplateId = tid;
                if (config.TryGetValue("periodId", out var periodId) && periodId.TryGetInt32(out var pid))
                    PeriodId = pid;
                if (config.TryGetValue("status", out var status))
                    Status = status.GetString();
                if (config.TryGetValue("fromDate", out var fromDate))
                    FromDate = DateTime.TryParse(fromDate.GetString(), out var fd) ? fd : null;
                if (config.TryGetValue("toDate", out var toDate))
                    ToDate = DateTime.TryParse(toDate.GetString(), out var td) ? td : null;
                if (config.TryGetValue("role", out var role))
                    Role = role.GetString();
                if (config.TryGetValue("action", out var action))
                    Action = action.GetString();
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse saved report configuration for report {SavedId}", savedId);
        }

        // Update run statistics
        savedReport.LastRunAt = DateTime.UtcNow;
        savedReport.RunCount++;
        await _context.SaveChangesAsync();
    }

    private async Task LoadDropdownOptions()
    {
        Templates = await _context.ReportTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync();

        Periods = await _context.ReportPeriods
            .OrderByDescending(p => p.EndDate)
            .Take(50)
            .ToListAsync();

        AuditActions = await _context.AuditLogs
            .Select(a => a.Action)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();
    }

    private async Task LoadReportData()
    {
        switch (ReportType)
        {
            case SavedReportType.Reports:
                await LoadReportsData();
                break;
            case SavedReportType.SuggestedActions:
                await LoadSuggestedActionsData();
                break;
            case SavedReportType.ResourceRequests:
                await LoadResourceRequestsData();
                break;
            case SavedReportType.Users:
                await LoadUsersData();
                break;
            case SavedReportType.AuditLog:
                await LoadAuditLogData();
                break;
        }
    }

    private async Task LoadReportsData()
    {
        var query = _context.Reports
            .Include(r => r.ReportTemplate)
            .Include(r => r.ReportPeriod)
            .Include(r => r.SubmittedBy)
            .ThenInclude(u => u.OrganizationalUnit)
            .Include(r => r.AssignedReviewer)
            .AsQueryable();

        if (TemplateId.HasValue)
            query = query.Where(r => r.ReportTemplateId == TemplateId.Value);
        if (PeriodId.HasValue)
            query = query.Where(r => r.ReportPeriodId == PeriodId.Value);
        if (!string.IsNullOrEmpty(Status))
            query = query.Where(r => r.Status == Status);
        if (FromDate.HasValue)
            query = query.Where(r => r.CreatedAt >= FromDate.Value);
        if (ToDate.HasValue)
            query = query.Where(r => r.CreatedAt <= ToDate.Value.AddDays(1));

        TotalCount = await query.CountAsync();
        ReportsData = await query.OrderByDescending(r => r.CreatedAt).Take(100).ToListAsync();
    }

    private async Task LoadSuggestedActionsData()
    {
        var query = _context.SuggestedActions
            .Include(a => a.Report)
            .ThenInclude(r => r.ReportTemplate)
            .Include(a => a.Report)
            .ThenInclude(r => r.ReportPeriod)
            .Include(a => a.ReviewedBy)
            .AsQueryable();

        if (TemplateId.HasValue)
            query = query.Where(a => a.Report.ReportTemplateId == TemplateId.Value);
        if (PeriodId.HasValue)
            query = query.Where(a => a.Report.ReportPeriodId == PeriodId.Value);
        if (FromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= FromDate.Value);
        if (ToDate.HasValue)
            query = query.Where(a => a.CreatedAt <= ToDate.Value.AddDays(1));

        TotalCount = await query.CountAsync();
        SuggestedActionsData = await query.OrderByDescending(a => a.CreatedAt).Take(100).ToListAsync();
    }

    private async Task LoadResourceRequestsData()
    {
        var query = _context.ResourceRequests
            .Include(r => r.Report)
            .ThenInclude(rep => rep.ReportTemplate)
            .Include(r => r.Report)
            .ThenInclude(rep => rep.ReportPeriod)
            .Include(r => r.ReviewedBy)
            .AsQueryable();

        if (TemplateId.HasValue)
            query = query.Where(r => r.Report.ReportTemplateId == TemplateId.Value);
        if (PeriodId.HasValue)
            query = query.Where(r => r.Report.ReportPeriodId == PeriodId.Value);
        if (FromDate.HasValue)
            query = query.Where(r => r.CreatedAt >= FromDate.Value);
        if (ToDate.HasValue)
            query = query.Where(r => r.CreatedAt <= ToDate.Value.AddDays(1));

        TotalCount = await query.CountAsync();
        ResourceRequestsData = await query.OrderByDescending(r => r.CreatedAt).Take(100).ToListAsync();
    }

    private async Task LoadUsersData()
    {
        var query = _context.Users
            .Include(u => u.OrganizationalUnit)
            .Where(u => u.IsActive)
            .AsQueryable();

        if (!string.IsNullOrEmpty(Role))
            query = query.Where(u => u.Role == Role);
        if (FromDate.HasValue)
            query = query.Where(u => u.CreatedAt >= FromDate.Value);
        if (ToDate.HasValue)
            query = query.Where(u => u.CreatedAt <= ToDate.Value.AddDays(1));

        TotalCount = await query.CountAsync();
        UsersData = await query.OrderBy(u => u.Name).Take(100).ToListAsync();
    }

    private async Task LoadAuditLogData()
    {
        var query = _context.AuditLogs
            .Include(a => a.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(Action))
            query = query.Where(a => a.Action == Action);
        if (FromDate.HasValue)
            query = query.Where(a => a.Timestamp >= FromDate.Value);
        if (ToDate.HasValue)
            query = query.Where(a => a.Timestamp <= ToDate.Value.AddDays(1));

        TotalCount = await query.CountAsync();
        AuditLogData = await query.OrderByDescending(a => a.Timestamp).Take(100).ToListAsync();
    }

    public async Task<IActionResult> OnPostSaveReportAsync(
        string SavedReportName,
        string? SavedReportDescription,
        string ReportType,
        string FilterConfig,
        bool IsPublic = false,
        bool IsPinned = false)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            TempData["ErrorMessage"] = "Unable to identify current user.";
            return RedirectToPage();
        }

        var savedReport = new SavedReport
        {
            Name = SavedReportName,
            Description = SavedReportDescription,
            ReportType = ReportType,
            FilterConfiguration = FilterConfig,
            IsPublic = IsPublic,
            IsPinnedToDashboard = IsPinned,
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.SavedReports.Add(savedReport);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} saved report configuration: {ReportName}", userId, SavedReportName);
        TempData["SuccessMessage"] = $"Report '{SavedReportName}' saved successfully!";
        return RedirectToPage(new { ReportType });
    }

    public string GetExportType() => ReportType switch
    {
        SavedReportType.Reports => "reports",
        SavedReportType.SuggestedActions => "suggested-actions",
        SavedReportType.ResourceRequests => "resource-requests",
        SavedReportType.AuditLog => "audit-log",
        _ => "reports"
    };

    public string GetExportQueryString()
    {
        var qs = "";
        if (TemplateId.HasValue) qs += $"&templateId={TemplateId}";
        if (PeriodId.HasValue) qs += $"&periodId={PeriodId}";
        if (!string.IsNullOrEmpty(Status)) qs += $"&status={Status}";
        if (FromDate.HasValue) qs += $"&fromDate={FromDate:yyyy-MM-dd}";
        if (ToDate.HasValue) qs += $"&toDate={ToDate:yyyy-MM-dd}";
        if (!string.IsNullOrEmpty(Action)) qs += $"&action={Action}";
        return qs;
    }

    public string GetFilterConfigJson()
    {
        var config = new Dictionary<string, object?>();
        if (TemplateId.HasValue) config["templateId"] = TemplateId;
        if (PeriodId.HasValue) config["periodId"] = PeriodId;
        if (!string.IsNullOrEmpty(Status)) config["status"] = Status;
        if (FromDate.HasValue) config["fromDate"] = FromDate.Value.ToString("yyyy-MM-dd");
        if (ToDate.HasValue) config["toDate"] = ToDate.Value.ToString("yyyy-MM-dd");
        if (!string.IsNullOrEmpty(Role)) config["role"] = Role;
        if (!string.IsNullOrEmpty(Action)) config["action"] = Action;
        return JsonSerializer.Serialize(config);
    }
}
