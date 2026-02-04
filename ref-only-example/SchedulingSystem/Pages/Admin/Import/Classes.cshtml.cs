using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;
using SchedulingSystem.Services;
using System.Text;

namespace SchedulingSystem.Pages.Admin.Import
{
    public class ClassesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ClassesModel(ApplicationDbContext context)
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
            template.AppendLine("Name,YearLevel,StudentCount,ParentClassName,IsActive");
            template.AppendLine("9A,9,30,,true");
            template.AppendLine("9B,9,30,,true");
            template.AppendLine("9A-1,9,15,9A,true");
            template.AppendLine("9A-2,9,15,9A,true");

            var bytes = Encoding.UTF8.GetBytes(template.ToString());
            return File(bytes, "text/csv", "classes_template.csv");
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

                // Get existing class names to check for duplicates
                var existingNames = await _context.Classes
                    .Select(c => c.Name.ToLower())
                    .ToListAsync();

                var classesToImport = new List<Class>();
                int rowNumber = 1;

                // First pass: import parent classes (classes without ParentClassName)
                foreach (var row in rows)
                {
                    rowNumber++;

                    // Validate required fields
                    if (!row.ContainsKey("Name") || string.IsNullOrWhiteSpace(row["Name"]))
                    {
                        Errors.Add($"Row {rowNumber}: Name is required.");
                        continue;
                    }

                    if (!row.ContainsKey("YearLevel") || string.IsNullOrWhiteSpace(row["YearLevel"]))
                    {
                        Errors.Add($"Row {rowNumber}: YearLevel is required.");
                        continue;
                    }

                    if (!row.ContainsKey("StudentCount") || string.IsNullOrWhiteSpace(row["StudentCount"]))
                    {
                        Errors.Add($"Row {rowNumber}: StudentCount is required.");
                        continue;
                    }

                    var name = row["Name"].Trim();

                    // Skip if this is a subgroup (has ParentClassName) - will handle in second pass
                    if (row.ContainsKey("ParentClassName") && !string.IsNullOrWhiteSpace(row["ParentClassName"]))
                    {
                        continue;
                    }

                    // Check for duplicate Name in existing data
                    if (existingNames.Contains(name.ToLower()))
                    {
                        Warnings.Add($"Row {rowNumber}: Class with name '{name}' already exists. Skipping.");
                        SkippedCount++;
                        continue;
                    }

                    // Check for duplicate Name in current import batch
                    if (classesToImport.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    {
                        Warnings.Add($"Row {rowNumber}: Duplicate Name '{name}' in import file. Skipping.");
                        SkippedCount++;
                        continue;
                    }

                    // Parse year level
                    if (!int.TryParse(row["YearLevel"], out var yearLevel))
                    {
                        Errors.Add($"Row {rowNumber}: Invalid YearLevel '{row["YearLevel"]}'. Must be a number.");
                        continue;
                    }

                    // Parse student count
                    if (!int.TryParse(row["StudentCount"], out var studentCount))
                    {
                        Errors.Add($"Row {rowNumber}: Invalid StudentCount '{row["StudentCount"]}'. Must be a number.");
                        continue;
                    }

                    // Parse optional fields
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

                    var classEntity = new Class
                    {
                        Name = name,
                        YearLevel = yearLevel,
                        MaleStudents = studentCount / 2,
                        FemaleStudents = studentCount - (studentCount / 2),
                        IsActive = isActive
                    };

                    classesToImport.Add(classEntity);
                }

                // Import parent classes first
                if (classesToImport.Any())
                {
                    _context.Classes.AddRange(classesToImport);
                    await _context.SaveChangesAsync();
                    ImportedCount = classesToImport.Count;
                }

                // Second pass: import subgroups (classes with ParentClassName)
                var allClasses = await _context.Classes
                    .ToDictionaryAsync(c => c.Name.ToLower(), c => c.Id);

                var subgroupsToImport = new List<Class>();
                rowNumber = 1;

                foreach (var row in rows)
                {
                    rowNumber++;

                    // Skip if not a subgroup
                    if (!row.ContainsKey("ParentClassName") || string.IsNullOrWhiteSpace(row["ParentClassName"]))
                    {
                        continue;
                    }

                    // Validate required fields (already validated but check again for safety)
                    if (!row.ContainsKey("Name") || string.IsNullOrWhiteSpace(row["Name"]))
                    {
                        continue; // Already reported in first pass
                    }

                    var name = row["Name"].Trim();
                    var parentClassName = row["ParentClassName"].Trim();

                    // Check for duplicate Name
                    if (existingNames.Contains(name.ToLower()))
                    {
                        // Already reported in first pass
                        continue;
                    }

                    if (subgroupsToImport.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    {
                        Warnings.Add($"Row {rowNumber}: Duplicate Name '{name}' in import file. Skipping.");
                        SkippedCount++;
                        continue;
                    }

                    // Lookup ParentClassId
                    if (!allClasses.TryGetValue(parentClassName.ToLower(), out var parentClassId))
                    {
                        Errors.Add($"Row {rowNumber}: Parent class '{parentClassName}' not found. Please ensure parent classes are imported first or exist in the system.");
                        continue;
                    }

                    // Parse required fields
                    if (!int.TryParse(row["YearLevel"], out var yearLevel))
                    {
                        continue; // Already reported in first pass
                    }

                    if (!int.TryParse(row["StudentCount"], out var studentCount))
                    {
                        continue; // Already reported in first pass
                    }

                    bool isActive = true;
                    if (row.ContainsKey("IsActive") && !string.IsNullOrWhiteSpace(row["IsActive"]))
                    {
                        bool.TryParse(row["IsActive"], out isActive);
                    }

                    var classEntity = new Class
                    {
                        Name = name,
                        YearLevel = yearLevel,
                        MaleStudents = studentCount / 2,
                        FemaleStudents = studentCount - (studentCount / 2),
                        ParentClassId = parentClassId,
                        IsActive = isActive
                    };

                    subgroupsToImport.Add(classEntity);
                }

                // Import subgroups
                if (subgroupsToImport.Any())
                {
                    _context.Classes.AddRange(subgroupsToImport);
                    await _context.SaveChangesAsync();
                    ImportedCount += subgroupsToImport.Count;
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
