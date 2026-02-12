using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Admin.Templates;

[Authorize]
public class CreateModel : PageModel
{
    private readonly ReportTemplateService _templateService;
    private readonly ApplicationDbContext _context;

    public CreateModel(ReportTemplateService templateService, ApplicationDbContext context)
    {
        _templateService = templateService;
        _context = context;
    }

    [BindProperty]
    public ReportTemplate Template { get; set; } = new();

    public List<SelectListItem> CommitteeOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadCommitteeOptions();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove("Template.CreatedBy");
        ModelState.Remove("Template.Committee");

        if (!ModelState.IsValid)
        {
            await LoadCommitteeOptions();
            return Page();
        }

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        await _templateService.CreateTemplateAsync(Template, userId);

        TempData["SuccessMessage"] = $"Template \"{Template.Name}\" created successfully.";
        return RedirectToPage("Index");
    }

    private async Task LoadCommitteeOptions()
    {
        var committees = await _context.Committees
            .Where(c => c.IsActive)
            .OrderBy(c => c.HierarchyLevel)
            .ThenBy(c => c.Name)
            .Select(c => new SelectListItem($"[{c.HierarchyLevel}] {c.Name}", c.Id.ToString()))
            .ToListAsync();
        CommitteeOptions = committees;
    }
}
