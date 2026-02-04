using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;
using SchedulingSystem.Services;
using System.Text;

namespace SchedulingSystem.Pages.Admin.Import
{
    public class StudentsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public StudentsModel(ApplicationDbContext context)
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
            template.AppendLine("StudentNumber,FirstName,LastName,ClassName,DateOfBirth,IsActive");
            template.AppendLine("S2024001,Alice,Johnson,9A,2009-05-15,true");
            template.AppendLine("S2024002,Bob,Williams,9A,2009-07-22,true");
            template.AppendLine("S2024003,Charlie,Brown,9B,2009-03-10,true");

            var bytes = Encoding.UTF8.GetBytes(template.ToString());
            return File(bytes, "text/csv", "students_template.csv");
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

                // Get existing student numbers to check for duplicates
                var existingStudentNumbers = await _context.Students
                    .Select(s => s.StudentNumber.ToLower())
                    .ToListAsync();

                // Get all classes for lookup
                var allClasses = await _context.Classes
                    .ToDictionaryAsync(c => c.Name.ToLower(), c => c.Id);

                var studentsToImport = new List<Student>();
                int rowNumber = 1;

                foreach (var row in rows)
                {
                    rowNumber++;

                    // Validate required fields
                    if (!row.ContainsKey("StudentNumber") || string.IsNullOrWhiteSpace(row["StudentNumber"]))
                    {
                        Errors.Add($"Row {rowNumber}: StudentNumber is required.");
                        continue;
                    }

                    if (!row.ContainsKey("FirstName") || string.IsNullOrWhiteSpace(row["FirstName"]))
                    {
                        Errors.Add($"Row {rowNumber}: FirstName is required.");
                        continue;
                    }

                    if (!row.ContainsKey("LastName") || string.IsNullOrWhiteSpace(row["LastName"]))
                    {
                        Errors.Add($"Row {rowNumber}: LastName is required.");
                        continue;
                    }

                    if (!row.ContainsKey("ClassName") || string.IsNullOrWhiteSpace(row["ClassName"]))
                    {
                        Errors.Add($"Row {rowNumber}: ClassName is required.");
                        continue;
                    }

                    var studentNumber = row["StudentNumber"].Trim();
                    var firstName = row["FirstName"].Trim();
                    var lastName = row["LastName"].Trim();
                    var className = row["ClassName"].Trim();

                    // Check for duplicate StudentNumber in existing data
                    if (existingStudentNumbers.Contains(studentNumber.ToLower()))
                    {
                        Warnings.Add($"Row {rowNumber}: Student with number '{studentNumber}' already exists. Skipping.");
                        SkippedCount++;
                        continue;
                    }

                    // Check for duplicate StudentNumber in current import batch
                    if (studentsToImport.Any(s => s.StudentNumber.Equals(studentNumber, StringComparison.OrdinalIgnoreCase)))
                    {
                        Warnings.Add($"Row {rowNumber}: Duplicate StudentNumber '{studentNumber}' in import file. Skipping.");
                        SkippedCount++;
                        continue;
                    }

                    // Lookup ClassId
                    if (!allClasses.TryGetValue(className.ToLower(), out var classId))
                    {
                        Errors.Add($"Row {rowNumber}: Class '{className}' not found. Please ensure the class exists before importing students.");
                        continue;
                    }

                    // Parse optional fields
                    DateTime? dateOfBirth = null;
                    if (row.ContainsKey("DateOfBirth") && !string.IsNullOrWhiteSpace(row["DateOfBirth"]))
                    {
                        if (DateTime.TryParse(row["DateOfBirth"], out var dob))
                        {
                            dateOfBirth = dob;
                        }
                        else
                        {
                            Warnings.Add($"Row {rowNumber}: Invalid DateOfBirth format '{row["DateOfBirth"]}'. Using null.");
                        }
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

                    var student = new Student
                    {
                        StudentNumber = studentNumber,
                        Name = studentNumber, // Short name
                        FirstName = firstName,
                        FullName = lastName, // Last name goes to FullName as per GPU010
                        ClassId = classId,
                        DateOfBirth = dateOfBirth,
                        IsActive = isActive
                    };

                    studentsToImport.Add(student);
                }

                // Import valid students
                if (studentsToImport.Any())
                {
                    _context.Students.AddRange(studentsToImport);
                    await _context.SaveChangesAsync();
                    ImportedCount = studentsToImport.Count;
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
