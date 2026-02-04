using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Classes;

public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DeleteModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Class Class { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var cls = await _context.Classes
            .Include(c => c.ParentClass)
            .Include(c => c.LessonClasses)
            .Include(c => c.Students)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (cls == null)
        {
            return NotFound();
        }

        Class = cls;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var cls = await _context.Classes.FindAsync(id);

        if (cls != null)
        {
            _context.Classes.Remove(cls);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
