using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Services;

namespace SchedulingSystem.Pages.Admin.Import
{
    public class UntisModel : PageModel
    {
        private readonly UntisImportService _importService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UntisModel> _logger;

        public UntisModel(
            UntisImportService importService,
            ApplicationDbContext context,
            ILogger<UntisModel> logger)
        {
            _importService = importService;
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public IFormFile? DepartmentsFile { get; set; }

        [BindProperty]
        public IFormFile? TeachersFile { get; set; }

        [BindProperty]
        public IFormFile? ClassesFile { get; set; }

        [BindProperty]
        public IFormFile? SubjectsFile { get; set; }

        [BindProperty]
        public IFormFile? RoomsFile { get; set; }

        [BindProperty]
        public IFormFile? LessonsFile { get; set; }

        [BindProperty]
        public IFormFile? StudentsFile { get; set; }

        [BindProperty]
        public IFormFile? TimetableFile { get; set; }

        [BindProperty]
        public int? TimetableId { get; set; }

        [BindProperty]
        public int? CombinedImportTimetableId { get; set; }

        [BindProperty]
        public IFormFile? AvailabilityFile { get; set; }

        [BindProperty]
        public IFormFile? PeriodsFile { get; set; }

        [BindProperty]
        public IFormFile? BreakSupervisionFile { get; set; }

        [BindProperty]
        public int? BreakSupervisionTimetableId { get; set; }

        [BindProperty]
        public List<IFormFile>? AllFiles { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public bool IsSuccess { get; set; }

        public Dictionary<string, UntisImportService.ImportResult> ImportResults { get; set; } = new();

        public SelectList TimetableList { get; set; } = new SelectList(new List<object>());
        public SelectList SchoolYearList { get; set; } = new SelectList(new List<object>());

        public async Task OnGetAsync()
        {
            await LoadTimetables();
            await LoadSchoolYears();
        }

        private async Task LoadTimetables()
        {
            var timetables = await _context.Timetables
                .Include(t => t.SchoolYear)
                .Include(t => t.Term)
                .OrderByDescending(t => t.CreatedDate)
                .Select(t => new
                {
                    t.Id,
                    // Show the timetable name first, then school year and term for context
                    DisplayName = t.Term != null
                        ? $"{t.Name} ({t.SchoolYear!.Name} - {t.Term.Name}, {t.Status})"
                        : $"{t.Name} ({t.SchoolYear!.Name}, {t.Status})"
                })
                .ToListAsync();

            TimetableList = new SelectList(timetables, "Id", "DisplayName");
        }

        private async Task LoadSchoolYears()
        {
            var schoolYears = await _context.SchoolYears
                .OrderByDescending(sy => sy.StartDate)
                .ToListAsync();

            SchoolYearList = new SelectList(schoolYears, "Id", "Name");
        }

        public async Task<IActionResult> OnPostImportDepartmentsAsync()
        {
            if (DepartmentsFile == null)
            {
                StatusMessage = "Please select a file to upload";
                IsSuccess = false;
                await LoadTimetables();
                await LoadSchoolYears();
                return Page();
            }

            try
            {
                using var stream = DepartmentsFile.OpenReadStream();
                var result = await _importService.ImportDepartmentsAsync(stream);
                ImportResults["Departments"] = result;

                StatusMessage = result.Message;
                IsSuccess = result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing departments");
                StatusMessage = $"Error: {ex.Message}";
                IsSuccess = false;
            }

            await LoadTimetables();
            await LoadSchoolYears();
            return Page();
        }

        public async Task<IActionResult> OnPostImportTeachersAsync()
        {
            if (TeachersFile == null)
            {
                StatusMessage = "Please select a file to upload";
                IsSuccess = false;
                await LoadTimetables();
                await LoadSchoolYears();
                return Page();
            }

            try
            {
                using var stream = TeachersFile.OpenReadStream();
                var result = await _importService.ImportTeachersAsync(stream);
                ImportResults["Teachers"] = result;

                StatusMessage = result.Message;
                IsSuccess = result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing teachers");
                StatusMessage = $"Error: {ex.Message}";
                IsSuccess = false;
            }

            await LoadTimetables();
            await LoadSchoolYears();
            return Page();
        }

        public async Task<IActionResult> OnPostImportClassesAsync()
        {
            if (ClassesFile == null)
            {
                StatusMessage = "Please select a file to upload";
                IsSuccess = false;
                await LoadTimetables();
                await LoadSchoolYears();
                return Page();
            }

            try
            {
                using var stream = ClassesFile.OpenReadStream();
                var result = await _importService.ImportClassesAsync(stream);
                ImportResults["Classes"] = result;

                StatusMessage = result.Message;
                IsSuccess = result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing classes");
                StatusMessage = $"Error: {ex.Message}";
                IsSuccess = false;
            }

            await LoadTimetables();
            await LoadSchoolYears();
            return Page();
        }

        public async Task<IActionResult> OnPostImportSubjectsAsync()
        {
            if (SubjectsFile == null)
            {
                StatusMessage = "Please select a file to upload";
                IsSuccess = false;
                await LoadTimetables();
                await LoadSchoolYears();
                return Page();
            }

            try
            {
                using var stream = SubjectsFile.OpenReadStream();
                var result = await _importService.ImportSubjectsAsync(stream);
                ImportResults["Subjects"] = result;

                StatusMessage = result.Message;
                IsSuccess = result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing subjects");
                StatusMessage = $"Error: {ex.Message}";
                IsSuccess = false;
            }

            await LoadTimetables();
            await LoadSchoolYears();
            return Page();
        }

        public async Task<IActionResult> OnPostImportRoomsAsync()
        {
            if (RoomsFile == null)
            {
                StatusMessage = "Please select a file to upload";
                IsSuccess = false;
                await LoadTimetables();
                await LoadSchoolYears();
                return Page();
            }

            try
            {
                using var stream = RoomsFile.OpenReadStream();
                var result = await _importService.ImportRoomsAsync(stream);
                ImportResults["Rooms"] = result;

                StatusMessage = result.Message;
                IsSuccess = result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing rooms");
                StatusMessage = $"Error: {ex.Message}";
                IsSuccess = false;
            }

            await LoadTimetables();
            await LoadSchoolYears();
            return Page();
        }

        public async Task<IActionResult> OnPostImportLessonsAsync()
        {
            if (LessonsFile == null)
            {
                StatusMessage = "Please select a file to upload";
                IsSuccess = false;
                await LoadTimetables();
                await LoadSchoolYears();
                return Page();
            }

            try
            {
                // Delete all existing lessons (CASCADE will handle related data)
                _logger.LogWarning("Deleting all existing lessons before import");
                var lessonsCount = await _context.Lessons.CountAsync();
                _context.Lessons.RemoveRange(_context.Lessons);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} existing lessons", lessonsCount);

                using var stream = LessonsFile.OpenReadStream();
                var result = await _importService.ImportLessonsAsync(stream);
                ImportResults["Lessons"] = result;

                StatusMessage = $"Deleted {lessonsCount} existing lessons. {result.Message}";
                IsSuccess = result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing lessons");
                StatusMessage = $"Error: {ex.Message}";
                IsSuccess = false;
            }

            await LoadTimetables();
            await LoadSchoolYears();
            return Page();
        }

        public async Task<IActionResult> OnPostImportStudentsAsync()
        {
            if (StudentsFile == null)
            {
                StatusMessage = "Please select a file to upload";
                IsSuccess = false;
                await LoadTimetables();
                await LoadSchoolYears();
                return Page();
            }

            try
            {
                using var stream = StudentsFile.OpenReadStream();
                var result = await _importService.ImportStudentsAsync(stream);
                ImportResults["Students"] = result;

                StatusMessage = result.Message;
                IsSuccess = result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing students");
                StatusMessage = $"Error: {ex.Message}";
                IsSuccess = false;
            }

            await LoadTimetables();
            await LoadSchoolYears();
            return Page();
        }

        public async Task<IActionResult> OnPostImportTimetableAsync()
        {
            if (TimetableFile == null)
            {
                StatusMessage = "Please select a file to upload";
                IsSuccess = false;
                await LoadTimetables();
                await LoadSchoolYears();
                return Page();
            }

            if (!TimetableId.HasValue)
            {
                StatusMessage = "Please select a timetable to import into";
                IsSuccess = false;
                await LoadTimetables();
                await LoadSchoolYears();
                return Page();
            }

            try
            {
                using var stream = TimetableFile.OpenReadStream();
                var result = await _importService.ImportTimetableAsync(stream, TimetableId.Value);
                ImportResults["Timetable"] = result;

                StatusMessage = result.Message;
                IsSuccess = result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing timetable");
                StatusMessage = $"Error: {ex.Message}";
                IsSuccess = false;
            }

            await LoadTimetables();
            await LoadSchoolYears();
            return Page();
        }

        public async Task<IActionResult> OnPostImportAllAsync()
        {
            if (AllFiles == null || !AllFiles.Any())
            {
                StatusMessage = "Please select files to upload";
                IsSuccess = false;
                await LoadTimetables();
                await LoadSchoolYears();
                return Page();
            }

            try
            {
                var fileDict = AllFiles.ToDictionary(
                    f => f.FileName.ToUpper(),
                    f => f
                );

                // Import in the correct order
                var importOrder = new[]
                {
                    ("GPU007.TXT", "Departments", (Func<Stream, Task<UntisImportService.ImportResult>>)_importService.ImportDepartmentsAsync),
                    ("GPU004.TXT", "Teachers", (Func<Stream, Task<UntisImportService.ImportResult>>)_importService.ImportTeachersAsync),
                    ("GPU003.TXT", "Classes", (Func<Stream, Task<UntisImportService.ImportResult>>)_importService.ImportClassesAsync),
                    ("GPU010.TXT", "Students", (Func<Stream, Task<UntisImportService.ImportResult>>)_importService.ImportStudentsAsync),
                    ("GPU006.TXT", "Subjects", (Func<Stream, Task<UntisImportService.ImportResult>>)_importService.ImportSubjectsAsync),
                    ("GPU005.TXT", "Rooms", (Func<Stream, Task<UntisImportService.ImportResult>>)_importService.ImportRoomsAsync),
                    ("GPU002.TXT", "Lessons", (Func<Stream, Task<UntisImportService.ImportResult>>)_importService.ImportLessonsAsync),
                    ("GPU016.TXT", "Availability", (Func<Stream, Task<UntisImportService.ImportResult>>)_importService.ImportAvailabilityAsync)
                };

                bool allSuccess = true;
                var results = new List<string>();

                foreach (var (fileName, entityName, importFunc) in importOrder)
                {
                    var file = fileDict.FirstOrDefault(f => f.Key.Contains(fileName)).Value;
                    if (file != null)
                    {
                        using var stream = file.OpenReadStream();
                        var result = await importFunc(stream);
                        ImportResults[entityName] = result;

                        if (!result.Success)
                            allSuccess = false;

                        results.Add($"{entityName}: {result.RecordsImported} imported, {result.RecordsUpdated} updated");
                    }
                }

                StatusMessage = allSuccess
                    ? $"Import completed successfully. {string.Join("; ", results)}"
                    : $"Import completed with errors. Check the results below.";
                IsSuccess = allSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during batch import");
                StatusMessage = $"Error: {ex.Message}";
                IsSuccess = false;
            }

            await LoadTimetables();
            await LoadSchoolYears();
            return Page();
        }

        // COMMENTED OUT - Will be rebuilt step by step
        /*
        public async Task<IActionResult> OnPostImportScheduleWithCoTeachingAsync()
        {
            if (LessonsFile == null || TimetableFile == null)
            {
                StatusMessage = "Please select both GPU002.TXT (Lessons) and GPU001.TXT (Schedule) files";
                IsSuccess = false;
                await LoadTimetables();
                await LoadSchoolYears();
                return Page();
            }

            if (!CombinedImportTimetableId.HasValue)
            {
                StatusMessage = "Please select a timetable to import into";
                IsSuccess = false;
                await LoadTimetables();
                await LoadSchoolYears();
                return Page();
            }

            try
            {
                using var lessonsStream = LessonsFile.OpenReadStream();
                using var scheduleStream = TimetableFile.OpenReadStream();

                var result = await _importService.ImportScheduleWithCoTeachingDetectionAsync(
                    lessonsStream,
                    scheduleStream,
                    CombinedImportTimetableId.Value);

                ImportResults["Schedule with Co-Teaching"] = result;

                if (result.Success)
                {
                    StatusMessage = result.Message ??
                                  $"Import completed successfully! {result.RecordsImported} lessons imported, {result.RecordsSkipped} skipped.";
                    IsSuccess = true;
                }
                else
                {
                    StatusMessage = $"Import failed: {string.Join(", ", result.Errors)}";
                    IsSuccess = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing schedule with co-teaching detection");
                StatusMessage = $"Error: {ex.Message}";
                IsSuccess = false;
            }

            await LoadTimetables();
            await LoadSchoolYears();
            return Page();
        }
        */

        /*
        public async Task<IActionResult> OnPostImportScheduleWithCoTeachingAsync(bool enableCoTeachingDetection = true)
        {
            if (LessonsFile == null || TimetableFile == null)
            {
                StatusMessage = "Please select both GPU002.TXT (Lessons) and GPU001.TXT (Schedule) files";
                IsSuccess = false;
                await LoadTimetables();
                await LoadSchoolYears();
                return Page();
            }

            if (!CombinedImportTimetableId.HasValue)
            {
                StatusMessage = "Please select a timetable to import into";
                IsSuccess = false;
                await LoadTimetables();
                await LoadSchoolYears();
                return Page();
            }

            try
            {
                // Delete all existing lessons (CASCADE will handle related data including scheduled lessons)
                _logger.LogWarning("Deleting all existing lessons before schedule import");
                var lessonsCount = await _context.Lessons.CountAsync();
                _context.Lessons.RemoveRange(_context.Lessons);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} existing lessons", lessonsCount);

                using var lessonsStream = LessonsFile.OpenReadStream();
                using var scheduleStream = TimetableFile.OpenReadStream();

                var result = await _importService.ImportScheduleWithConflictDetectionAsync(
                    lessonsStream,
                    scheduleStream,
                    CombinedImportTimetableId.Value,
                    enableCoTeachingDetection);

                ImportResults["Schedule Import"] = result;

                if (result.Success)
                {
                    StatusMessage = $"Deleted {lessonsCount} existing lessons. " + (result.Message ??
                                  $"Import completed successfully! {result.RecordsImported} lessons imported, {result.RecordsSkipped} skipped.");
                    IsSuccess = true;
                }
                else
                {
                    StatusMessage = $"Import failed: {string.Join(", ", result.Errors)}";
                    IsSuccess = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing schedule with co-teaching detection");
                StatusMessage = $"Error: {ex.Message}";
                IsSuccess = false;
            }

            await LoadTimetables();
            await LoadSchoolYears();
            return Page();
        }
        */

        public async Task<IActionResult> OnPostCreateTimetableAsync(string newTimetableName, int newTimetableSchoolYearId, string? newTimetableNotes)
        {
            if (string.IsNullOrWhiteSpace(newTimetableName))
            {
                StatusMessage = "Timetable name is required.";
                IsSuccess = false;
                await LoadTimetables();
                await LoadSchoolYears();
                return Page();
            }

            var schoolYear = await _context.SchoolYears.FindAsync(newTimetableSchoolYearId);
            if (schoolYear == null)
            {
                StatusMessage = "Invalid school year selected.";
                IsSuccess = false;
                await LoadTimetables();
                await LoadSchoolYears();
                return Page();
            }

            try
            {
                var timetable = new Models.Timetable
                {
                    Name = newTimetableName,
                    SchoolYearId = newTimetableSchoolYearId,
                    CreatedDate = DateTime.Now,
                    Status = Models.TimetableStatus.Draft,
                    Notes = newTimetableNotes
                };

                _context.Timetables.Add(timetable);
                await _context.SaveChangesAsync();

                StatusMessage = $"Timetable '{newTimetableName}' created successfully! You can now import GPU001.TXT into this timetable.";
                IsSuccess = true;

                _logger.LogInformation("Created new timetable: {TimetableName} (ID: {TimetableId})", timetable.Name, timetable.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating timetable");
                StatusMessage = $"Error creating timetable: {ex.Message}";
                IsSuccess = false;
            }

            await LoadTimetables();
            await LoadSchoolYears();
            return Page();
        }

        public async Task<IActionResult> OnPostImportAvailabilityAsync()
        {
            if (AvailabilityFile == null)
            {
                StatusMessage = "Please select a file to upload";
                IsSuccess = false;
                await LoadTimetables();
                await LoadSchoolYears();
                return Page();
            }

            try
            {
                var result = await _importService.ImportAvailabilityAsync(AvailabilityFile);
                ImportResults["Availability (GPU016.TXT)"] = result;

                if (result.Success)
                {
                    StatusMessage = $"Availability import completed: {result.RecordsImported} records imported, {result.RecordsSkipped} skipped";
                    IsSuccess = true;
                    _logger.LogInformation("Availability import completed: {RecordsImported} imported, {RecordsSkipped} skipped",
                        result.RecordsImported, result.RecordsSkipped);
                }
                else
                {
                    StatusMessage = $"Availability import failed: {string.Join(", ", result.Errors)}";
                    IsSuccess = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing availability data");
                StatusMessage = $"Error importing availability: {ex.Message}";
                IsSuccess = false;
            }

            await LoadTimetables();
            await LoadSchoolYears();
            return Page();
        }

        public async Task<IActionResult> OnPostCleanupDatabaseAsync()
        {
            try
            {
                _logger.LogWarning("Starting database cleanup - removing all UNTIS data and absences/substitutions");

                // Delete in reverse order of dependencies to avoid foreign key issues

                // 1. Delete Substitutions (depends on Absences and ScheduledLessons)
                var substitutionsCount = await _context.Substitutions.CountAsync();
                _context.Substitutions.RemoveRange(_context.Substitutions);
                _logger.LogInformation("Removing {Count} Substitutions", substitutionsCount);

                // 2. Delete Absences (depends on Teachers)
                var absencesCount = await _context.Absences.CountAsync();
                _context.Absences.RemoveRange(_context.Absences);
                _logger.LogInformation("Removing {Count} Absences", absencesCount);

                // 3. Delete ScheduledLessons (depends on Timetables, Lessons, Periods, Rooms)
                var scheduledLessonsCount = await _context.ScheduledLessons.CountAsync();
                _context.ScheduledLessons.RemoveRange(_context.ScheduledLessons);
                _logger.LogInformation("Removing {Count} ScheduledLessons", scheduledLessonsCount);

                // 4. Delete Timetables
                var timetablesCount = await _context.Timetables.CountAsync();
                _context.Timetables.RemoveRange(_context.Timetables);
                _logger.LogInformation("Removing {Count} Timetables", timetablesCount);

                // 5. Delete TeacherAvailability (depends on Teachers)
                var teacherAvailabilityCount = await _context.TeacherAvailabilities.CountAsync();
                _context.TeacherAvailabilities.RemoveRange(_context.TeacherAvailabilities);
                _logger.LogInformation("Removing {Count} TeacherAvailabilities", teacherAvailabilityCount);

                // 6. Delete TeacherSubjects (depends on Teachers and Subjects)
                var teacherSubjectsCount = await _context.TeacherSubjects.CountAsync();
                _context.TeacherSubjects.RemoveRange(_context.TeacherSubjects);
                _logger.LogInformation("Removing {Count} TeacherSubjects", teacherSubjectsCount);

                // 7. Delete Lessons (depends on Classes, Teachers, Subjects)
                var lessonsCount = await _context.Lessons.CountAsync();
                _context.Lessons.RemoveRange(_context.Lessons);
                _logger.LogInformation("Removing {Count} Lessons", lessonsCount);

                // 8. Delete Students (depends on Classes)
                var studentsCount = await _context.Students.CountAsync();
                _context.Students.RemoveRange(_context.Students);
                _logger.LogInformation("Removing {Count} Students", studentsCount);

                // 9. Delete Rooms
                var roomsCount = await _context.Rooms.CountAsync();
                _context.Rooms.RemoveRange(_context.Rooms);
                _logger.LogInformation("Removing {Count} Rooms", roomsCount);

                // 10. Delete Subjects
                var subjectsCount = await _context.Subjects.CountAsync();
                _context.Subjects.RemoveRange(_context.Subjects);
                _logger.LogInformation("Removing {Count} Subjects", subjectsCount);

                // 11. Delete Classes
                var classesCount = await _context.Classes.CountAsync();
                _context.Classes.RemoveRange(_context.Classes);
                _logger.LogInformation("Removing {Count} Classes", classesCount);

                // 12. Delete Teachers (depends on Departments)
                var teachersCount = await _context.Teachers.CountAsync();
                _context.Teachers.RemoveRange(_context.Teachers);
                _logger.LogInformation("Removing {Count} Teachers", teachersCount);

                // 13. Delete Departments
                var departmentsCount = await _context.Departments.CountAsync();
                _context.Departments.RemoveRange(_context.Departments);
                _logger.LogInformation("Removing {Count} Departments", departmentsCount);

                // Save all changes
                await _context.SaveChangesAsync();

                var totalDeleted = substitutionsCount + absencesCount + scheduledLessonsCount + timetablesCount +
                                  teacherAvailabilityCount + teacherSubjectsCount + lessonsCount + studentsCount +
                                  roomsCount + subjectsCount + classesCount + teachersCount + departmentsCount;

                StatusMessage = $"Database cleanup completed successfully! Removed {totalDeleted} records total: " +
                               $"{departmentsCount} Departments, {teachersCount} Teachers, {classesCount} Classes, " +
                               $"{studentsCount} Students, {subjectsCount} Subjects, {roomsCount} Rooms, " +
                               $"{lessonsCount} Lessons, {teacherSubjectsCount} TeacherSubjects, " +
                               $"{teacherAvailabilityCount} TeacherAvailabilities, {timetablesCount} Timetables, " +
                               $"{scheduledLessonsCount} ScheduledLessons, {absencesCount} Absences, {substitutionsCount} Substitutions";
                IsSuccess = true;

                _logger.LogWarning("Database cleanup completed - removed {TotalCount} records", totalDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database cleanup");
                StatusMessage = $"Error during cleanup: {ex.Message}";
                IsSuccess = false;
            }

            await LoadTimetables();
            await LoadSchoolYears();
            return Page();
        }

        public async Task<IActionResult> OnPostImportPeriodsAsync()
        {
            if (PeriodsFile == null)
            {
                StatusMessage = "Please select a file to upload";
                IsSuccess = false;
                await LoadTimetables();
                await LoadSchoolYears();
                return Page();
            }

            try
            {
                using var stream = PeriodsFile.OpenReadStream();
                var rows = CsvService.ParseCsv(stream);

                if (rows.Count == 0)
                {
                    StatusMessage = "The CSV file is empty or contains no valid data rows.";
                    IsSuccess = false;
                    await LoadTimetables();
                    await LoadSchoolYears();
                    return Page();
                }

                // Get existing period numbers to check for duplicates
                var existingPeriodNumbers = await _context.Periods
                    .Select(p => p.PeriodNumber)
                    .ToListAsync();

                var periodsToImport = new List<Models.Period>();
                var errors = new List<string>();
                var warnings = new List<string>();
                int rowNumber = 1;
                int skippedCount = 0;

                foreach (var row in rows)
                {
                    rowNumber++;

                    var csvLineData = string.Join(";", row.Values);

                    // Validate required fields
                    if (!row.ContainsKey("PeriodNumber") || string.IsNullOrWhiteSpace(row["PeriodNumber"]))
                    {
                        errors.Add($"Row {rowNumber}: PeriodNumber is required.|LINE:{csvLineData}");
                        continue;
                    }

                    if (!row.ContainsKey("Name") || string.IsNullOrWhiteSpace(row["Name"]))
                    {
                        errors.Add($"Row {rowNumber}: Name is required.|LINE:{csvLineData}");
                        continue;
                    }

                    if (!row.ContainsKey("StartTime") || string.IsNullOrWhiteSpace(row["StartTime"]))
                    {
                        errors.Add($"Row {rowNumber}: StartTime is required.|LINE:{csvLineData}");
                        continue;
                    }

                    if (!row.ContainsKey("EndTime") || string.IsNullOrWhiteSpace(row["EndTime"]))
                    {
                        errors.Add($"Row {rowNumber}: EndTime is required.|LINE:{csvLineData}");
                        continue;
                    }

                    // Parse period number
                    if (!int.TryParse(row["PeriodNumber"], out var periodNumber))
                    {
                        errors.Add($"Row {rowNumber}: Invalid PeriodNumber '{row["PeriodNumber"]}'. Must be a number.|LINE:{csvLineData}");
                        continue;
                    }

                    var name = row["Name"].Trim();

                    // Check for duplicate PeriodNumber in existing data
                    if (existingPeriodNumbers.Contains(periodNumber))
                    {
                        warnings.Add($"Row {rowNumber}: Period with number '{periodNumber}' already exists. Skipping.|LINE:{csvLineData}");
                        skippedCount++;
                        continue;
                    }

                    // Check for duplicate PeriodNumber in current import batch
                    if (periodsToImport.Any(p => p.PeriodNumber == periodNumber))
                    {
                        warnings.Add($"Row {rowNumber}: Duplicate PeriodNumber '{periodNumber}' in import file. Skipping.|LINE:{csvLineData}");
                        skippedCount++;
                        continue;
                    }

                    // Parse times
                    if (!TimeSpan.TryParse(row["StartTime"], out var startTime))
                    {
                        errors.Add($"Row {rowNumber}: Invalid StartTime format '{row["StartTime"]}'. Use format HH:mm (e.g., 08:00).|LINE:{csvLineData}");
                        continue;
                    }

                    if (!TimeSpan.TryParse(row["EndTime"], out var endTime))
                    {
                        errors.Add($"Row {rowNumber}: Invalid EndTime format '{row["EndTime"]}'. Use format HH:mm (e.g., 08:45).|LINE:{csvLineData}");
                        continue;
                    }

                    // Validate time logic
                    if (endTime <= startTime)
                    {
                        errors.Add($"Row {rowNumber}: EndTime must be after StartTime.|LINE:{csvLineData}");
                        continue;
                    }

                    var period = new Models.Period
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

                    StatusMessage = $"Periods import completed: {periodsToImport.Count} imported, {skippedCount} skipped. " +
                                  (errors.Any() ? $"{errors.Count} errors encountered." : "");
                    IsSuccess = !errors.Any();

                    // Add detailed error/warning results
                    var periodsResult = new UntisImportService.ImportResult
                    {
                        Success = !errors.Any(),
                        RecordsProcessed = rows.Count,
                        RecordsImported = periodsToImport.Count,
                        RecordsSkipped = skippedCount,
                        Errors = errors,
                        Warnings = warnings
                    };
                    ImportResults["Periods"] = periodsResult;
                }
                else
                {
                    StatusMessage = errors.Any()
                        ? $"Periods import failed: {errors.Count} errors encountered."
                        : "No valid periods to import.";
                    IsSuccess = false;

                    var periodsResult = new UntisImportService.ImportResult
                    {
                        Success = false,
                        RecordsProcessed = rows.Count,
                        RecordsImported = 0,
                        RecordsSkipped = skippedCount,
                        Errors = errors,
                        Warnings = warnings
                    };
                    ImportResults["Periods"] = periodsResult;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing periods");
                StatusMessage = $"Error importing periods: {ex.Message}";
                IsSuccess = false;
            }

            await LoadTimetables();
            await LoadSchoolYears();
            return Page();
        }

        public async Task<IActionResult> OnPostImportBreakSupervisionAsync()
        {
            if (BreakSupervisionFile == null)
            {
                StatusMessage = "Please select a file to upload";
                IsSuccess = false;
                await LoadTimetables();
                await LoadSchoolYears();
                return Page();
            }

            if (!BreakSupervisionTimetableId.HasValue)
            {
                StatusMessage = "Please select a timetable to import break supervision duties into";
                IsSuccess = false;
                await LoadTimetables();
                await LoadSchoolYears();
                return Page();
            }

            try
            {
                using var stream = BreakSupervisionFile.OpenReadStream();
                var result = await _importService.ImportBreakSupervisionAsync(stream, BreakSupervisionTimetableId.Value);
                ImportResults["Break Supervision"] = result;

                StatusMessage = result.Message;
                IsSuccess = result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing break supervision data");
                StatusMessage = $"Error: {ex.Message}";
                IsSuccess = false;
            }

            await LoadTimetables();
            await LoadSchoolYears();
            return Page();
        }

        public async Task<IActionResult> OnPostClearBreakSupervisionAsync()
        {
            if (!BreakSupervisionTimetableId.HasValue)
            {
                StatusMessage = "Please select a timetable to clear break supervision duties from";
                IsSuccess = false;
                await LoadTimetables();
                await LoadSchoolYears();
                return Page();
            }

            try
            {
                var result = await _importService.ClearBreakSupervisionDataAsync(BreakSupervisionTimetableId.Value);
                ImportResults["Clear Break Supervision"] = result;

                StatusMessage = $"Cleared {result.RecordsProcessed} break supervision duties from selected timetable";
                IsSuccess = result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing break supervision data");
                StatusMessage = $"Error: {ex.Message}";
                IsSuccess = false;
            }

            await LoadTimetables();
            await LoadSchoolYears();
            return Page();
        }

        public IActionResult OnGetDownloadPeriodsTemplate()
        {
            var template = new System.Text.StringBuilder();
            template.AppendLine("PeriodNumber,Name,StartTime,EndTime");
            template.AppendLine("1,Period 1,08:00,08:45");
            template.AppendLine("2,Period 2,08:50,09:35");
            template.AppendLine("3,Period 3,09:40,10:25");

            var bytes = System.Text.Encoding.UTF8.GetBytes(template.ToString());
            return File(bytes, "text/csv", "periods_template.csv");
        }

        public IActionResult OnGetDownloadTemplate(string type)
        {
            byte[] fileContent;
            string fileName;

            switch (type.ToLower())
            {
                case "departments":
                    fileContent = ImportTemplateService.GenerateDepartmentsTemplate();
                    fileName = "GPU007_Departments_Template.csv";
                    break;
                case "teachers":
                    fileContent = ImportTemplateService.GenerateTeachersTemplate();
                    fileName = "GPU004_Teachers_Template.csv";
                    break;
                case "classes":
                    fileContent = ImportTemplateService.GenerateClassesTemplate();
                    fileName = "GPU003_Classes_Template.csv";
                    break;
                case "students":
                    fileContent = ImportTemplateService.GenerateStudentsTemplate();
                    fileName = "GPU010_Students_Template.csv";
                    break;
                case "subjects":
                    fileContent = ImportTemplateService.GenerateSubjectsTemplate();
                    fileName = "GPU006_Subjects_Template.csv";
                    break;
                case "rooms":
                    fileContent = ImportTemplateService.GenerateRoomsTemplate();
                    fileName = "GPU005_Rooms_Template.csv";
                    break;
                case "lessons":
                    fileContent = ImportTemplateService.GenerateLessonsTemplate();
                    fileName = "GPU002_Lessons_Template.csv";
                    break;
                case "qualifications":
                    fileContent = ImportTemplateService.GenerateTeacherQualificationsTemplate();
                    fileName = "GPU008_TeacherQualifications_Template.csv";
                    break;
                case "timetable":
                    fileContent = ImportTemplateService.GenerateTimetableTemplate();
                    fileName = "GPU001_Timetable_Template.csv";
                    break;
                case "availability":
                    fileContent = ImportTemplateService.GenerateAvailabilityTemplate();
                    fileName = "GPU016_Availability_Template.csv";
                    break;
                default:
                    return NotFound();
            }

            return File(fileContent, "text/csv", fileName);
        }
    }
}
