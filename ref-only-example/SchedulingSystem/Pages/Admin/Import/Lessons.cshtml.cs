using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;
using SchedulingSystem.Services;
using System.Text;

namespace SchedulingSystem.Pages.Admin.Import
{
    public class LessonsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LessonsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public IFormFile? UploadedFile { get; set; }

        public bool ShowResults { get; set; }
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public void OnGet()
        {
        }

        public IActionResult OnGetDownloadTemplate()
        {
            var template = new StringBuilder();
            template.AppendLine("SubjectCode,ClassName,TeacherEmail,SecondTeacherEmail,FrequencyPerWeek,Duration,RequiredRoomType,SpecialRequirements,IsActive");
            template.AppendLine("MATH,9A,john.doe@school.edu,,4,45,Classroom,,true");
            template.AppendLine("SCI,9A,jane.smith@school.edu,bob.jones@school.edu,3,90,Laboratory,Team Teaching,true");
            template.AppendLine("ENG,9B,mary.white@school.edu,,5,45,,,true");

            var bytes = Encoding.UTF8.GetBytes(template.ToString());
            return File(bytes, "text/csv", "lessons_template.csv");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (UploadedFile == null || UploadedFile.Length == 0)
            {
                Errors.Add("Please select a valid CSV file.");
                ShowResults = true;
                return Page();
            }

            try
            {
                using var stream = UploadedFile.OpenReadStream();
                var rows = CsvService.ParseCsv(stream);

                if (rows.Count == 0)
                {
                    Errors.Add("The CSV file is empty or contains no valid data rows.");
                    ShowResults = true;
                    return Page();
                }

                // Load lookups
                var subjects = await _context.Subjects
                    .ToDictionaryAsync(s => s.Code.ToLower(), s => s.Id);

                var classes = await _context.Classes
                    .ToDictionaryAsync(c => c.Name.ToLower(), c => c.Id);

                var teachers = await _context.Teachers
                    .Where(t => t.Email != null)
                    .ToDictionaryAsync(t => t.Email.ToLower(), t => t.Id);

                // Get existing lessons to check for duplicates (same Subject + Class + Teacher)
                var existingLessons = await _context.Lessons
                    .Include(l => l.LessonSubjects)
                    .Include(l => l.LessonClasses)
                    .Include(l => l.LessonTeachers)
                    .ToListAsync();

                var lessonsToImport = new List<Lesson>();
                int rowNumber = 1;

                foreach (var row in rows)
                {
                    rowNumber++;

                    // Validate required fields
                    if (!row.ContainsKey("SubjectCode") || string.IsNullOrWhiteSpace(row["SubjectCode"]))
                    {
                        Errors.Add($"Row {rowNumber}: SubjectCode is required.");
                        continue;
                    }

                    if (!row.ContainsKey("ClassName") || string.IsNullOrWhiteSpace(row["ClassName"]))
                    {
                        Errors.Add($"Row {rowNumber}: ClassName is required.");
                        continue;
                    }

                    if (!row.ContainsKey("TeacherEmail") || string.IsNullOrWhiteSpace(row["TeacherEmail"]))
                    {
                        Errors.Add($"Row {rowNumber}: TeacherEmail is required.");
                        continue;
                    }

                    var subjectCode = row["SubjectCode"].Trim();
                    var className = row["ClassName"].Trim();
                    var teacherEmail = row["TeacherEmail"].Trim();

                    // Lookup SubjectId
                    if (!subjects.TryGetValue(subjectCode.ToLower(), out var subjectId))
                    {
                        Errors.Add($"Row {rowNumber}: Subject '{subjectCode}' not found. Please ensure the subject exists.");
                        continue;
                    }

                    // Lookup ClassId
                    if (!classes.TryGetValue(className.ToLower(), out var classId))
                    {
                        Errors.Add($"Row {rowNumber}: Class '{className}' not found. Please ensure the class exists.");
                        continue;
                    }

                    // Lookup TeacherId
                    if (!teachers.TryGetValue(teacherEmail.ToLower(), out var teacherId))
                    {
                        Errors.Add($"Row {rowNumber}: Teacher '{teacherEmail}' not found. Please ensure the teacher exists.");
                        continue;
                    }

                    // Check for duplicate lesson in existing data
                    if (existingLessons.Any(l => l.LessonSubjects.Any(ls => ls.SubjectId == subjectId) &&
                                                 l.LessonClasses.Any(lc => lc.ClassId == classId) &&
                                                 l.LessonTeachers.Any(lt => lt.TeacherId == teacherId)))
                    {
                        Warnings.Add($"Row {rowNumber}: Lesson with Subject '{subjectCode}', Class '{className}', and Teacher '{teacherEmail}' already exists. Skipping.");
                        SkippedCount++;
                        continue;
                    }

                    // Check for duplicate in current import batch
                    if (lessonsToImport.Any(l => l.LessonSubjects.Any(ls => ls.SubjectId == subjectId) &&
                                                 l.LessonClasses.Any(lc => lc.ClassId == classId) &&
                                                 l.LessonTeachers.Any(lt => lt.TeacherId == teacherId)))
                    {
                        Warnings.Add($"Row {rowNumber}: Duplicate lesson in import file. Skipping.");
                        SkippedCount++;
                        continue;
                    }

                    // Lookup optional SecondTeacherId
                    int? secondTeacherId = null;
                    if (row.ContainsKey("SecondTeacherEmail") && !string.IsNullOrWhiteSpace(row["SecondTeacherEmail"]))
                    {
                        var secondTeacherEmail = row["SecondTeacherEmail"].Trim();
                        if (teachers.TryGetValue(secondTeacherEmail.ToLower(), out var secondId))
                        {
                            secondTeacherId = secondId;
                        }
                        else
                        {
                            Warnings.Add($"Row {rowNumber}: Second teacher '{secondTeacherEmail}' not found. Proceeding without second teacher.");
                        }
                    }

                    // Parse FrequencyPerWeek (default: 1)
                    int frequencyPerWeek = 1;
                    if (row.ContainsKey("FrequencyPerWeek") && !string.IsNullOrWhiteSpace(row["FrequencyPerWeek"]))
                    {
                        if (!int.TryParse(row["FrequencyPerWeek"], out frequencyPerWeek))
                        {
                            Warnings.Add($"Row {rowNumber}: Invalid FrequencyPerWeek '{row["FrequencyPerWeek"]}'. Using default (1).");
                            frequencyPerWeek = 1;
                        }
                    }

                    // Parse Duration (default: 45)
                    int duration = 45;
                    if (row.ContainsKey("Duration") && !string.IsNullOrWhiteSpace(row["Duration"]))
                    {
                        if (!int.TryParse(row["Duration"], out duration))
                        {
                            Warnings.Add($"Row {rowNumber}: Invalid Duration '{row["Duration"]}'. Using default (45).");
                            duration = 45;
                        }
                    }

                    // Parse optional fields
                    string? requiredRoomType = null;
                    if (row.ContainsKey("RequiredRoomType") && !string.IsNullOrWhiteSpace(row["RequiredRoomType"]))
                    {
                        requiredRoomType = row["RequiredRoomType"].Trim();
                    }

                    string? specialRequirements = null;
                    if (row.ContainsKey("SpecialRequirements") && !string.IsNullOrWhiteSpace(row["SpecialRequirements"]))
                    {
                        specialRequirements = row["SpecialRequirements"].Trim();
                    }

                    bool isActive = true;
                    if (row.ContainsKey("IsActive") && !string.IsNullOrWhiteSpace(row["IsActive"]))
                    {
                        if (bool.TryParse(row["IsActive"], out var active))
                        {
                            isActive = active;
                        }
                        else
                        {
                            Warnings.Add($"Row {rowNumber}: Invalid IsActive value '{row["IsActive"]}'. Using default (true).");
                        }
                    }

                    var lesson = new Lesson
                    {
                        FrequencyPerWeek = frequencyPerWeek,
                        Duration = duration,
                        RequiredRoomType = requiredRoomType,
                        SpecialRequirements = specialRequirements,
                        IsActive = isActive,
                        LessonSubjects = new List<LessonSubject>
                        {
                            new LessonSubject { SubjectId = subjectId }
                        },
                        LessonClasses = new List<LessonClass>
                        {
                            new LessonClass { ClassId = classId }
                        },
                        LessonTeachers = new List<LessonTeacher>
                        {
                            new LessonTeacher { TeacherId = teacherId }
                        }
                    };

                    // Add second teacher if provided
                    if (secondTeacherId.HasValue)
                    {
                        lesson.LessonTeachers.Add(new LessonTeacher { TeacherId = secondTeacherId.Value });
                    }

                    lessonsToImport.Add(lesson);
                }

                // Import valid lessons
                if (lessonsToImport.Any())
                {
                    _context.Lessons.AddRange(lessonsToImport);
                    await _context.SaveChangesAsync();
                    ImportedCount = lessonsToImport.Count;
                }

                ShowResults = true;
            }
            catch (Exception ex)
            {
                Errors.Add($"An error occurred while processing the file: {ex.Message}");
                ShowResults = true;
            }

            return Page();
        }
    }
}
