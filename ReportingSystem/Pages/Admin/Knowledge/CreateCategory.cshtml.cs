using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Admin.Knowledge;

public class CreateCategoryModel : PageModel
{
    private readonly KnowledgeBaseService _knowledgeService;

    public CreateCategoryModel(KnowledgeBaseService knowledgeService)
    {
        _knowledgeService = knowledgeService;
    }

    [BindProperty]
    public KnowledgeCategory Category { get; set; } = new();

    public List<SelectListItem> ParentCategories { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadParentCategoriesAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove("Category.ParentCategory");
        ModelState.Remove("Category.SubCategories");
        ModelState.Remove("Category.Articles");

        if (!ModelState.IsValid)
        {
            await LoadParentCategoriesAsync();
            return Page();
        }

        await _knowledgeService.CreateCategoryAsync(Category);
        TempData["SuccessMessage"] = $"Category '{Category.Name}' created successfully.";
        return RedirectToPage("/Admin/Knowledge/Index");
    }

    private async Task LoadParentCategoriesAsync()
    {
        var categories = await _knowledgeService.GetCategoriesAsync();
        ParentCategories = categories
            .Where(c => c.ParentCategoryId == null)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            .ToList();
    }
}
