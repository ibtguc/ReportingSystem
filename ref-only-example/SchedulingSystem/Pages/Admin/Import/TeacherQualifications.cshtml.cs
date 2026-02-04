using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;
using SchedulingSystem.Services;
using System.Text;

namespace SchedulingSystem.Pages.Admin.Import
{
    public class TeacherQualificationsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public TeacherQualificationsModel(ApplicationDbContext context)
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
            template.AppendLine("TeacherEmail,SubjectCode,QualificationLevel,ClassLevelFrom,ClassLevelTo,IsPreferred,Notes");
            template.AppendLine("john.doe@school.edu,MATH,Master,1,12,true,Mathematics specialist");
            template.AppendLine("jane.smith@school.edu,SCI,PhD,9,12,true,Physics background - upper school");
            template.AppendLine("bob.jones@school.edu,ENG,Bachelor,1,8,false,Lower school only");

            var bytes = Encoding.UTF8.GetBytes(template.ToString());
            return File(bytes, "text/csv", "teacher_qualifications_template.csv");
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
                var teachers = await _context.Teachers
                    .Where(t => t.Email != null)
                    .ToDictionaryAsync(t => t.Email.ToLower(), t => t.Id);

                var subjects = await _context.Subjects
                    .ToDictionaryAsync(s => s.Code.ToLower(), s => s.Id);

                // Get existing qualifications to check for duplicates
                var existingQualifications = await _context.TeacherSubjects
                    .Select(ts => new { ts.TeacherId, ts.SubjectId })
                    .ToListAsync();

                var qualificationsToImport = new List<TeacherSubject>();
                int rowNumber = 1;

                foreach (var row in rows)
                {
                    rowNumber++;

                    // Validate required fields
                    if (!row.ContainsKey("TeacherEmail") || string.IsNullOrWhiteSpace(row["TeacherEmail"]))
                    {
                        Errors.Add($"Row {rowNumber}: TeacherEmail is required.");
                        continue;
                    }

                    if (!row.ContainsKey("SubjectCode") || string.IsNullOrWhiteSpace(row["SubjectCode"]))
                    {
                        Errors.Add($"Row {rowNumber}: SubjectCode is required.");
                        continue;
                    }

                    var teacherEmail = row["TeacherEmail"].Trim();
                    var subjectCode = row["SubjectCode"].Trim();

                    // Lookup TeacherId
                    if (!teachers.TryGetValue(teacherEmail.ToLower(), out var teacherId))
                    {
                        Errors.Add($"Row {rowNumber}: Teacher '{teacherEmail}' not found. Please ensure the teacher exists.");
                        continue;
                    }

                    // Lookup SubjectId
                    if (!subjects.TryGetValue(subjectCode.ToLower(), out var subjectId))
                    {
                        Errors.Add($"Row {rowNumber}: Subject '{subjectCode}' not found. Please ensure the subject exists.");
                        continue;
                    }

                    // Check for duplicate qualification in existing data
                    if (existingQualifications.Any(q => q.TeacherId == teacherId && q.SubjectId == subjectId))
                    {
                        Warnings.Add($"Row {rowNumber}: Qualification for Teacher '{teacherEmail}' and Subject '{subjectCode}' already exists. Skipping.");
                        SkippedCount++;
                        continue;
                    }

                    // Check for duplicate in current import batch
                    if (qualificationsToImport.Any(q => q.TeacherId == teacherId && q.SubjectId == subjectId))
                    {
                        Warnings.Add($"Row {rowNumber}: Duplicate qualification in import file. Skipping.");
                        SkippedCount++;
                        continue;
                    }

                    // Parse optional fields
                    string? qualificationLevel = null;
                    if (row.ContainsKey("QualificationLevel") && !string.IsNullOrWhiteSpace(row["QualificationLevel"]))
                    {
                        qualificationLevel = row["QualificationLevel"].Trim();
                    }

                    bool isPreferred = false;
                    if (row.ContainsKey("IsPreferred") && !string.IsNullOrWhiteSpace(row["IsPreferred"]))
                    {
                        if (bool.TryParse(row["IsPreferred"], out var preferred))
                        {
                            isPreferred = preferred;
                        }
                        else
                        {
                            Warnings.Add($"Row {rowNumber}: Invalid IsPreferred value '{row["IsPreferred"]}'. Using default (false).");
                        }
                    }

                    string? notes = null;
                    if (row.ContainsKey("Notes") && !string.IsNullOrWhiteSpace(row["Notes"]))
                    {
                        notes = row["Notes"].Trim();
                    }

                    // Parse class level range
                    int? classLevelFrom = null;
                    if (row.ContainsKey("ClassLevelFrom") && !string.IsNullOrWhiteSpace(row["ClassLevelFrom"]))
                    {
                        if (int.TryParse(row["ClassLevelFrom"], out var levelFrom))
                        {
                            classLevelFrom = levelFrom;
                        }
                        else
                        {
                            Warnings.Add($"Row {rowNumber}: Invalid ClassLevelFrom value '{row["ClassLevelFrom"]}'.");
                        }
                    }

                    int? classLevelTo = null;
                    if (row.ContainsKey("ClassLevelTo") && !string.IsNullOrWhiteSpace(row["ClassLevelTo"]))
                    {
                        if (int.TryParse(row["ClassLevelTo"], out var levelTo))
                        {
                            classLevelTo = levelTo;
                        }
                        else
                        {
                            Warnings.Add($"Row {rowNumber}: Invalid ClassLevelTo value '{row["ClassLevelTo"]}'.");
                        }
                    }

                    var qualification = new TeacherSubject
                    {
                        TeacherId = teacherId,
                        SubjectId = subjectId,
                        QualificationLevel = qualificationLevel,
                        ClassLevelFrom = classLevelFrom,
                        ClassLevelTo = classLevelTo,
                        IsPreferred = isPreferred,
                        Notes = notes
                    };

                    qualificationsToImport.Add(qualification);
                }

                // Import valid qualifications
                if (qualificationsToImport.Any())
                {
                    _context.TeacherSubjects.AddRange(qualificationsToImport);
                    await _context.SaveChangesAsync();
                    ImportedCount = qualificationsToImport.Count;
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
