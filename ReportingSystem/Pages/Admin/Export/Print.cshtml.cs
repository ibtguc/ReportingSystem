using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Admin.Export;

public class PrintModel : PageModel
{
    private readonly ExportService _exportService;

    public PrintModel(ExportService exportService)
    {
        _exportService = exportService;
    }

    public string PrintableHtml { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(int reportId)
    {
        PrintableHtml = await _exportService.GeneratePrintableReportHtmlAsync(reportId);
        return Page();
    }
}
