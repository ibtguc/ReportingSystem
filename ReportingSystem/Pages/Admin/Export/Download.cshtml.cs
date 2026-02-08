using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Admin.Export;

public class DownloadModel : PageModel
{
    private readonly ExportService _exportService;
    private readonly ILogger<DownloadModel> _logger;

    public DownloadModel(ExportService exportService, ILogger<DownloadModel> logger)
    {
        _exportService = exportService;
        _logger = logger;
    }

    /// <summary>
    /// Export reports list to CSV or Excel.
    /// GET /Admin/Export/Download?type=reports&format=csv&templateId=1&periodId=2&status=approved
    /// </summary>
    public async Task<IActionResult> OnGetAsync(
        string type,
        string format = "csv",
        int? templateId = null,
        int? periodId = null,
        string? status = null,
        int? reportId = null,
        int? userId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? action = null,
        string? entityType = null)
    {
        try
        {
            byte[] fileContent;
            string fileName;
            string contentType;

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            switch (type?.ToLower())
            {
                case "reports":
                    var reportFilter = new ReportExportFilter
                    {
                        TemplateId = templateId,
                        PeriodId = periodId,
                        Status = status,
                        SubmittedById = userId,
                        FromDate = fromDate,
                        ToDate = toDate
                    };

                    if (format == "excel")
                    {
                        fileContent = await _exportService.ExportReportsToExcelAsync(reportFilter);
                        fileName = $"Reports_{timestamp}.xls";
                        contentType = "application/vnd.ms-excel";
                    }
                    else
                    {
                        fileContent = await _exportService.ExportReportsToCsvAsync(reportFilter);
                        fileName = $"Reports_{timestamp}.csv";
                        contentType = "text/csv";
                    }
                    break;

                case "report-detail":
                    if (!reportId.HasValue)
                        return BadRequest("Report ID is required");

                    if (format == "excel")
                    {
                        fileContent = await _exportService.ExportReportDetailToExcelAsync(reportId.Value);
                        fileName = $"Report_{reportId}_{timestamp}.xls";
                        contentType = "application/vnd.ms-excel";
                    }
                    else
                    {
                        fileContent = await _exportService.ExportReportDetailToCsvAsync(reportId.Value);
                        fileName = $"Report_{reportId}_{timestamp}.csv";
                        contentType = "text/csv";
                    }
                    break;

                case "suggested-actions":
                    fileContent = await _exportService.ExportSuggestedActionsToCsvAsync(reportId);
                    fileName = $"SuggestedActions_{timestamp}.csv";
                    contentType = "text/csv";
                    break;

                case "resource-requests":
                    fileContent = await _exportService.ExportResourceRequestsToCsvAsync(reportId);
                    fileName = $"ResourceRequests_{timestamp}.csv";
                    contentType = "text/csv";
                    break;

                case "audit-log":
                    var auditFilter = new AuditLogExportFilter
                    {
                        Action = action,
                        EntityType = entityType,
                        UserId = userId,
                        FromDate = fromDate,
                        ToDate = toDate
                    };
                    fileContent = await _exportService.ExportAuditLogToCsvAsync(auditFilter);
                    fileName = $"AuditLog_{timestamp}.csv";
                    contentType = "text/csv";
                    break;

                case "aggregation":
                    fileContent = await _exportService.ExportAggregationSummaryToExcelAsync(periodId, templateId);
                    fileName = $"AggregationSummary_{timestamp}.xls";
                    contentType = "application/vnd.ms-excel";
                    break;

                default:
                    return BadRequest("Invalid export type");
            }

            _logger.LogInformation("User exported {Type} data as {Format}", type, format);
            return File(fileContent, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export failed for type {Type}", type);
            return StatusCode(500, "Export failed");
        }
    }
}
