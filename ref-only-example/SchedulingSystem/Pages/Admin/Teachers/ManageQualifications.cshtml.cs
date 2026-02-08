using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Teachers
{
    public class ManageQualificationsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ManageQualificationsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Teacher Teacher { get; set; } = default!;
        public List<TeacherSubject> Qualifications { get; set; } = new();
        public SelectList SubjectList { get; set; } = default!;
        public SelectList QualificationLevelList { get; set; } = default!;

        [BindProperty]
        public TeacherSubject NewQualification { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacher = await _context.Teachers
                .Include(t => t.TeacherSubjects)
                    .ThenInclude(ts => ts.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (teacher == null)
            {
                return NotFound();
            }

            Teacher = teacher;
            Qualifications = teacher.TeacherSubjects.OrderBy(ts => ts.Subject?.Name).ToList();

            await LoadSelectListsAsync(id.Value);

            return Page();
        }

        public async Task<IActionResult> OnPostAddAsync(int id)
        {
            if (!ModelState.IsValid)
            {
                return await OnGetAsync(id);
            }

            // Check if qualification already exists
            var exists = await _context.TeacherSubjects
                .AnyAsync(ts => ts.TeacherId == id && ts.SubjectId == NewQualification.SubjectId);

            if (exists)
            {
                ModelState.AddModelError("", "This teacher already has a qualification for this subject.");
                return await OnGetAsync(id);
            }

            NewQualification.TeacherId = id;
            _context.TeacherSubjects.Add(NewQualification);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, int qualificationId)
        {
            var qualification = await _context.TeacherSubjects.FindAsync(qualificationId);

            if (qualification != null)
            {
                _context.TeacherSubjects.Remove(qualification);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostTogglePreferredAsync(int id, int qualificationId)
        {
            var qualification = await _context.TeacherSubjects.FindAsync(qualificationId);

            if (qualification != null)
            {
                qualification.IsPreferred = !qualification.IsPreferred;
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { id });
        }

        private async Task LoadSelectListsAsync(int teacherId)
        {
            // Get subjects that teacher doesn't have yet
            var existingSubjectIds = await _context.TeacherSubjects
                .Where(ts => ts.TeacherId == teacherId)
                .Select(ts => ts.SubjectId)
                .ToListAsync();

            var availableSubjects = await _context.Subjects
                .Where(s => !existingSubjectIds.Contains(s.Id))
                .OrderBy(s => s.Name)
                .ToListAsync();

            SubjectList = new SelectList(availableSubjects, "Id", "Name");

            QualificationLevelList = new SelectList(new[]
            {
                new { Value = "Expert", Text = "Expert" },
                new { Value = "Qualified", Text = "Qualified" },
                new { Value = "Basic", Text = "Basic" }
            }, "Value", "Text");
        }
    }
}
