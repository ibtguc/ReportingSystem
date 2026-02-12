using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Knowledge;

public class IndexModel : PageModel
{
    private readonly KnowledgeBaseService _knowledgeService;

    public IndexModel(KnowledgeBaseService knowledgeService)
    {
        _knowledgeService = knowledgeService;
    }

    public List<KnowledgeCategory> Categories { get; set; } = new();
    public List<KnowledgeArticle> Articles { get; set; } = new();
    public KnowledgeBaseStats Stats { get; set; } = new();
    public int TotalArticles { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Page { get; set; } = 1;

    public async Task OnGetAsync()
    {
        const int pageSize = 12;
        CurrentPage = Page < 1 ? 1 : Page;

        Categories = await _knowledgeService.GetTopLevelCategoriesAsync();
        Stats = await _knowledgeService.GetStatsAsync();
        TotalArticles = await _knowledgeService.GetArticleCountAsync(CategoryId, Search);
        TotalPages = (int)Math.Ceiling(TotalArticles / (double)pageSize);
        Articles = await _knowledgeService.GetArticlesAsync(CategoryId, Search, CurrentPage, pageSize);
    }
}
