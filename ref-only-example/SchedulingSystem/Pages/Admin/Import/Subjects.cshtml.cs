using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;
using SchedulingSystem.Services;
using System.Text;

namespace SchedulingSystem.Pages.Admin.Import
{
    public class SubjectsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SubjectsModel(ApplicationDbContext context)
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
            template.AppendLine("Code,Name,Color");
            template.AppendLine("ENG,English,#4CAF50");
            template.AppendLine("MATH,Mathematics,#2196F3");
            template.AppendLine("SCI,Science,#FF9800");

            var bytes = Encoding.UTF8.GetBytes(template.ToString());
            return File(bytes, "text/csv", "subjects_template.csv");
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

                // Get existing subject codes to check for duplicates
                var existingCodes = await _context.Subjects
                    .Select(s => s.Code.ToLower())
                    .ToListAsync();

                var subjectsToImport = new List<Subject>();
                int rowNumber = 1;

                foreach (var row in rows)
                {
                    rowNumber++;

                    // Validate required fields
                    if (!row.ContainsKey("Code") || string.IsNullOrWhiteSpace(row["Code"]))
                    {
                        Errors.Add($"Row {rowNumber}: Code is required.");
                        continue;
                    }

                    if (!row.ContainsKey("Name") || string.IsNullOrWhiteSpace(row["Name"]))
                    {
                        Errors.Add($"Row {rowNumber}: Name is required.");
                        continue;
                    }

                    var code = row["Code"].Trim();
                    var name = row["Name"].Trim();

                    // Check for duplicate Code in existing data
                    if (existingCodes.Contains(code.ToLower()))
                    {
                        Warnings.Add($"Row {rowNumber}: Subject with code '{code}' already exists. Skipping.");
                        SkippedCount++;
                        continue;
                    }

                    // Check for duplicate Code in current import batch
                    if (subjectsToImport.Any(s => s.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
                    {
                        Warnings.Add($"Row {rowNumber}: Duplicate Code '{code}' in import file. Skipping.");
                        SkippedCount++;
                        continue;
                    }

                    // Parse optional fields
                    string color = "#808080"; // Default gray
                    if (row.ContainsKey("Color") && !string.IsNullOrWhiteSpace(row["Color"]))
                    {
                        var colorValue = row["Color"].Trim();
                        // Basic validation: check if it starts with # and has 7 characters
                        if (colorValue.StartsWith("#") && colorValue.Length == 7)
                        {
                            color = colorValue;
                        }
                        else
                        {
                            Warnings.Add($"Row {rowNumber}: Invalid Color format '{colorValue}'. Using default gray (#808080).");
                        }
                    }

                    var subject = new Subject
                    {
                        Code = code,
                        Name = name,
                        Color = color
                    };

                    subjectsToImport.Add(subject);
                }

                // Import valid subjects
                if (subjectsToImport.Any())
                {
                    _context.Subjects.AddRange(subjectsToImport);
                    await _context.SaveChangesAsync();
                    ImportedCount = subjectsToImport.Count;
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
