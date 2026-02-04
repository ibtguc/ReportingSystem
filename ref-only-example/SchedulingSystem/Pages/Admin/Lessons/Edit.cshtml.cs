using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Lessons;

public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public LessonEditModel LessonInput { get; set; } = new();

    [BindProperty]
    public string? ReturnUrl { get; set; }

    public bool IsNewLesson => LessonInput.Id == 0;

    // Data for dropdowns
    public List<Teacher> AllTeachers { get; set; } = new();
    public List<Subject> AllSubjects { get; set; } = new();
    public List<Class> AllClasses { get; set; } = new();

    // Selected IDs for the lesson
    public List<int> SelectedTeacherIds { get; set; } = new();
    public List<int> SelectedSubjectIds { get; set; } = new();
    public List<int> SelectedClassIds { get; set; } = new();

    // LessonAssignments for specific teacher-subject-class combinations
    public List<LessonAssignment> LessonAssignments { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id, string? returnUrl, int? classId, int? subjectId)
    {
        ReturnUrl = returnUrl ?? Url.Page("./Dashboard");

        await LoadDropdownData();

        if (id.HasValue && id.Value > 0)
        {
            // Editing existing lesson
            var lesson = await _context.Lessons
                .Include(l => l.LessonTeachers)
                .Include(l => l.LessonSubjects)
                .Include(l => l.LessonClasses)
                .Include(l => l.LessonAssignments)
                    .ThenInclude(la => la.Teacher)
                .Include(l => l.LessonAssignments)
                    .ThenInclude(la => la.Subject)
                .Include(l => l.LessonAssignments)
                    .ThenInclude(la => la.Class)
                .FirstOrDefaultAsync(l => l.Id == id.Value);

            if (lesson == null)
            {
                return NotFound();
            }

            LessonInput = new LessonEditModel
            {
                Id = lesson.Id,
                Description = lesson.Description,
                Duration = lesson.Duration,
                FrequencyPerWeek = lesson.FrequencyPerWeek,
                ClassPeriodsPerWeek = lesson.ClassPeriodsPerWeek,
                TeacherPeriodsPerWeek = lesson.TeacherPeriodsPerWeek,
                SpecialRequirements = lesson.SpecialRequirements,
                RequiredRoomType = lesson.RequiredRoomType,
                IsActive = lesson.IsActive
            };

            SelectedTeacherIds = lesson.LessonTeachers.OrderBy(lt => lt.Order).Select(lt => lt.TeacherId).ToList();
            SelectedSubjectIds = lesson.LessonSubjects.OrderBy(ls => ls.Order).Select(ls => ls.SubjectId).ToList();
            SelectedClassIds = lesson.LessonClasses.OrderBy(lc => lc.Order).Select(lc => lc.ClassId).ToList();
            LessonAssignments = lesson.LessonAssignments.OrderBy(la => la.Order).ToList();
        }
        else
        {
            // Creating new lesson
            LessonInput = new LessonEditModel
            {
                Duration = 1,
                FrequencyPerWeek = 1,
                IsActive = true
            };

            // Pre-populate from matrix selection
            if (classId.HasValue)
            {
                SelectedClassIds.Add(classId.Value);
            }
            if (subjectId.HasValue)
            {
                SelectedSubjectIds.Add(subjectId.Value);
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadDropdownData();

        // Parse selected IDs from form
        SelectedTeacherIds = ParseSelectedIds("SelectedTeacherIds");
        SelectedSubjectIds = ParseSelectedIds("SelectedSubjectIds");
        SelectedClassIds = ParseSelectedIds("SelectedClassIds");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        Lesson lesson;

        // Parse LessonAssignments from form
        var assignmentData = ParseLessonAssignments();

        if (LessonInput.Id == 0)
        {
            // Create new lesson
            lesson = new Lesson
            {
                Description = LessonInput.Description,
                Duration = LessonInput.Duration,
                FrequencyPerWeek = LessonInput.FrequencyPerWeek,
                ClassPeriodsPerWeek = LessonInput.ClassPeriodsPerWeek,
                TeacherPeriodsPerWeek = LessonInput.TeacherPeriodsPerWeek,
                SpecialRequirements = LessonInput.SpecialRequirements,
                RequiredRoomType = LessonInput.RequiredRoomType,
                IsActive = LessonInput.IsActive
            };

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();
        }
        else
        {
            // Update existing lesson
            lesson = await _context.Lessons
                .Include(l => l.LessonTeachers)
                .Include(l => l.LessonSubjects)
                .Include(l => l.LessonClasses)
                .Include(l => l.LessonAssignments)
                .FirstOrDefaultAsync(l => l.Id == LessonInput.Id);

            if (lesson == null)
            {
                return NotFound();
            }

            lesson.Description = LessonInput.Description;
            lesson.Duration = LessonInput.Duration;
            lesson.FrequencyPerWeek = LessonInput.FrequencyPerWeek;
            lesson.ClassPeriodsPerWeek = LessonInput.ClassPeriodsPerWeek;
            lesson.TeacherPeriodsPerWeek = LessonInput.TeacherPeriodsPerWeek;
            lesson.SpecialRequirements = LessonInput.SpecialRequirements;
            lesson.RequiredRoomType = LessonInput.RequiredRoomType;
            lesson.IsActive = LessonInput.IsActive;

            // Clear existing relationships
            _context.LessonTeachers.RemoveRange(lesson.LessonTeachers);
            _context.LessonSubjects.RemoveRange(lesson.LessonSubjects);
            _context.LessonClasses.RemoveRange(lesson.LessonClasses);
            _context.LessonAssignments.RemoveRange(lesson.LessonAssignments);
        }

        // Add teachers
        int order = 0;
        foreach (var teacherId in SelectedTeacherIds)
        {
            _context.LessonTeachers.Add(new LessonTeacher
            {
                LessonId = lesson.Id,
                TeacherId = teacherId,
                IsLead = order == 0,
                Order = order++
            });
        }

        // Add subjects
        order = 0;
        foreach (var subjectId in SelectedSubjectIds)
        {
            _context.LessonSubjects.Add(new LessonSubject
            {
                LessonId = lesson.Id,
                SubjectId = subjectId,
                IsPrimary = order == 0,
                Order = order++
            });
        }

        // Add classes
        order = 0;
        foreach (var classId in SelectedClassIds)
        {
            _context.LessonClasses.Add(new LessonClass
            {
                LessonId = lesson.Id,
                ClassId = classId,
                IsPrimary = order == 0,
                Order = order++
            });
        }

        // Add LessonAssignments (teacher-subject-class combinations)
        order = 0;
        foreach (var assignment in assignmentData)
        {
            _context.LessonAssignments.Add(new LessonAssignment
            {
                LessonId = lesson.Id,
                TeacherId = assignment.TeacherId,
                SubjectId = assignment.SubjectId,
                ClassId = assignment.ClassId,
                Notes = assignment.Notes,
                Order = order++
            });
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = LessonInput.Id == 0
            ? "Lesson created successfully!"
            : "Lesson updated successfully!";

        return Redirect(ReturnUrl ?? Url.Page("./Dashboard")!);
    }

    public IActionResult OnPostCancel()
    {
        return Redirect(ReturnUrl ?? Url.Page("./Dashboard")!);
    }

    private async Task LoadDropdownData()
    {
        AllTeachers = await _context.Teachers
            .Where(t => t.IsActive)
            .OrderBy(t => t.FirstName)
            .ThenBy(t => t.LastName)
            .ToListAsync();

        AllSubjects = await _context.Subjects
            .OrderBy(s => s.Name)
            .ToListAsync();

        AllClasses = await _context.Classes
            .OrderBy(c => c.YearLevel)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    private List<int> ParseSelectedIds(string fieldName)
    {
        var result = new List<int>();
        var values = Request.Form[fieldName];
        foreach (var value in values)
        {
            if (int.TryParse(value, out int id))
            {
                result.Add(id);
            }
        }
        return result;
    }

    private List<LessonAssignmentInput> ParseLessonAssignments()
    {
        var result = new List<LessonAssignmentInput>();

        var teacherIds = Request.Form["Assignment_TeacherId"];
        var subjectIds = Request.Form["Assignment_SubjectId"];
        var classIds = Request.Form["Assignment_ClassId"];
        var notes = Request.Form["Assignment_Notes"];

        // All arrays should have the same length
        var count = teacherIds.Count;

        for (int i = 0; i < count; i++)
        {
            int? teacherId = null;
            int? subjectId = null;
            int? classId = null;

            if (int.TryParse(teacherIds[i], out int tid) && tid > 0)
                teacherId = tid;
            if (int.TryParse(subjectIds[i], out int sid) && sid > 0)
                subjectId = sid;
            if (int.TryParse(classIds[i], out int cid) && cid > 0)
                classId = cid;

            // Only add if at least one value is set
            if (teacherId.HasValue || subjectId.HasValue || classId.HasValue)
            {
                result.Add(new LessonAssignmentInput
                {
                    TeacherId = teacherId,
                    SubjectId = subjectId,
                    ClassId = classId,
                    Notes = i < notes.Count ? notes[i] : null
                });
            }
        }

        return result;
    }

    public class LessonAssignmentInput
    {
        public int? TeacherId { get; set; }
        public int? SubjectId { get; set; }
        public int? ClassId { get; set; }
        public string? Notes { get; set; }
    }

    public class LessonEditModel
    {
        public int Id { get; set; }
        public string? Description { get; set; }
        public int Duration { get; set; } = 1;
        public int FrequencyPerWeek { get; set; } = 1;
        public int? ClassPeriodsPerWeek { get; set; }
        public int? TeacherPeriodsPerWeek { get; set; }
        public string? SpecialRequirements { get; set; }
        public string? RequiredRoomType { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
