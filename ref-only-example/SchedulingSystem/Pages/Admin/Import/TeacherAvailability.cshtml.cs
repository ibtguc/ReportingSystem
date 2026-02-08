using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;
using SchedulingSystem.Services;
using System.Text;

namespace SchedulingSystem.Pages.Admin.Import
{
    public class TeacherAvailabilityModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public TeacherAvailabilityModel(ApplicationDbContext context)
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
            template.AppendLine("TeacherEmail,DayOfWeek,PeriodNumber,Criticality,Reason");
            template.AppendLine("john.doe@school.edu,0,1,3,Sunday first period - Cannot teach");
            template.AppendLine("jane.smith@school.edu,1,5,1,Monday last period - Prefer not to");
            template.AppendLine("bob.jones@school.edu,3,3,2,Wednesday mid-day meeting - Should not");

            var bytes = Encoding.UTF8.GetBytes(template.ToString());
            return File(bytes, "text/csv", "teacher_availability_template.csv");
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

                var periods = await _context.Periods
                    .ToDictionaryAsync(p => p.PeriodNumber, p => p.Id);

                // Get existing availability to check for duplicates
                var existingAvailability = await _context.TeacherAvailabilities
                    .Select(ta => new { ta.TeacherId, ta.DayOfWeek, ta.PeriodId })
                    .ToListAsync();

                var availabilityToImport = new List<TeacherAvailability>();
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

                    if (!row.ContainsKey("DayOfWeek") || string.IsNullOrWhiteSpace(row["DayOfWeek"]))
                    {
                        Errors.Add($"Row {rowNumber}: DayOfWeek is required.");
                        continue;
                    }

                    if (!row.ContainsKey("PeriodNumber") || string.IsNullOrWhiteSpace(row["PeriodNumber"]))
                    {
                        Errors.Add($"Row {rowNumber}: PeriodNumber is required.");
                        continue;
                    }

                    if (!row.ContainsKey("Criticality") || string.IsNullOrWhiteSpace(row["Criticality"]))
                    {
                        Errors.Add($"Row {rowNumber}: Criticality is required.");
                        continue;
                    }

                    var teacherEmail = row["TeacherEmail"].Trim();

                    // Lookup TeacherId
                    if (!teachers.TryGetValue(teacherEmail.ToLower(), out var teacherId))
                    {
                        Errors.Add($"Row {rowNumber}: Teacher '{teacherEmail}' not found. Please ensure the teacher exists.");
                        continue;
                    }

                    // Parse DayOfWeek (0=Sunday, 1=Monday, ..., 4=Thursday)
                    if (!int.TryParse(row["DayOfWeek"], out var dayOfWeekInt) || dayOfWeekInt < 0 || dayOfWeekInt > 4)
                    {
                        Errors.Add($"Row {rowNumber}: Invalid DayOfWeek '{row["DayOfWeek"]}'. Must be 0 (Sunday) to 4 (Thursday).");
                        continue;
                    }
                    var dayOfWeek = (DayOfWeek)dayOfWeekInt;

                    // Parse PeriodNumber
                    if (!int.TryParse(row["PeriodNumber"], out var periodNumber))
                    {
                        Errors.Add($"Row {rowNumber}: Invalid PeriodNumber '{row["PeriodNumber"]}'. Must be a number.");
                        continue;
                    }

                    // Lookup PeriodId
                    if (!periods.TryGetValue(periodNumber, out var periodId))
                    {
                        Errors.Add($"Row {rowNumber}: Period with number '{periodNumber}' not found. Please ensure the period exists.");
                        continue;
                    }

                    // Parse Importance (-3 to +3)
                    if (!int.TryParse(row["Criticality"], out var importance) || importance < -3 || importance > 3)
                    {
                        Errors.Add($"Row {rowNumber}: Invalid Importance '{row["Criticality"]}'. Must be between -3 (optional) and +3 (must).");
                        continue;
                    }

                    // Check for duplicate availability in existing data
                    if (existingAvailability.Any(a => a.TeacherId == teacherId && a.DayOfWeek == dayOfWeek && a.PeriodId == periodId))
                    {
                        Warnings.Add($"Row {rowNumber}: Availability for Teacher '{teacherEmail}', Day {dayOfWeek}, Period {periodNumber} already exists. Skipping.");
                        SkippedCount++;
                        continue;
                    }

                    // Check for duplicate in current import batch
                    if (availabilityToImport.Any(a => a.TeacherId == teacherId && a.DayOfWeek == dayOfWeek && a.PeriodId == periodId))
                    {
                        Warnings.Add($"Row {rowNumber}: Duplicate availability in import file. Skipping.");
                        SkippedCount++;
                        continue;
                    }

                    // Parse optional Reason
                    string? reason = null;
                    if (row.ContainsKey("Reason") && !string.IsNullOrWhiteSpace(row["Reason"]))
                    {
                        reason = row["Reason"].Trim();
                    }

                    var availability = new TeacherAvailability
                    {
                        TeacherId = teacherId,
                        DayOfWeek = dayOfWeek,
                        PeriodId = periodId,
                        Importance = importance,
                        Reason = reason
                    };

                    availabilityToImport.Add(availability);
                }

                // Import valid availability constraints
                if (availabilityToImport.Any())
                {
                    _context.TeacherAvailabilities.AddRange(availabilityToImport);
                    await _context.SaveChangesAsync();
                    ImportedCount = availabilityToImport.Count;
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
