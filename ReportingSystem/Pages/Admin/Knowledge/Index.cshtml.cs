using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Admin.Knowledge;

public class IndexModel : PageModel
{
    private readonly KnowledgeBaseService _knowledgeService;

    public IndexModel(KnowledgeBaseService knowledgeService)
    {
        _knowledgeService = knowledgeService;
    }

    public List<KnowledgeCategory> Categories { get; set; } = new();
    public KnowledgeBaseStats Stats { get; set; } = new();

    public async Task OnGetAsync()
    {
        Categories = await _knowledgeService.GetCategoriesAsync(includeInactive: true);
        Stats = await _knowledgeService.GetStatsAsync();
    }

    public async Task<IActionResult> OnPostBulkIndexAsync()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Forbid();

        var count = await _knowledgeService.BulkIndexAsync(userId);
        TempData["SuccessMessage"] = $"Bulk indexing complete: {count} new articles created.";
        return RedirectToPage();
    }
}
