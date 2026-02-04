using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Classes;

public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Class Class { get; set; } = new();

    public SelectList ParentClassList { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var cls = await _context.Classes.FindAsync(id);

        if (cls == null)
        {
            return NotFound();
        }

        Class = cls;
        await LoadParentClassListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadParentClassListAsync();
            return Page();
        }

        // Prevent setting self as parent
        if (Class.ParentClassId == Class.Id)
        {
            ModelState.AddModelError("Class.ParentClassId", "A class cannot be its own parent.");
            await LoadParentClassListAsync();
            return Page();
        }

        _context.Attach(Class).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ClassExists(Class.Id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return RedirectToPage("./Index");
    }

    private bool ClassExists(int id)
    {
        return _context.Classes.Any(e => e.Id == id);
    }

    private async Task LoadParentClassListAsync()
    {
        var classes = await _context.Classes
            .Where(c => c.Id != Class.Id)
            .OrderBy(c => c.Name)
            .ToListAsync();

        ParentClassList = new SelectList(classes, "Id", "Name");
    }
}
