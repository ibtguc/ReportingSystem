using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;
using SchedulingSystem.Services;
using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Pages.Admin.Timetables;

public class ValidationChecklist
{
    public bool HasSchoolYears { get; set; }
    public bool HasSubjects { get; set; }
    public bool HasTeachers { get; set; }
    public bool HasClasses { get; set; }
    public bool HasRooms { get; set; }
    public bool HasPeriods { get; set; }
    public bool HasLessons { get; set; }
    public bool HasActiveRooms { get; set; }
    public bool HasNonBreakPeriods { get; set; }

    public bool IsComplete => HasSchoolYears && HasSubjects && HasTeachers &&
                              HasClasses && HasRooms && HasPeriods && HasLessons &&
                              HasActiveRooms && HasNonBreakPeriods;

    public List<string> MissingItems
    {
        get
        {
            var missing = new List<string>();
            if (!HasSchoolYears) missing.Add("School Years");
            if (!HasSubjects) missing.Add("Subjects");
            if (!HasTeachers) missing.Add("Teachers");
            if (!HasClasses) missing.Add("Classes");
            if (!HasRooms) missing.Add("Rooms");
            if (!HasPeriods) missing.Add("Periods");
            if (!HasLessons) missing.Add("Active Lessons");
            if (!HasActiveRooms) missing.Add("Active Rooms");
            if (!HasNonBreakPeriods) missing.Add("Non-break Periods");
            return missing;
        }
    }
}

public class GenerateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly SchedulingService _schedulingService;
    private readonly SchedulingServiceEnhanced _schedulingServiceEnhanced;
    private readonly SchedulingServiceSimulatedAnnealing _schedulingServiceSA;
    private readonly ILogger<GenerateModel> _logger;

    public GenerateModel(
        ApplicationDbContext context,
        SchedulingService schedulingService,
        SchedulingServiceEnhanced schedulingServiceEnhanced,
        SchedulingServiceSimulatedAnnealing schedulingServiceSA,
        ILogger<GenerateModel> logger)
    {
        _context = context;
        _schedulingService = schedulingService;
        _schedulingServiceEnhanced = schedulingServiceEnhanced;
        _schedulingServiceSA = schedulingServiceSA;
        _logger = logger;
    }

    [BindProperty]
    [Required(ErrorMessage = "Timetable name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string TimetableName { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Please select a school year")]
    public int SchoolYearId { get; set; }

    [BindProperty]
    [Display(Name = "Algorithm")]
    public string Algorithm { get; set; } = "Enhanced";

    [BindProperty]
    [Display(Name = "Optimization Level")]
    public string OptimizationLevel { get; set; } = "Default";

    public SelectList SchoolYears { get; set; } = null!;
    public List<Timetable> ExistingTimetables { get; set; } = new();
    public SchedulingResult? Result { get; set; }

    // Statistics
    public int TotalLessons { get; set; }
    public int TotalInstances { get; set; }
    public int TotalPeriods { get; set; }
    public int TotalRooms { get; set; }
    public int TotalTeachers { get; set; }
    public int TotalClasses { get; set; }
    public int TotalSubjects { get; set; }

    // Validation Checklist
    public ValidationChecklist Checklist { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadDataAsync();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            _logger.LogInformation("Starting timetable generation: {Name} for School Year {Id} using {Algorithm}",
                TimetableName, SchoolYearId, Algorithm);

            // Start timing
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Generate the timetable using selected algorithm
            if (Algorithm == "Enhanced")
            {
                var weights = OptimizationLevel switch
                {
                    "Aggressive" => SoftConstraintWeights.Aggressive,
                    "Relaxed" => SoftConstraintWeights.Relaxed,
                    _ => SoftConstraintWeights.Default
                };

                Result = await _schedulingServiceEnhanced.GenerateTimetableAsync(SchoolYearId, TimetableName, weights);
            }
            else if (Algorithm == "SimulatedAnnealing")
            {
                var saConfig = OptimizationLevel switch
                {
                    "Aggressive" => SimulatedAnnealingConfig.Thorough,
                    "Relaxed" => SimulatedAnnealingConfig.Fast,
                    _ => SimulatedAnnealingConfig.Balanced
                };

                Result = await _schedulingServiceSA.GenerateTimetableAsync(SchoolYearId, TimetableName, saConfig);
            }
            else
            {
                Result = await _schedulingService.GenerateTimetableAsync(SchoolYearId, TimetableName);
            }

            // Stop timing
            stopwatch.Stop();

            if (Result.Success)
            {
                _logger.LogInformation("Timetable generated successfully: {Id} in {Duration}ms", Result.TimetableId, stopwatch.ElapsedMilliseconds);

                // Save generation duration to the timetable
                var timetable = await _context.Timetables.FindAsync(Result.TimetableId);
                if (timetable != null)
                {
                    timetable.GenerationDurationMs = stopwatch.ElapsedMilliseconds;
                    await _context.SaveChangesAsync();
                }

                // Reload existing timetables to show the newly created one
                ExistingTimetables = await _context.Timetables
                    .Include(t => t.SchoolYear)
                    .OrderByDescending(t => t.CreatedDate)
                    .Take(10)
                    .ToListAsync();
            }
            else
            {
                _logger.LogWarning("Timetable generation failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during timetable generation");
            ModelState.AddModelError(string.Empty, "An error occurred during generation: " + ex.Message);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostPublishAsync(int id)
    {
        try
        {
            var timetable = await _context.Timetables.FindAsync(id);

            if (timetable == null)
            {
                TempData["ErrorMessage"] = "Timetable not found.";
                return RedirectToPage();
            }

            if (timetable.Status != TimetableStatus.Draft)
            {
                TempData["ErrorMessage"] = "Only draft timetables can be published.";
                return RedirectToPage();
            }

            // Update status to Published
            timetable.Status = TimetableStatus.Published;
            timetable.PublishedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Timetable published: {Id} - {Name}", timetable.Id, timetable.Name);
            TempData["SuccessMessage"] = $"Timetable '{timetable.Name}' has been published successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing timetable {Id}", id);
            TempData["ErrorMessage"] = $"Error publishing timetable: {ex.Message}";
        }

        return RedirectToPage();
    }

    private async Task LoadDataAsync()
    {
        // Load school years for dropdown
        var schoolYears = await _context.SchoolYears
            .OrderByDescending(sy => sy.StartDate)
            .ToListAsync();

        SchoolYears = new SelectList(schoolYears, "Id", "Name");

        // Set default school year if only one exists
        if (schoolYears.Count == 1 && SchoolYearId == 0)
        {
            SchoolYearId = schoolYears[0].Id;
        }

        // Load existing timetables
        ExistingTimetables = await _context.Timetables
            .Include(t => t.SchoolYear)
            .OrderByDescending(t => t.CreatedDate)
            .Take(10)
            .ToListAsync();

        // Calculate statistics
        var lessons = await _context.Lessons.Where(l => l.IsActive).ToListAsync();
        TotalLessons = lessons.Count;
        TotalInstances = lessons.Sum(l => l.FrequencyPerWeek);
        TotalPeriods = await _context.Periods.Where(p => !p.IsBreak).CountAsync();
        TotalRooms = await _context.Rooms.Where(r => r.IsActive).CountAsync();
        TotalTeachers = await _context.Teachers.CountAsync();
        TotalClasses = await _context.Classes.CountAsync();
        TotalSubjects = await _context.Subjects.CountAsync();

        // Populate validation checklist
        Checklist = new ValidationChecklist
        {
            HasSchoolYears = schoolYears.Any(),
            HasSubjects = await _context.Subjects.AnyAsync(),
            HasTeachers = await _context.Teachers.AnyAsync(),
            HasClasses = await _context.Classes.AnyAsync(),
            HasRooms = await _context.Rooms.AnyAsync(),
            HasPeriods = await _context.Periods.AnyAsync(),
            HasLessons = lessons.Any(),
            HasActiveRooms = await _context.Rooms.AnyAsync(r => r.IsActive),
            HasNonBreakPeriods = await _context.Periods.AnyAsync(p => !p.IsBreak)
        };

        // Generate default name
        if (string.IsNullOrEmpty(TimetableName))
        {
            var schoolYear = schoolYears.FirstOrDefault(sy => sy.Id == SchoolYearId);
            if (schoolYear != null)
            {
                TimetableName = $"{schoolYear.Name} Timetable - {DateTime.Now:yyyy-MM-dd}";
            }
        }
    }
}
