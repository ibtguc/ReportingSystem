using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Lessons
{
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Overview Statistics
        public int TotalLessons { get; set; }
        public int TeamTaughtCount { get; set; }
        public int ParallelLessonsCount { get; set; }
        public int TotalExpectedLessons { get; set; }
        public int MissingLessonsCount { get; set; }
        public double CompletionPercentage { get; set; }

        // Grade-level statistics
        public List<GradeStats> GradeStatistics { get; set; } = new();

        // Matrix data
        public List<Subject> Subjects { get; set; } = new();
        public List<Class> MainClasses { get; set; } = new(); // Exclude subgroups for main matrix
        public Dictionary<int, Dictionary<int, LessonCell>> LessonMatrix { get; set; } = new(); // ClassId -> SubjectId -> LessonCell

        // Detailed list
        public List<Lesson> AllLessons { get; set; } = new();

        // Lesson to Timetables mapping
        public Dictionary<int, List<TimetableInfo>> LessonTimetables { get; set; } = new();

        // Data for edit modal dropdowns
        public List<Teacher> AllTeachers { get; set; } = new();
        public List<Subject> AllSubjects { get; set; } = new();
        public List<Class> AllClasses { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Load all data with includes
            var allLessons = await _context.Lessons
                .Include(l => l.LessonSubjects)
                    .ThenInclude(ls => ls.Subject)
                .Include(l => l.LessonClasses)
                    .ThenInclude(lc => lc.Class)
                .Include(l => l.LessonTeachers)
                    .ThenInclude(lt => lt.Teacher)
                .Include(l => l.LessonAssignments)
                    .ThenInclude(la => la.Teacher)
                .Include(l => l.LessonAssignments)
                    .ThenInclude(la => la.Subject)
                .Include(l => l.LessonAssignments)
                    .ThenInclude(la => la.Class)
                .AsSplitQuery()
                .ToListAsync();

            AllLessons = allLessons;

            // Load timetable info for each lesson
            var scheduledLessons = await _context.ScheduledLessons
                .Include(sl => sl.Timetable)
                .Where(sl => sl.TimetableId != null && sl.Timetable != null)
                .ToListAsync();

            // Group by lesson ID and map to timetables
            LessonTimetables = scheduledLessons
                .GroupBy(sl => sl.LessonId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(sl => sl.Timetable!)
                        .DistinctBy(t => t.Id)
                        .Select(t => new TimetableInfo
                        {
                            Id = t.Id,
                            Name = t.Name,
                            Status = t.Status
                        })
                        .OrderBy(t => t.Name)
                        .ToList()
                );

            var subjects = await _context.Subjects
                .OrderBy(s => s.Category)
                .ThenBy(s => s.Name)
                .ToListAsync();

            Subjects = subjects;

            var allClasses = await _context.Classes
                .OrderBy(c => c.YearLevel)
                .ThenBy(c => c.Name)
                .ToListAsync();

            AllClasses = allClasses;

            // Load all teachers for edit modal
            AllTeachers = await _context.Teachers
                .Where(t => t.IsActive)
                .OrderBy(t => t.FirstName)
                .ThenBy(t => t.LastName)
                .ToListAsync();

            // Store all subjects for edit modal
            AllSubjects = subjects;

            // Separate main classes from subgroups
            MainClasses = allClasses.Where(c => c.ParentClassId == null).ToList();
            var subgroups = allClasses.Where(c => c.ParentClassId != null).ToList();

            // Calculate overview statistics
            TotalLessons = allLessons.Count;
            TeamTaughtCount = allLessons.Count(l => l.LessonTeachers.Count() > 1);
            ParallelLessonsCount = allLessons.Count(l => !string.IsNullOrEmpty(l.SpecialRequirements)
                && l.SpecialRequirements.Contains("Parallel lesson"));

            // Expected lessons: each main class should have all subjects
            TotalExpectedLessons = MainClasses.Count * subjects.Count;
            var actualMainClassLessons = allLessons.Count(l =>
                MainClasses.Any(c => l.LessonClasses.Any(lc => lc.ClassId == c.Id)));
            MissingLessonsCount = TotalExpectedLessons - actualMainClassLessons;
            CompletionPercentage = TotalExpectedLessons > 0
                ? Math.Round((double)actualMainClassLessons / TotalExpectedLessons * 100, 1)
                : 0;

            // Calculate grade-level statistics
            var gradeGroups = MainClasses.GroupBy(c => c.YearLevel);
            foreach (var gradeGroup in gradeGroups)
            {
                var yearLevel = gradeGroup.Key;
                var classesInGrade = gradeGroup.ToList();
                var expectedForGrade = classesInGrade.Count * subjects.Count;
                var actualForGrade = allLessons.Count(l =>
                    classesInGrade.Any(c => l.LessonClasses.Any(lc => lc.ClassId == c.Id)));
                var missingForGrade = expectedForGrade - actualForGrade;

                GradeStatistics.Add(new GradeStats
                {
                    YearLevel = yearLevel,
                    GradeName = $"Grade {yearLevel}",
                    TotalClasses = classesInGrade.Count,
                    ExpectedLessons = expectedForGrade,
                    ActualLessons = actualForGrade,
                    MissingLessons = missingForGrade,
                    CompletionPercentage = expectedForGrade > 0
                        ? Math.Round((double)actualForGrade / expectedForGrade * 100, 1)
                        : 0
                });
            }

            // Build matrix: ClassId -> SubjectId -> LessonCell
            foreach (var classEntity in MainClasses)
            {
                LessonMatrix[classEntity.Id] = new Dictionary<int, LessonCell>();

                foreach (var subject in subjects)
                {
                    var lessonsForCell = allLessons
                        .Where(l => l.LessonClasses.Any(lc => lc.ClassId == classEntity.Id) &&
                                    l.LessonSubjects.Any(ls => ls.SubjectId == subject.Id))
                        .ToList();

                    // Check if there are subgroup lessons for this subject
                    var subgroupsForClass = subgroups.Where(sg => sg.ParentClassId == classEntity.Id).ToList();
                    var subgroupLessons = allLessons
                        .Where(l => subgroupsForClass.Any(sg => l.LessonClasses.Any(lc => lc.ClassId == sg.Id)) &&
                                    l.LessonSubjects.Any(ls => ls.SubjectId == subject.Id))
                        .ToList();

                    // Combine main class and subgroup lessons for total frequency calculation
                    var allLessonsForCell = lessonsForCell.Concat(subgroupLessons).Distinct().ToList();
                    var totalFrequency = allLessonsForCell.Sum(l => l.FrequencyPerWeek);

                    LessonMatrix[classEntity.Id][subject.Id] = new LessonCell
                    {
                        HasLesson = allLessonsForCell.Any(),
                        Lesson = lessonsForCell.FirstOrDefault(),
                        Lessons = allLessonsForCell,
                        HasTeamTeaching = allLessonsForCell.Any(l => l.LessonTeachers.Count() > 1),
                        HasParallelLessons = subgroupLessons.Any(),
                        ParallelLessonsCount = subgroupLessons.Count(),
                        TotalFrequencyPerWeek = totalFrequency
                    };
                }
            }
        }

        // API: Create new lesson
        public async Task<IActionResult> OnPostCreateLessonAsync([FromBody] LessonCreateModel model)
        {
            if (model == null)
            {
                return new JsonResult(new { success = false, message = "Invalid data" });
            }

            var lesson = new Lesson
            {
                Description = model.Description,
                Duration = model.Duration > 0 ? model.Duration : 1,
                FrequencyPerWeek = model.FrequencyPerWeek > 0 ? model.FrequencyPerWeek : 1,
                ClassPeriodsPerWeek = model.ClassPeriodsPerWeek,
                TeacherPeriodsPerWeek = model.TeacherPeriodsPerWeek,
                SpecialRequirements = model.SpecialRequirements,
                IsActive = model.IsActive
            };

            // Add teachers if provided
            if (model.TeacherIds != null && model.TeacherIds.Any())
            {
                int order = 0;
                foreach (var teacherId in model.TeacherIds)
                {
                    lesson.LessonTeachers.Add(new LessonTeacher
                    {
                        TeacherId = teacherId,
                        IsLead = order == 0,
                        Order = order++
                    });
                }
            }

            // Add subjects if provided
            if (model.SubjectIds != null && model.SubjectIds.Any())
            {
                int order = 0;
                foreach (var subjectId in model.SubjectIds)
                {
                    lesson.LessonSubjects.Add(new LessonSubject
                    {
                        SubjectId = subjectId,
                        IsPrimary = order == 0,
                        Order = order++
                    });
                }
            }

            // Add classes if provided
            if (model.ClassIds != null && model.ClassIds.Any())
            {
                int order = 0;
                foreach (var classId in model.ClassIds)
                {
                    lesson.LessonClasses.Add(new LessonClass
                    {
                        ClassId = classId,
                        IsPrimary = order == 0,
                        Order = order++
                    });
                }
            }

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            return new JsonResult(new {
                success = true,
                message = "Lesson created successfully",
                lessonId = lesson.Id
            });
        }

        // API: Get lesson data for editing
        public async Task<IActionResult> OnGetLessonAsync(int id)
        {
            var lesson = await _context.Lessons
                .Include(l => l.LessonTeachers)
                    .ThenInclude(lt => lt.Teacher)
                .Include(l => l.LessonSubjects)
                    .ThenInclude(ls => ls.Subject)
                .Include(l => l.LessonClasses)
                    .ThenInclude(lc => lc.Class)
                .Include(l => l.LessonAssignments)
                    .ThenInclude(la => la.Teacher)
                .Include(l => l.LessonAssignments)
                    .ThenInclude(la => la.Subject)
                .Include(l => l.LessonAssignments)
                    .ThenInclude(la => la.Class)
                .AsSplitQuery()
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null)
            {
                return new JsonResult(new { success = false, message = "Lesson not found" });
            }

            var result = new
            {
                success = true,
                lesson = new
                {
                    lesson.Id,
                    lesson.Duration,
                    lesson.FrequencyPerWeek,
                    lesson.SpecialRequirements,
                    lesson.Description,
                    lesson.IsActive,
                    teachers = lesson.LessonTeachers.Select(lt => new
                    {
                        lt.Id,
                        lt.TeacherId,
                        teacherName = lt.Teacher?.FullName,
                        lt.IsLead,
                        lt.Order,
                        lt.WorkloadPercentage,
                        lt.Role
                    }).OrderBy(t => t.Order).ToList(),
                    subjects = lesson.LessonSubjects.Select(ls => new
                    {
                        ls.Id,
                        ls.SubjectId,
                        subjectName = ls.Subject?.Name,
                        subjectCode = ls.Subject?.Code,
                        ls.IsPrimary,
                        ls.Order
                    }).OrderBy(s => s.Order).ToList(),
                    classes = lesson.LessonClasses.Select(lc => new
                    {
                        lc.Id,
                        lc.ClassId,
                        className = lc.Class?.Name,
                        lc.IsPrimary,
                        lc.Order
                    }).OrderBy(c => c.Order).ToList(),
                    assignments = lesson.LessonAssignments.Select(la => new
                    {
                        la.Id,
                        la.TeacherId,
                        teacherName = la.Teacher?.ShortName ?? la.Teacher?.FullName,
                        la.SubjectId,
                        subjectName = la.Subject?.Name,
                        subjectCode = la.Subject?.Code,
                        la.ClassId,
                        className = la.Class?.Name,
                        la.Notes,
                        la.Order
                    }).OrderBy(a => a.Order).ToList()
                }
            };

            return new JsonResult(result);
        }

        // API: Update lesson basic properties
        public async Task<IActionResult> OnPostUpdateLessonAsync([FromBody] LessonUpdateModel model)
        {
            if (model == null)
            {
                return new JsonResult(new { success = false, message = "Invalid data" });
            }

            var lesson = await _context.Lessons.FindAsync(model.Id);
            if (lesson == null)
            {
                return new JsonResult(new { success = false, message = "Lesson not found" });
            }

            lesson.Duration = model.Duration;
            lesson.FrequencyPerWeek = model.FrequencyPerWeek;
            lesson.SpecialRequirements = model.SpecialRequirements;
            lesson.Description = model.Description;
            lesson.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, message = "Lesson updated successfully" });
        }

        // API: Update lesson teachers
        public async Task<IActionResult> OnPostUpdateTeachersAsync([FromBody] LessonTeachersUpdateModel model)
        {
            if (model == null)
            {
                return new JsonResult(new { success = false, message = "Invalid data" });
            }

            var lesson = await _context.Lessons
                .Include(l => l.LessonTeachers)
                .FirstOrDefaultAsync(l => l.Id == model.LessonId);

            if (lesson == null)
            {
                return new JsonResult(new { success = false, message = "Lesson not found" });
            }

            // Remove existing teachers
            _context.LessonTeachers.RemoveRange(lesson.LessonTeachers);

            // Add new teachers
            if (model.TeacherIds != null && model.TeacherIds.Any())
            {
                int order = 0;
                foreach (var teacherId in model.TeacherIds)
                {
                    _context.LessonTeachers.Add(new LessonTeacher
                    {
                        LessonId = model.LessonId,
                        TeacherId = teacherId,
                        IsLead = order == 0, // First teacher is lead
                        Order = order++
                    });
                }
            }

            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, message = "Teachers updated successfully" });
        }

        // API: Update lesson subjects
        public async Task<IActionResult> OnPostUpdateSubjectsAsync([FromBody] LessonSubjectsUpdateModel model)
        {
            if (model == null)
            {
                return new JsonResult(new { success = false, message = "Invalid data" });
            }

            var lesson = await _context.Lessons
                .Include(l => l.LessonSubjects)
                .FirstOrDefaultAsync(l => l.Id == model.LessonId);

            if (lesson == null)
            {
                return new JsonResult(new { success = false, message = "Lesson not found" });
            }

            // Remove existing subjects
            _context.LessonSubjects.RemoveRange(lesson.LessonSubjects);

            // Add new subjects
            if (model.SubjectIds != null && model.SubjectIds.Any())
            {
                int order = 0;
                foreach (var subjectId in model.SubjectIds)
                {
                    _context.LessonSubjects.Add(new LessonSubject
                    {
                        LessonId = model.LessonId,
                        SubjectId = subjectId,
                        IsPrimary = order == 0, // First subject is primary
                        Order = order++
                    });
                }
            }

            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, message = "Subjects updated successfully" });
        }

        // API: Update lesson classes
        public async Task<IActionResult> OnPostUpdateClassesAsync([FromBody] LessonClassesUpdateModel model)
        {
            if (model == null)
            {
                return new JsonResult(new { success = false, message = "Invalid data" });
            }

            var lesson = await _context.Lessons
                .Include(l => l.LessonClasses)
                .FirstOrDefaultAsync(l => l.Id == model.LessonId);

            if (lesson == null)
            {
                return new JsonResult(new { success = false, message = "Lesson not found" });
            }

            // Remove existing classes
            _context.LessonClasses.RemoveRange(lesson.LessonClasses);

            // Add new classes
            if (model.ClassIds != null && model.ClassIds.Any())
            {
                int order = 0;
                foreach (var classId in model.ClassIds)
                {
                    _context.LessonClasses.Add(new LessonClass
                    {
                        LessonId = model.LessonId,
                        ClassId = classId,
                        IsPrimary = order == 0, // First class is primary
                        Order = order++
                    });
                }
            }

            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, message = "Classes updated successfully" });
        }

        // API: Delete lesson
        public async Task<IActionResult> OnPostDeleteLessonAsync([FromBody] DeleteLessonModel model)
        {
            if (model == null)
            {
                return new JsonResult(new { success = false, message = "Invalid data" });
            }

            var lesson = await _context.Lessons
                .Include(l => l.LessonTeachers)
                .Include(l => l.LessonSubjects)
                .Include(l => l.LessonClasses)
                .Include(l => l.LessonAssignments)
                .Include(l => l.ScheduledLessons)
                .FirstOrDefaultAsync(l => l.Id == model.LessonId);

            if (lesson == null)
            {
                return new JsonResult(new { success = false, message = "Lesson not found" });
            }

            // Check if there are scheduled lessons
            if (lesson.ScheduledLessons.Any())
            {
                return new JsonResult(new {
                    success = false,
                    message = $"Cannot delete: This lesson has {lesson.ScheduledLessons.Count} scheduled instance(s). Remove them from the timetable first."
                });
            }

            // Remove related records
            _context.LessonTeachers.RemoveRange(lesson.LessonTeachers);
            _context.LessonSubjects.RemoveRange(lesson.LessonSubjects);
            _context.LessonClasses.RemoveRange(lesson.LessonClasses);
            _context.LessonAssignments.RemoveRange(lesson.LessonAssignments);
            _context.Lessons.Remove(lesson);

            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, message = "Lesson deleted successfully" });
        }

        // Models for API requests
        public class DeleteLessonModel
        {
            public int LessonId { get; set; }
        }
        public class LessonCreateModel
        {
            public string? Description { get; set; }
            public int Duration { get; set; }
            public int FrequencyPerWeek { get; set; }
            public int? ClassPeriodsPerWeek { get; set; }
            public int? TeacherPeriodsPerWeek { get; set; }
            public string? SpecialRequirements { get; set; }
            public bool IsActive { get; set; }
            public List<int>? TeacherIds { get; set; }
            public List<int>? SubjectIds { get; set; }
            public List<int>? ClassIds { get; set; }
        }

        public class LessonUpdateModel
        {
            public int Id { get; set; }
            public int Duration { get; set; }
            public int FrequencyPerWeek { get; set; }
            public string? SpecialRequirements { get; set; }
            public string? Description { get; set; }
            public bool IsActive { get; set; }
        }

        public class LessonTeachersUpdateModel
        {
            public int LessonId { get; set; }
            public List<int>? TeacherIds { get; set; }
        }

        public class LessonSubjectsUpdateModel
        {
            public int LessonId { get; set; }
            public List<int>? SubjectIds { get; set; }
        }

        public class LessonClassesUpdateModel
        {
            public int LessonId { get; set; }
            public List<int>? ClassIds { get; set; }
        }

        public class GradeStats
        {
            public int YearLevel { get; set; }
            public string GradeName { get; set; } = string.Empty;
            public int TotalClasses { get; set; }
            public int ExpectedLessons { get; set; }
            public int ActualLessons { get; set; }
            public int MissingLessons { get; set; }
            public double CompletionPercentage { get; set; }
        }

        public class LessonCell
        {
            public bool HasLesson { get; set; }
            public Lesson? Lesson { get; set; }
            public List<Lesson> Lessons { get; set; } = new();
            public bool HasTeamTeaching { get; set; }
            public bool HasParallelLessons { get; set; }
            public int ParallelLessonsCount { get; set; }
            public int TotalFrequencyPerWeek { get; set; }
        }

        public class TimetableInfo
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public TimetableStatus Status { get; set; }
        }
    }
}
