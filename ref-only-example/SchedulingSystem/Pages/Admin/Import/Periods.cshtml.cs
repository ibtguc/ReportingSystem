using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;
using SchedulingSystem.Services;
using System.Text;

namespace SchedulingSystem.Pages.Admin.Import
{
    public class PeriodsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PeriodsModel(ApplicationDbContext context)
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
            template.AppendLine("PeriodNumber,Name,StartTime,EndTime");
            template.AppendLine("1,Period 1,08:00,08:45");
            template.AppendLine("2,Period 2,08:50,09:35");
            template.AppendLine("3,Period 3,09:40,10:25");

            var bytes = Encoding.UTF8.GetBytes(template.ToString());
            return File(bytes, "text/csv", "periods_template.csv");
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

                // Get existing period numbers to check for duplicates
                var existingPeriodNumbers = await _context.Periods
                    .Select(p => p.PeriodNumber)
                    .ToListAsync();

                var periodsToImport = new List<Period>();
                int rowNumber = 1;

                foreach (var row in rows)
                {
                    rowNumber++;

                    // Validate required fields
                    if (!row.ContainsKey("PeriodNumber") || string.IsNullOrWhiteSpace(row["PeriodNumber"]))
                    {
                        Errors.Add($"Row {rowNumber}: PeriodNumber is required.");
                        continue;
                    }

                    if (!row.ContainsKey("Name") || string.IsNullOrWhiteSpace(row["Name"]))
                    {
                        Errors.Add($"Row {rowNumber}: Name is required.");
                        continue;
                    }

                    if (!row.ContainsKey("StartTime") || string.IsNullOrWhiteSpace(row["StartTime"]))
                    {
                        Errors.Add($"Row {rowNumber}: StartTime is required.");
                        continue;
                    }

                    if (!row.ContainsKey("EndTime") || string.IsNullOrWhiteSpace(row["EndTime"]))
                    {
                        Errors.Add($"Row {rowNumber}: EndTime is required.");
                        continue;
                    }

                    // Parse period number
                    if (!int.TryParse(row["PeriodNumber"], out var periodNumber))
                    {
                        Errors.Add($"Row {rowNumber}: Invalid PeriodNumber '{row["PeriodNumber"]}'. Must be a number.");
                        continue;
                    }

                    var name = row["Name"].Trim();

                    // Check for duplicate PeriodNumber in existing data
                    if (existingPeriodNumbers.Contains(periodNumber))
                    {
                        Warnings.Add($"Row {rowNumber}: Period with number '{periodNumber}' already exists. Skipping.");
                        SkippedCount++;
                        continue;
                    }

                    // Check for duplicate PeriodNumber in current import batch
                    if (periodsToImport.Any(p => p.PeriodNumber == periodNumber))
                    {
                        Warnings.Add($"Row {rowNumber}: Duplicate PeriodNumber '{periodNumber}' in import file. Skipping.");
                        SkippedCount++;
                        continue;
                    }

                    // Parse times
                    if (!TimeSpan.TryParse(row["StartTime"], out var startTime))
                    {
                        Errors.Add($"Row {rowNumber}: Invalid StartTime format '{row["StartTime"]}'. Use format HH:mm (e.g., 08:00).");
                        continue;
                    }

                    if (!TimeSpan.TryParse(row["EndTime"], out var endTime))
                    {
                        Errors.Add($"Row {rowNumber}: Invalid EndTime format '{row["EndTime"]}'. Use format HH:mm (e.g., 08:45).");
                        continue;
                    }

                    // Validate time logic
                    if (endTime <= startTime)
                    {
                        Errors.Add($"Row {rowNumber}: EndTime must be after StartTime.");
                        continue;
                    }

                    var period = new Period
                    {
                        PeriodNumber = periodNumber,
                        Name = name,
                        StartTime = startTime,
                        EndTime = endTime
                    };

                    periodsToImport.Add(period);
                }

                // Import valid periods
                if (periodsToImport.Any())
                {
                    _context.Periods.AddRange(periodsToImport);
                    await _context.SaveChangesAsync();
                    ImportedCount = periodsToImport.Count;
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
