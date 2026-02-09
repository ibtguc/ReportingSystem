using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Admin.Knowledge;

public class EditCategoryModel : PageModel
{
    private readonly KnowledgeBaseService _knowledgeService;

    public EditCategoryModel(KnowledgeBaseService knowledgeService)
    {
        _knowledgeService = knowledgeService;
    }

    [BindProperty]
    public KnowledgeCategory Category { get; set; } = null!;

    public List<SelectListItem> ParentCategories { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var category = await _knowledgeService.GetCategoryByIdAsync(id);
        if (category == null) return NotFound();

        Category = category;
        await LoadParentCategoriesAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove("Category.ParentCategory");
        ModelState.Remove("Category.SubCategories");
        ModelState.Remove("Category.Articles");

        if (!ModelState.IsValid)
        {
            await LoadParentCategoriesAsync(Category.Id);
            return Page();
        }

        var result = await _knowledgeService.UpdateCategoryAsync(Category);
        if (result == null) return NotFound();

        TempData["SuccessMessage"] = $"Category '{Category.Name}' updated successfully.";
        return RedirectToPage("/Admin/Knowledge/Index");
    }

    private async Task LoadParentCategoriesAsync(int excludeId)
    {
        var categories = await _knowledgeService.GetCategoriesAsync();
        ParentCategories = categories
            .Where(c => c.ParentCategoryId == null && c.Id != excludeId)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            .ToList();
    }
}
