using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;
using SchedulingSystem.Services;
using System.Text;

namespace SchedulingSystem.Pages.Admin.Import
{
    public class TeachersModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public TeachersModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public IFormFile? UploadedFile { get; set; }

        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }
        public bool ShowResults { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnGetDownloadTemplate()
        {
            var template = new StringBuilder();
            template.AppendLine("FirstName,LastName,Email,PhoneNumber,MaxHoursPerWeek,IsActive");
            template.AppendLine("John,Doe,john.doe@school.edu,123-456-7890,40,true");
            template.AppendLine("Jane,Smith,jane.smith@school.edu,123-456-7891,40,true");

            var bytes = Encoding.UTF8.GetBytes(template.ToString());
            return File(bytes, "text/csv", "teachers_template.csv");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (UploadedFile == null || UploadedFile.Length == 0)
            {
                Errors.Add("Please select a CSV file to upload.");
                ShowResults = true;
                return Page();
            }

            try
            {
                using var stream = UploadedFile.OpenReadStream();
                var rows = CsvService.ParseCsv(stream);

                if (rows.Count == 0)
                {
                    Errors.Add("The CSV file is empty or has no data rows.");
                    ShowResults = true;
                    return Page();
                }

                // Get existing emails to check for duplicates
                var existingEmails = await _context.Teachers
                    .Where(t => t.Email != null)
                    .Select(t => t.Email.ToLower())
                    .ToListAsync();

                var teachersToImport = new List<Teacher>();
                int rowNumber = 1; // Start from 1 (header is row 0)

                foreach (var row in rows)
                {
                    rowNumber++;

                    // Validate required fields
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

                    if (!row.ContainsKey("Email") || string.IsNullOrWhiteSpace(row["Email"]))
                    {
                        Errors.Add($"Row {rowNumber}: Email is required.");
                        continue;
                    }

                    var email = row["Email"].Trim();

                    // Check for duplicate email in existing data
                    if (existingEmails.Contains(email.ToLower()))
                    {
                        Warnings.Add($"Row {rowNumber}: Teacher with email '{email}' already exists. Skipping.");
                        SkippedCount++;
                        continue;
                    }

                    // Check for duplicate in current import batch
                    if (teachersToImport.Any(t => t.Email != null && t.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
                    {
                        Warnings.Add($"Row {rowNumber}: Duplicate email '{email}' in import file. Skipping.");
                        SkippedCount++;
                        continue;
                    }

                    // Parse MaxHoursPerWeek
                    int maxHours = 40; // default
                    if (row.ContainsKey("MaxHoursPerWeek") && !string.IsNullOrWhiteSpace(row["MaxHoursPerWeek"]))
                    {
                        if (!int.TryParse(row["MaxHoursPerWeek"], out maxHours))
                        {
                            Warnings.Add($"Row {rowNumber}: Invalid MaxHoursPerWeek '{row["MaxHoursPerWeek"]}'. Using default (40).");
                            maxHours = 40;
                        }
                    }

                    // Parse IsActive
                    bool isActive = true; // default
                    if (row.ContainsKey("IsActive") && !string.IsNullOrWhiteSpace(row["IsActive"]))
                    {
                        if (!bool.TryParse(row["IsActive"], out isActive))
                        {
                            Warnings.Add($"Row {rowNumber}: Invalid IsActive value '{row["IsActive"]}'. Using default (true).");
                            isActive = true;
                        }
                    }

                    var teacher = new Teacher
                    {
                        FirstName = row["FirstName"].Trim(),
                        LastName = row["LastName"].Trim(),
                        Email = email,
                        PhoneNumber = row.ContainsKey("PhoneNumber") ? row["PhoneNumber"].Trim() : null,
                        MaxHoursPerWeek = maxHours,
                        IsActive = isActive
                    };

                    teachersToImport.Add(teacher);
                }

                // Import valid records
                if (teachersToImport.Any())
                {
                    _context.Teachers.AddRange(teachersToImport);
                    await _context.SaveChangesAsync();
                    ImportedCount = teachersToImport.Count;
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
