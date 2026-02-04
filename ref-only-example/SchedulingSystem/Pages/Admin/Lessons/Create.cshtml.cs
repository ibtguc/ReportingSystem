// DISABLED: Lessons are managed through data import
// This page is commented out to prevent manual data entry that could conflict with imported data.
// Use /Admin/Import/Untis to import Lesson data from UNTIS export files.

/*
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Lessons
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Lesson Lesson { get; set; } = default!;

        public SelectList SubjectList { get; set; } = default!;
        public SelectList ClassList { get; set; } = default!;
        public SelectList TeacherList { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadDropdownsAsync();

            // Set defaults
            Lesson = new Lesson
            {
                Duration = 45,
                FrequencyPerWeek = 1,
                IsActive = true
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return Page();
            }

            // Check if this lesson combination already exists
            var existingLesson = await _context.Lessons
                .FirstOrDefaultAsync(l =>
                    l.SubjectId == Lesson.SubjectId &&
                    l.ClassId == Lesson.ClassId &&
                    l.TeacherId == Lesson.TeacherId);

            if (existingLesson != null)
            {
                ModelState.AddModelError(string.Empty,
                    "A lesson with this Subject, Class, and Teacher combination already exists.");
                await LoadDropdownsAsync();
                return Page();
            }

            _context.Lessons.Add(Lesson);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        // API endpoint to get qualified teachers for a subject
        public async Task<IActionResult> OnGetQualifiedTeachersAsync(int subjectId)
        {
            var qualifiedTeachers = await _context.TeacherSubjects
                .Where(ts => ts.SubjectId == subjectId)
                .Include(ts => ts.Teacher)
                .Select(ts => new
                {
                    id = ts.TeacherId,
                    name = ts.Teacher!.FullName,
                    qualification = ts.QualificationLevel,
                    isPreferred = ts.IsPreferred
                })
                .OrderBy(t => t.name)
                .ToListAsync();

            return new JsonResult(qualifiedTeachers);
        }

        private async Task LoadDropdownsAsync()
        {
            SubjectList = new SelectList(
                await _context.Subjects.OrderBy(s => s.Name).ToListAsync(),
                "Id", "Name");

            ClassList = new SelectList(
                await _context.Classes.OrderBy(c => c.Name).ToListAsync(),
                "Id", "Name");

            // Load all teachers initially, will be filtered by JavaScript
            var teachers = await _context.Teachers
                .OrderBy(t => t.LastName)
                .ThenBy(t => t.FirstName)
                .ToListAsync();

            TeacherList = new SelectList(teachers, "Id", "FullName");
        }
    }
}
*/
