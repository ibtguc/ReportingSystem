using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Subjects;

public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DeleteModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Subject Subject { get; set; } = new();

    public int LessonCount { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var subject = await _context.Subjects
            .Include(s => s.Department)
            .Include(s => s.LessonSubjects)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (subject == null)
        {
            return NotFound();
        }

        Subject = subject;
        LessonCount = subject.LessonSubjects.Count;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var subject = await _context.Subjects
            .Include(s => s.LessonSubjects)
            .Include(s => s.TeacherSubjects)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (subject != null)
        {
            // Remove related entities first
            _context.LessonSubjects.RemoveRange(subject.LessonSubjects);
            _context.TeacherSubjects.RemoveRange(subject.TeacherSubjects);

            // Remove availability constraints if they exist
            var availabilities = await _context.SubjectAvailabilities
                .Where(sa => sa.SubjectId == id)
                .ToListAsync();
            _context.SubjectAvailabilities.RemoveRange(availabilities);

            // Remove the subject
            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Subject '{subject.Name}' deleted successfully!";
        }

        return RedirectToPage("./Index");
    }
}
