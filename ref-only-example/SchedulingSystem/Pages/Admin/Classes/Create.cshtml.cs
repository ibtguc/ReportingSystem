using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Classes;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Class Class { get; set; } = new();

    public SelectList ParentClassList { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadParentClassListAsync();
        Class.IsActive = true;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadParentClassListAsync();
            return Page();
        }

        _context.Classes.Add(Class);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }

    private async Task LoadParentClassListAsync()
    {
        var classes = await _context.Classes
            .OrderBy(c => c.Name)
            .ToListAsync();

        ParentClassList = new SelectList(classes, "Id", "Name");
    }
}
