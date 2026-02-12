using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Knowledge;

public class ArticleModel : PageModel
{
    private readonly KnowledgeBaseService _knowledgeService;

    public ArticleModel(KnowledgeBaseService knowledgeService)
    {
        _knowledgeService = knowledgeService;
    }

    public KnowledgeArticle Article { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var article = await _knowledgeService.GetArticleByIdAsync(id);
        if (article == null)
            return NotFound();

        Article = article;
        await _knowledgeService.IncrementViewCountAsync(id);
        return Page();
    }
}
