using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;
using System.Text;

namespace ReportingSystem.Services;

/// <summary>
/// Service for exporting data to various formats (CSV, Excel, HTML for PDF).
/// </summary>
public class ExportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ExportService> _logger;

    public ExportService(ApplicationDbContext context, ILogger<ExportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region CSV Export

    /// <summary>
    /// Export reports list to CSV format.
    /// </summary>
    public async Task<byte[]> ExportReportsToCsvAsync(ReportExportFilter? filter = null)
    {
        var query = _context.Reports
            .Include(r => r.ReportTemplate)
            .Include(r => r.ReportPeriod)
            .Include(r => r.SubmittedBy)
            .ThenInclude(u => u.OrganizationalUnit)
            .Include(r => r.AssignedReviewer)
            .AsQueryable();

        if (filter != null)
        {
            if (filter.TemplateId.HasValue)
                query = query.Where(r => r.ReportTemplateId == filter.TemplateId.Value);
            if (filter.PeriodId.HasValue)
                query = query.Where(r => r.ReportPeriodId == filter.PeriodId.Value);
            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(r => r.Status == filter.Status);
            if (filter.SubmittedById.HasValue)
                query = query.Where(r => r.SubmittedById == filter.SubmittedById.Value);
            if (filter.FromDate.HasValue)
                query = query.Where(r => r.CreatedAt >= filter.FromDate.Value);
            if (filter.ToDate.HasValue)
                query = query.Where(r => r.CreatedAt <= filter.ToDate.Value);
        }

        var reports = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("ID,Template,Period,Status,Submitted By,Organization,Created At,Submitted At,Reviewed At,Reviewer");

        foreach (var r in reports)
        {
            sb.AppendLine($"{r.Id}," +
                $"\"{EscapeCsv(r.ReportTemplate.Name)}\"," +
                $"\"{EscapeCsv(r.ReportPeriod.Name)}\"," +
                $"\"{ReportStatus.DisplayName(r.Status)}\"," +
                $"\"{EscapeCsv(r.SubmittedBy?.Name ?? "N/A")}\"," +
                $"\"{EscapeCsv(r.SubmittedBy?.OrganizationalUnit?.Name ?? "N/A")}\"," +
                $"{r.CreatedAt:yyyy-MM-dd HH:mm}," +
                $"{(r.SubmittedAt?.ToString("yyyy-MM-dd HH:mm") ?? "")}," +
                $"{(r.ReviewedAt?.ToString("yyyy-MM-dd HH:mm") ?? "")}," +
                $"\"{EscapeCsv(r.AssignedReviewer?.Name ?? "")}\"");
        }

        _logger.LogInformation("Exported {Count} reports to CSV", reports.Count);
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    /// <summary>
    /// Export a single report with all field values to CSV.
    /// </summary>
    public async Task<byte[]> ExportReportDetailToCsvAsync(int reportId)
    {
        var report = await _context.Reports
            .Include(r => r.ReportTemplate)
            .Include(r => r.ReportPeriod)
            .Include(r => r.SubmittedBy)
            .Include(r => r.FieldValues)
            .ThenInclude(fv => fv.ReportField)
            .FirstOrDefaultAsync(r => r.Id == reportId);

        if (report == null)
            return Encoding.UTF8.GetBytes("Report not found");

        var sb = new StringBuilder();
        sb.AppendLine($"Report: {report.ReportTemplate.Name}");
        sb.AppendLine($"Period: {report.ReportPeriod.Name}");
        sb.AppendLine($"Status: {ReportStatus.DisplayName(report.Status)}");
        sb.AppendLine($"Submitted By: {report.SubmittedBy?.Name ?? "N/A"}");
        sb.AppendLine($"Created: {report.CreatedAt:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"Submitted: {(report.SubmittedAt?.ToString("yyyy-MM-dd HH:mm") ?? "Not submitted")}");
        sb.AppendLine();
        sb.AppendLine("Field,Section,Value");

        foreach (var fv in report.FieldValues.OrderBy(f => f.ReportField.Section).ThenBy(f => f.ReportField.OrderIndex))
        {
            sb.AppendLine($"\"{EscapeCsv(fv.ReportField.Label)}\"," +
                $"\"{EscapeCsv(fv.ReportField.Section)}\"," +
                $"\"{EscapeCsv(fv.Value ?? "")}\"");
        }

        _logger.LogInformation("Exported report {ReportId} detail to CSV", reportId);
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    /// <summary>
    /// Export suggested actions to CSV.
    /// </summary>
    public async Task<byte[]> ExportSuggestedActionsToCsvAsync(int? reportId = null)
    {
        var query = _context.SuggestedActions
            .Include(a => a.Report)
            .ThenInclude(r => r.ReportTemplate)
            .Include(a => a.ReviewedBy)
            .AsQueryable();

        if (reportId.HasValue)
            query = query.Where(a => a.ReportId == reportId.Value);

        var actions = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("ID,Title,Category,Priority,Status,Report,Created,Reviewed By");

        foreach (var a in actions)
        {
            sb.AppendLine($"{a.Id}," +
                $"\"{EscapeCsv(a.Title)}\"," +
                $"\"{ActionCategory.DisplayName(a.Category)}\"," +
                $"\"{ActionPriority.DisplayName(a.Priority)}\"," +
                $"\"{ActionStatus.DisplayName(a.Status)}\"," +
                $"\"{EscapeCsv(a.Report.ReportTemplate.Name)}\"," +
                $"{a.CreatedAt:yyyy-MM-dd}," +
                $"\"{EscapeCsv(a.ReviewedBy?.Name ?? "")}\"");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    /// <summary>
    /// Export resource requests to CSV.
    /// </summary>
    public async Task<byte[]> ExportResourceRequestsToCsvAsync(int? reportId = null)
    {
        var query = _context.ResourceRequests
            .Include(r => r.Report)
            .ThenInclude(r => r.ReportTemplate)
            .Include(r => r.ReviewedBy)
            .AsQueryable();

        if (reportId.HasValue)
            query = query.Where(r => r.ReportId == reportId.Value);

        var requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("ID,Title,Category,Urgency,Status,Estimated Cost,Approved Amount,Currency,Report,Created");

        foreach (var r in requests)
        {
            sb.AppendLine($"{r.Id}," +
                $"\"{EscapeCsv(r.Title)}\"," +
                $"\"{ResourceCategory.DisplayName(r.Category)}\"," +
                $"\"{ResourceUrgency.DisplayName(r.Urgency)}\"," +
                $"\"{ResourceStatus.DisplayName(r.Status)}\"," +
                $"{r.EstimatedCost?.ToString("F2") ?? ""}," +
                $"{r.ApprovedAmount?.ToString("F2") ?? ""}," +
                $"{r.Currency ?? "EGP"}," +
                $"\"{EscapeCsv(r.Report.ReportTemplate.Name)}\"," +
                $"{r.CreatedAt:yyyy-MM-dd}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    /// <summary>
    /// Export audit log to CSV.
    /// </summary>
    public async Task<byte[]> ExportAuditLogToCsvAsync(AuditLogExportFilter? filter = null)
    {
        var query = _context.AuditLogs
            .Include(a => a.User)
            .AsQueryable();

        if (filter != null)
        {
            if (!string.IsNullOrEmpty(filter.Action))
                query = query.Where(a => a.Action == filter.Action);
            if (!string.IsNullOrEmpty(filter.EntityType))
                query = query.Where(a => a.EntityType == filter.EntityType);
            if (filter.UserId.HasValue)
                query = query.Where(a => a.UserId == filter.UserId.Value);
            if (filter.FromDate.HasValue)
                query = query.Where(a => a.Timestamp >= filter.FromDate.Value);
            if (filter.ToDate.HasValue)
                query = query.Where(a => a.Timestamp <= filter.ToDate.Value);
        }

        var logs = await query.OrderByDescending(a => a.Timestamp).Take(10000).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("ID,Timestamp,User,Action,Entity Type,Entity ID,Old Value,New Value,IP Address");

        foreach (var log in logs)
        {
            sb.AppendLine($"{log.Id}," +
                $"{log.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                $"\"{EscapeCsv(log.User?.Name ?? "System")}\"," +
                $"\"{log.Action}\"," +
                $"\"{log.EntityType}\"," +
                $"{log.EntityId ?? 0}," +
                $"\"{EscapeCsv(TruncateValue(log.OldValue, 100))}\"," +
                $"\"{EscapeCsv(TruncateValue(log.NewValue, 100))}\"," +
                $"\"{log.IpAddress ?? ""}\"");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    #endregion

    #region Excel Export (HTML-based)

    /// <summary>
    /// Export reports list to Excel-compatible HTML format.
    /// </summary>
    public async Task<byte[]> ExportReportsToExcelAsync(ReportExportFilter? filter = null)
    {
        var query = _context.Reports
            .Include(r => r.ReportTemplate)
            .Include(r => r.ReportPeriod)
            .Include(r => r.SubmittedBy)
            .ThenInclude(u => u.OrganizationalUnit)
            .Include(r => r.AssignedReviewer)
            .AsQueryable();

        if (filter != null)
        {
            if (filter.TemplateId.HasValue)
                query = query.Where(r => r.ReportTemplateId == filter.TemplateId.Value);
            if (filter.PeriodId.HasValue)
                query = query.Where(r => r.ReportPeriodId == filter.PeriodId.Value);
            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(r => r.Status == filter.Status);
        }

        var reports = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\">");
        sb.AppendLine("<head><meta charset=\"UTF-8\"><style>");
        sb.AppendLine("table { border-collapse: collapse; } th, td { border: 1px solid #000; padding: 8px; }");
        sb.AppendLine("th { background-color: #4472C4; color: white; font-weight: bold; }");
        sb.AppendLine(".status-approved { background-color: #C6EFCE; } .status-rejected { background-color: #FFC7CE; }");
        sb.AppendLine(".status-submitted { background-color: #BDD7EE; } .status-draft { background-color: #EDEDED; }");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>ID</th><th>Template</th><th>Period</th><th>Status</th><th>Submitted By</th><th>Organization</th><th>Created</th><th>Submitted</th><th>Reviewed</th></tr>");

        foreach (var r in reports)
        {
            var statusClass = r.Status switch
            {
                ReportStatus.Approved => "status-approved",
                ReportStatus.Rejected => "status-rejected",
                ReportStatus.Submitted => "status-submitted",
                _ => "status-draft"
            };

            sb.AppendLine($"<tr class=\"{statusClass}\">" +
                $"<td>{r.Id}</td>" +
                $"<td>{HtmlEncode(r.ReportTemplate.Name)}</td>" +
                $"<td>{HtmlEncode(r.ReportPeriod.Name)}</td>" +
                $"<td>{ReportStatus.DisplayName(r.Status)}</td>" +
                $"<td>{HtmlEncode(r.SubmittedBy?.Name ?? "N/A")}</td>" +
                $"<td>{HtmlEncode(r.SubmittedBy?.OrganizationalUnit?.Name ?? "N/A")}</td>" +
                $"<td>{r.CreatedAt:yyyy-MM-dd}</td>" +
                $"<td>{(r.SubmittedAt?.ToString("yyyy-MM-dd") ?? "")}</td>" +
                $"<td>{(r.ReviewedAt?.ToString("yyyy-MM-dd") ?? "")}</td></tr>");
        }

        sb.AppendLine("</table></body></html>");

        _logger.LogInformation("Exported {Count} reports to Excel", reports.Count);
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    /// <summary>
    /// Export a single report with all details to Excel-compatible HTML.
    /// </summary>
    public async Task<byte[]> ExportReportDetailToExcelAsync(int reportId)
    {
        var report = await _context.Reports
            .Include(r => r.ReportTemplate)
            .Include(r => r.ReportPeriod)
            .Include(r => r.SubmittedBy)
            .ThenInclude(u => u.OrganizationalUnit)
            .Include(r => r.AssignedReviewer)
            .Include(r => r.FieldValues)
            .ThenInclude(fv => fv.ReportField)
            .FirstOrDefaultAsync(r => r.Id == reportId);

        if (report == null)
            return Encoding.UTF8.GetBytes("Report not found");

        var sb = new StringBuilder();
        sb.AppendLine("<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\">");
        sb.AppendLine("<head><meta charset=\"UTF-8\"><style>");
        sb.AppendLine("table { border-collapse: collapse; margin-bottom: 20px; } th, td { border: 1px solid #000; padding: 8px; }");
        sb.AppendLine("th { background-color: #4472C4; color: white; } .header { background-color: #D9E1F2; font-weight: bold; }");
        sb.AppendLine("</style></head><body>");

        // Report Header
        sb.AppendLine("<h2>Report Details</h2>");
        sb.AppendLine("<table><tr><th colspan=\"2\">Report Information</th></tr>");
        sb.AppendLine($"<tr><td class=\"header\">Template</td><td>{HtmlEncode(report.ReportTemplate.Name)}</td></tr>");
        sb.AppendLine($"<tr><td class=\"header\">Period</td><td>{HtmlEncode(report.ReportPeriod.Name)}</td></tr>");
        sb.AppendLine($"<tr><td class=\"header\">Status</td><td>{ReportStatus.DisplayName(report.Status)}</td></tr>");
        sb.AppendLine($"<tr><td class=\"header\">Submitted By</td><td>{HtmlEncode(report.SubmittedBy?.Name ?? "N/A")}</td></tr>");
        sb.AppendLine($"<tr><td class=\"header\">Organization</td><td>{HtmlEncode(report.SubmittedBy?.OrganizationalUnit?.Name ?? "N/A")}</td></tr>");
        sb.AppendLine($"<tr><td class=\"header\">Created</td><td>{report.CreatedAt:yyyy-MM-dd HH:mm}</td></tr>");
        sb.AppendLine($"<tr><td class=\"header\">Submitted</td><td>{(report.SubmittedAt?.ToString("yyyy-MM-dd HH:mm") ?? "Not submitted")}</td></tr>");
        sb.AppendLine($"<tr><td class=\"header\">Reviewed</td><td>{(report.ReviewedAt?.ToString("yyyy-MM-dd HH:mm") ?? "")}</td></tr>");
        sb.AppendLine("</table>");

        // Field Values
        sb.AppendLine("<h3>Report Data</h3>");
        sb.AppendLine("<table><tr><th>Section</th><th>Field</th><th>Value</th></tr>");

        foreach (var fv in report.FieldValues.OrderBy(f => f.ReportField.Section).ThenBy(f => f.ReportField.OrderIndex))
        {
            sb.AppendLine($"<tr><td>{HtmlEncode(fv.ReportField.Section)}</td>" +
                $"<td>{HtmlEncode(fv.ReportField.Label)}</td>" +
                $"<td>{HtmlEncode(fv.Value ?? "")}</td></tr>");
        }

        sb.AppendLine("</table></body></html>");

        _logger.LogInformation("Exported report {ReportId} detail to Excel", reportId);
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    /// <summary>
    /// Export aggregation summary to Excel-compatible HTML.
    /// </summary>
    public async Task<byte[]> ExportAggregationSummaryToExcelAsync(int? periodId = null, int? templateId = null)
    {
        var query = _context.AggregatedValues
            .Include(av => av.AggregationRule)
            .ThenInclude(ar => ar.ReportField)
            .Include(av => av.OrganizationalUnit)
            .Include(av => av.ReportPeriod)
            .AsQueryable();

        if (periodId.HasValue)
            query = query.Where(av => av.ReportPeriodId == periodId.Value);
        if (templateId.HasValue)
            query = query.Where(av => av.AggregationRule.ReportField.ReportTemplateId == templateId.Value);

        var values = await query.OrderBy(av => av.OrganizationalUnit.Name).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("<html xmlns:o=\"urn:schemas-microsoft-com:office:office\"><head><meta charset=\"UTF-8\"><style>");
        sb.AppendLine("table { border-collapse: collapse; } th, td { border: 1px solid #000; padding: 8px; }");
        sb.AppendLine("th { background-color: #4472C4; color: white; }");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine("<h2>Aggregation Summary</h2>");
        sb.AppendLine("<table><tr><th>Org Unit</th><th>Period</th><th>Field</th><th>Value</th><th>Status</th><th>Source Count</th></tr>");

        foreach (var av in values)
        {
            sb.AppendLine($"<tr><td>{HtmlEncode(av.OrganizationalUnit?.Name ?? "N/A")}</td>" +
                $"<td>{HtmlEncode(av.ReportPeriod?.Name ?? "N/A")}</td>" +
                $"<td>{HtmlEncode(av.AggregationRule?.ReportField?.Label ?? "N/A")}</td>" +
                $"<td>{av.DisplayValue}</td>" +
                $"<td>{av.Status}</td>" +
                $"<td>{av.SourceReportCount}</td></tr>");
        }

        sb.AppendLine("</table></body></html>");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    #endregion

    #region Print-Friendly HTML (for PDF via browser print)

    /// <summary>
    /// Generate print-friendly HTML for a report (for browser PDF export).
    /// </summary>
    public async Task<string> GeneratePrintableReportHtmlAsync(int reportId)
    {
        var report = await _context.Reports
            .Include(r => r.ReportTemplate)
            .Include(r => r.ReportPeriod)
            .Include(r => r.SubmittedBy)
            .ThenInclude(u => u.OrganizationalUnit)
            .Include(r => r.AssignedReviewer)
            .Include(r => r.FieldValues)
            .ThenInclude(fv => fv.ReportField)
            .FirstOrDefaultAsync(r => r.Id == reportId);

        if (report == null)
            return "<h1>Report not found</h1>";

        // Get upward flow items
        var suggestedActions = await _context.SuggestedActions
            .Where(a => a.ReportId == reportId)
            .ToListAsync();

        var resourceRequests = await _context.ResourceRequests
            .Where(r => r.ReportId == reportId)
            .ToListAsync();

        var supportRequests = await _context.SupportRequests
            .Where(s => s.ReportId == reportId)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"UTF-8\">");
        sb.AppendLine("<title>Report - " + HtmlEncode(report.ReportTemplate.Name) + "</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("@media print { body { -webkit-print-color-adjust: exact; print-color-adjust: exact; } }");
        sb.AppendLine("body { font-family: Arial, sans-serif; margin: 40px; line-height: 1.6; }");
        sb.AppendLine("h1 { color: #333; border-bottom: 2px solid #4472C4; padding-bottom: 10px; }");
        sb.AppendLine("h2 { color: #4472C4; margin-top: 30px; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin: 20px 0; }");
        sb.AppendLine("th, td { border: 1px solid #ddd; padding: 12px; text-align: left; }");
        sb.AppendLine("th { background-color: #4472C4; color: white; }");
        sb.AppendLine(".info-table td:first-child { width: 200px; font-weight: bold; background-color: #f5f5f5; }");
        sb.AppendLine(".status { padding: 4px 12px; border-radius: 4px; font-weight: bold; }");
        sb.AppendLine(".status-approved { background-color: #d4edda; color: #155724; }");
        sb.AppendLine(".status-rejected { background-color: #f8d7da; color: #721c24; }");
        sb.AppendLine(".status-submitted { background-color: #cce5ff; color: #004085; }");
        sb.AppendLine(".footer { margin-top: 40px; padding-top: 20px; border-top: 1px solid #ddd; font-size: 12px; color: #666; }");
        sb.AppendLine("</style></head><body>");

        // Header
        sb.AppendLine($"<h1>{HtmlEncode(report.ReportTemplate.Name)}</h1>");

        // Report Info
        sb.AppendLine("<table class=\"info-table\">");
        sb.AppendLine($"<tr><td>Period</td><td>{HtmlEncode(report.ReportPeriod.Name)}</td></tr>");
        sb.AppendLine($"<tr><td>Status</td><td><span class=\"status status-{report.Status}\">{ReportStatus.DisplayName(report.Status)}</span></td></tr>");
        sb.AppendLine($"<tr><td>Submitted By</td><td>{HtmlEncode(report.SubmittedBy?.Name ?? "N/A")}</td></tr>");
        sb.AppendLine($"<tr><td>Organization</td><td>{HtmlEncode(report.SubmittedBy?.OrganizationalUnit?.Name ?? "N/A")}</td></tr>");
        sb.AppendLine($"<tr><td>Created</td><td>{report.CreatedAt:MMMM dd, yyyy HH:mm}</td></tr>");
        sb.AppendLine($"<tr><td>Submitted</td><td>{(report.SubmittedAt?.ToString("MMMM dd, yyyy HH:mm") ?? "Not submitted")}</td></tr>");
        if (report.ReviewedAt.HasValue)
        {
            sb.AppendLine($"<tr><td>Reviewed</td><td>{report.ReviewedAt.Value:MMMM dd, yyyy HH:mm}</td></tr>");
            sb.AppendLine($"<tr><td>Reviewer</td><td>{HtmlEncode(report.AssignedReviewer?.Name ?? "N/A")}</td></tr>");
        }
        sb.AppendLine("</table>");

        // Field Values by Section
        var sections = report.FieldValues
            .GroupBy(fv => fv.ReportField.Section)
            .OrderBy(g => g.First().ReportField.OrderIndex);

        foreach (var section in sections)
        {
            sb.AppendLine($"<h2>{HtmlEncode(section.Key)}</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Field</th><th>Value</th></tr>");
            foreach (var fv in section.OrderBy(f => f.ReportField.OrderIndex))
            {
                sb.AppendLine($"<tr><td>{HtmlEncode(fv.ReportField.Label)}</td><td>{HtmlEncode(fv.Value ?? "-")}</td></tr>");
            }
            sb.AppendLine("</table>");
        }

        // Upward Flow Items
        if (suggestedActions.Any())
        {
            sb.AppendLine("<h2>Suggested Actions</h2>");
            sb.AppendLine("<table><tr><th>Title</th><th>Category</th><th>Priority</th><th>Status</th></tr>");
            foreach (var a in suggestedActions)
            {
                sb.AppendLine($"<tr><td>{HtmlEncode(a.Title)}</td><td>{ActionCategory.DisplayName(a.Category)}</td>" +
                    $"<td>{ActionPriority.DisplayName(a.Priority)}</td><td>{ActionStatus.DisplayName(a.Status)}</td></tr>");
            }
            sb.AppendLine("</table>");
        }

        if (resourceRequests.Any())
        {
            sb.AppendLine("<h2>Resource Requests</h2>");
            sb.AppendLine("<table><tr><th>Title</th><th>Category</th><th>Est. Cost</th><th>Status</th></tr>");
            foreach (var r in resourceRequests)
            {
                sb.AppendLine($"<tr><td>{HtmlEncode(r.Title)}</td><td>{ResourceCategory.DisplayName(r.Category)}</td>" +
                    $"<td>{r.EstimatedCost?.ToString("N2") ?? "N/A"} {r.Currency ?? "EGP"}</td><td>{ResourceStatus.DisplayName(r.Status)}</td></tr>");
            }
            sb.AppendLine("</table>");
        }

        if (supportRequests.Any())
        {
            sb.AppendLine("<h2>Support Requests</h2>");
            sb.AppendLine("<table><tr><th>Title</th><th>Category</th><th>Urgency</th><th>Status</th></tr>");
            foreach (var s in supportRequests)
            {
                sb.AppendLine($"<tr><td>{HtmlEncode(s.Title)}</td><td>{SupportCategory.DisplayName(s.Category)}</td>" +
                    $"<td>{SupportUrgency.DisplayName(s.Urgency)}</td><td>{SupportStatus.DisplayName(s.Status)}</td></tr>");
            }
            sb.AppendLine("</table>");
        }

        // Footer
        sb.AppendLine($"<div class=\"footer\">Generated on {DateTime.Now:MMMM dd, yyyy HH:mm} | Reporting System</div>");
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    #endregion

    #region Helper Methods

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        return value.Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", "");
    }

    private static string HtmlEncode(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        return System.Net.WebUtility.HtmlEncode(value);
    }

    private static string TruncateValue(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
    }

    #endregion
}

#region Export Filter Classes

public class ReportExportFilter
{
    public int? TemplateId { get; set; }
    public int? PeriodId { get; set; }
    public string? Status { get; set; }
    public int? SubmittedById { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class AuditLogExportFilter
{
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public int? UserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

#endregion
