using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;
using SchedulingSystem.Hubs;
using SchedulingSystem.Services.Constraints;
using System.Diagnostics;
using System.Text.Json;

namespace SchedulingSystem.Pages.Admin.Timetables;

public class LessonMoveResultModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LessonMoveResultModel> _logger;

    public LessonMoveResultModel(ApplicationDbContext context, ILogger<LessonMoveResultModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Query Parameters (for display)
    [BindProperty(SupportsGet = true)]
    public int TimetableId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string SelectedLessonIds { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string Algorithm { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public int MaxDepth { get; set; }

    [BindProperty(SupportsGet = true)]
    public int MaxTimeMinutes { get; set; }

    [BindProperty(SupportsGet = true)]
    public string DestinationDay { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public int? DestinationPeriodId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string AvoidSlotsSelected { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string AvoidSlotsUnlocked { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string IgnoredConstraints { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public bool SubstitutionMode { get; set; } = false;

    // Computed properties for display
    public List<int> SelectedLessonIdsList =>
        string.IsNullOrEmpty(SelectedLessonIds)
            ? new List<int>()
            : SelectedLessonIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToList();

    public List<string> IgnoredConstraintsList =>
        string.IsNullOrEmpty(IgnoredConstraints)
            ? new List<string>()
            : IgnoredConstraints.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

    // Display properties
    public string TimetableName { get; set; } = string.Empty;
    public List<SelectedLessonInfo> SelectedLessonsInfo { get; set; } = new();
    public List<ConstraintInfo> IgnoredConstraintsInfo { get; set; } = new();

    // Result property to hold execution results
    public DebugLessonMoveResult? Result { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            _logger.LogInformation("Starting debug lesson move for timetable {TimetableId}", TimetableId);

            // Load all lessons for this timetable with related data
            var allLessons = await _context.ScheduledLessons
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l!.LessonTeachers)
                        .ThenInclude(lt => lt.Teacher)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l!.LessonClasses)
                        .ThenInclude(lc => lc.Class)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l!.LessonSubjects)
                        .ThenInclude(ls => ls.Subject)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l!.LessonAssignments)
                        .ThenInclude(la => la.Teacher)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l!.LessonAssignments)
                        .ThenInclude(la => la.Subject)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l!.LessonAssignments)
                        .ThenInclude(la => la.Class)
                .Include(sl => sl.ScheduledLessonRooms)
                    .ThenInclude(slr => slr.Room)
                .Include(sl => sl.ScheduledLessonRooms)
                    .ThenInclude(slr => slr.RoomAssignments)
                        .ThenInclude(ra => ra.LessonAssignment)
                .Where(sl => sl.TimetableId == TimetableId)
                .AsSplitQuery()
                .ToListAsync();

            _logger.LogInformation("Loaded {Count} scheduled lessons", allLessons.Count);

            // Load timetable name
            var timetable = await _context.Timetables.FindAsync(TimetableId);
            TimetableName = timetable?.Name ?? $"Timetable {TimetableId}";

            // Populate selected lessons info
            foreach (var lessonId in SelectedLessonIdsList)
            {
                var lesson = allLessons.FirstOrDefault(l => l.Id == lessonId);
                if (lesson != null)
                {
                    var subjects = lesson.Lesson?.LessonSubjects?.Select(ls => ls.Subject?.Name).Where(n => n != null) ?? Enumerable.Empty<string?>();
                    var classes = lesson.Lesson?.LessonClasses?.Select(lc => lc.Class?.Name).Where(n => n != null) ?? Enumerable.Empty<string?>();
                    var teachers = lesson.Lesson?.LessonTeachers?.Select(lt => lt.Teacher?.ShortName ?? lt.Teacher?.FullName).Where(n => n != null) ?? Enumerable.Empty<string?>();

                    SelectedLessonsInfo.Add(new SelectedLessonInfo
                    {
                        ScheduledLessonId = lesson.Id,
                        LessonId = lesson.LessonId,
                        Subject = string.Join(", ", subjects),
                        Class = string.Join(", ", classes),
                        Teacher = string.Join(", ", teachers),
                        CurrentSlot = $"{lesson.DayOfWeek}-P{lesson.PeriodId}"
                    });
                }
            }

            // Populate ignored constraints info
            foreach (var code in IgnoredConstraintsList)
            {
                IgnoredConstraintsInfo.Add(new ConstraintInfo
                {
                    Code = code,
                    Name = GetConstraintName(code)
                });
            }

            // Get periods for this timetable (load first, then order in memory due to SQLite TimeSpan limitation)
            var periods = await _context.Periods.ToListAsync();
            periods = periods.OrderBy(p => p.StartTime).ToList();

            // Load availability data for constraint checking (HC-4 to HC-7)
            var availabilityData = await LoadAvailabilityDataAsync();

            // Build request from query parameters
            var request = new DebugLessonMoveRequest
            {
                TimetableId = TimetableId,
                SelectedLessonIds = SelectedLessonIdsList,
                Algorithm = Algorithm,
                MaxDepth = MaxDepth,
                MaxTimeMinutes = MaxTimeMinutes,
                DestinationDay = DestinationDay,
                DestinationPeriodId = DestinationPeriodId ?? 0,
                AvoidSlotsSelected = AvoidSlotsSelected,
                AvoidSlotsUnlocked = AvoidSlotsUnlocked,
                IgnoredConstraints = IgnoredConstraintsList,
                SubstitutionMode = SubstitutionMode
            };

            // Execute the algorithm
            var debugger = new SimpleLessonMoveDebugger(_context, _logger);
            Result = await debugger.TryMoveLesson(request, allLessons, periods, availabilityData);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing debug lesson move");
            Result = new DebugLessonMoveResult
            {
                Success = false,
                Message = $"Error: {ex.Message}",
                DebugEvents = new List<DebugEvent>()
            };
            return Page();
        }
    }

    public async Task<IActionResult> OnPostEvaluateConstraintsAsync([FromBody] EvaluateConstraintsRequest request)
    {
        try
        {
            _logger.LogInformation("Evaluating constraints for {Count} movements", request.Movements.Count);

            // Load scheduled lessons with all related data
            var scheduledLessonIds = request.Movements.Select(m => m.ScheduledLessonId).ToList();
            var scheduledLessons = await _context.ScheduledLessons
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l!.LessonTeachers)
                        .ThenInclude(lt => lt.Teacher)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l!.LessonClasses)
                        .ThenInclude(lc => lc.Class)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l!.LessonSubjects)
                        .ThenInclude(ls => ls.Subject)
                .Include(sl => sl.Room)
                .Include(sl => sl.ScheduledLessonRooms)
                    .ThenInclude(slr => slr.Room)
                .Where(sl => scheduledLessonIds.Contains(sl.Id))
                .ToDictionaryAsync(sl => sl.Id);

            // Load all scheduled lessons for the timetable (for conflict checking)
            var allScheduledLessons = await _context.ScheduledLessons
                .Include(sl => sl.Lesson)
                .Include(sl => sl.ScheduledLessonRooms)
                .Where(sl => sl.TimetableId == request.TimetableId)
                .ToListAsync();

            var violations = new List<List<ConstraintViolation>>();

            foreach (var movement in request.Movements)
            {
                var movementViolations = new List<ConstraintViolation>();

                if (!scheduledLessons.TryGetValue(movement.ScheduledLessonId, out var scheduledLesson))
                {
                    violations.Add(movementViolations);
                    continue;
                }

                // Parse the destination slot
                var toSlotParts = movement.ToSlot.Split(" - ");
                if (toSlotParts.Length != 2 ||
                    !Enum.TryParse<DayOfWeek>(toSlotParts[0], out var toDay) ||
                    !int.TryParse(toSlotParts[1].Replace("Period ", ""), out var toPeriodId))
                {
                    violations.Add(movementViolations);
                    continue;
                }

                // Check each ignored constraint to see if this movement would violate it
                foreach (var ignoredConstraint in request.IgnoredConstraints)
                {
                    var wouldViolate = CheckConstraintViolation(
                        scheduledLesson,
                        toDay,
                        toPeriodId,
                        ignoredConstraint,
                        allScheduledLessons);

                    if (wouldViolate != null)
                    {
                        movementViolations.Add(wouldViolate);
                    }
                }

                violations.Add(movementViolations);
            }

            return new JsonResult(new { violations });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating constraints");
            return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    public async Task<IActionResult> OnPostSaveAsDraftAsync([FromBody] SaveAsDraftRequest request)
    {
        try
        {
            _logger.LogInformation("Saving solution as draft timetable: {Name}", request.Name);

            // Load base timetable with scheduled lessons
            var baseTimetable = await _context.Timetables
                .Include(t => t.ScheduledLessons)
                    .ThenInclude(sl => sl.ScheduledLessonRooms)
                .FirstOrDefaultAsync(t => t.Id == request.TimetableId);

            if (baseTimetable == null)
            {
                return new JsonResult(new { error = "Base timetable not found" }) { StatusCode = 404 };
            }

            // Create new timetable
            var newTimetable = new Timetable
            {
                Name = request.Name,
                SchoolYearId = baseTimetable.SchoolYearId,
                TermId = baseTimetable.TermId,
                CreatedDate = DateTime.UtcNow,
                Status = TimetableStatus.Draft,
                Notes = $"Draft created from {baseTimetable.Name} via Debug Lesson Move"
            };

            _context.Timetables.Add(newTimetable);
            await _context.SaveChangesAsync();

            // Clone all scheduled lessons from base timetable
            var scheduledLessonsMap = new Dictionary<int, ScheduledLesson>();
            foreach (var sl in baseTimetable.ScheduledLessons)
            {
                var newSl = new ScheduledLesson
                {
                    LessonId = sl.LessonId,
                    DayOfWeek = sl.DayOfWeek,
                    PeriodId = sl.PeriodId,
                    RoomId = sl.RoomId,
                    WeekNumber = sl.WeekNumber,
                    TimetableId = newTimetable.Id,
                    IsLocked = sl.IsLocked
                };
                _context.ScheduledLessons.Add(newSl);
                scheduledLessonsMap[sl.Id] = newSl;

                // Clone multi-room assignments
                foreach (var slr in sl.ScheduledLessonRooms)
                {
                    newSl.ScheduledLessonRooms.Add(new ScheduledLessonRoom
                    {
                        RoomId = slr.RoomId
                    });
                }
            }

            await _context.SaveChangesAsync();

            // Apply movements
            _logger.LogInformation("Applying {Count} movements to new timetable {Id}", request.Movements.Count, newTimetable.Id);
            int appliedCount = 0;
            int removedCount = 0;

            foreach (var movement in request.Movements)
            {
                if (!scheduledLessonsMap.TryGetValue(movement.ScheduledLessonId, out var sl))
                {
                    _logger.LogWarning("Scheduled lesson {Id} not found in map", movement.ScheduledLessonId);
                    continue;
                }

                // Check if this is a removal (unscheduled) movement
                if (movement.ToSlot.Contains("REMOVED") || movement.ToSlot.Contains("Unscheduled"))
                {
                    _context.ScheduledLessons.Remove(sl);
                    removedCount++;
                    _logger.LogInformation("Removed scheduled lesson {Id}", movement.ScheduledLessonId);
                    continue;
                }

                // Parse destination slot - handle multiple formats
                DayOfWeek? toDay = null;
                int? toPeriodId = null;

                // Try format: "Sunday-P1" (from TimeSlot.ToString())
                var shortMatch = System.Text.RegularExpressions.Regex.Match(movement.ToSlot, @"(\w+)-P(\d+)");
                if (shortMatch.Success)
                {
                    if (Enum.TryParse<DayOfWeek>(shortMatch.Groups[1].Value, out var day))
                    {
                        toDay = day;
                        toPeriodId = int.Parse(shortMatch.Groups[2].Value);
                    }
                }

                // Try format: "Sunday, Period 1"
                if (!toDay.HasValue)
                {
                    var commaMatch = System.Text.RegularExpressions.Regex.Match(movement.ToSlot, @"(\w+),\s*Period\s*(\d+)");
                    if (commaMatch.Success)
                    {
                        if (Enum.TryParse<DayOfWeek>(commaMatch.Groups[1].Value, out var day))
                        {
                            toDay = day;
                            toPeriodId = int.Parse(commaMatch.Groups[2].Value);
                        }
                    }
                }

                // Try format: "Sunday - Period 1"
                if (!toDay.HasValue)
                {
                    var dashMatch = System.Text.RegularExpressions.Regex.Match(movement.ToSlot, @"(\w+)\s*-\s*Period\s*(\d+)");
                    if (dashMatch.Success)
                    {
                        if (Enum.TryParse<DayOfWeek>(dashMatch.Groups[1].Value, out var day))
                        {
                            toDay = day;
                            toPeriodId = int.Parse(dashMatch.Groups[2].Value);
                        }
                    }
                }

                if (toDay.HasValue && toPeriodId.HasValue)
                {
                    sl.DayOfWeek = toDay.Value;
                    sl.PeriodId = toPeriodId.Value;
                    appliedCount++;
                }
                else
                {
                    _logger.LogWarning("Could not parse destination slot: {ToSlot}", movement.ToSlot);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Draft timetable created: {Id}, applied {Applied} movements, removed {Removed} lessons",
                newTimetable.Id, appliedCount, removedCount);

            return new JsonResult(new { timetableId = newTimetable.Id, applied = appliedCount, removed = removedCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving draft timetable");
            return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    private ConstraintViolation? CheckConstraintViolation(
        ScheduledLesson scheduledLesson,
        DayOfWeek targetDay,
        int targetPeriodId,
        string constraintCode,
        List<ScheduledLesson> allScheduledLessons)
    {
        try
        {
            // Get constraint description
            var description = GetConstraintDescription(constraintCode);

            // Check based on constraint code
            switch (constraintCode)
            {
                case "HC-01": // Teacher conflict
                    var teacherIds = scheduledLesson.Lesson?.LessonTeachers?.Select(lt => lt.TeacherId).ToList() ?? new List<int>();
                    if (teacherIds.Any())
                    {
                        var hasConflict = allScheduledLessons.Any(sl =>
                            sl.Id != scheduledLesson.Id &&
                            sl.DayOfWeek == targetDay &&
                            sl.PeriodId == targetPeriodId &&
                            sl.Lesson?.LessonTeachers?.Any(lt => teacherIds.Contains(lt.TeacherId)) == true);

                        if (hasConflict)
                            return new ConstraintViolation { Code = constraintCode, Description = description };
                    }
                    break;

                case "HC-02": // Class conflict
                    var classIds = scheduledLesson.Lesson?.LessonClasses?.Select(lc => lc.ClassId).ToList() ?? new List<int>();
                    if (classIds.Any())
                    {
                        var hasConflict = allScheduledLessons.Any(sl =>
                            sl.Id != scheduledLesson.Id &&
                            sl.DayOfWeek == targetDay &&
                            sl.PeriodId == targetPeriodId &&
                            sl.Lesson?.LessonClasses?.Any(lc => classIds.Contains(lc.ClassId)) == true);

                        if (hasConflict)
                            return new ConstraintViolation { Code = constraintCode, Description = description };
                    }
                    break;

                case "HC-03": // Room conflict (if room assigned)
                    if (scheduledLesson.RoomId.HasValue || scheduledLesson.ScheduledLessonRooms.Any())
                    {
                        var roomIds = new List<int>();
                        if (scheduledLesson.RoomId.HasValue)
                            roomIds.Add(scheduledLesson.RoomId.Value);
                        roomIds.AddRange(scheduledLesson.ScheduledLessonRooms.Select(slr => slr.RoomId));

                        var hasConflict = allScheduledLessons.Any(sl =>
                            sl.Id != scheduledLesson.Id &&
                            sl.DayOfWeek == targetDay &&
                            sl.PeriodId == targetPeriodId &&
                            (roomIds.Contains(sl.RoomId ?? 0) ||
                             sl.ScheduledLessonRooms.Any(slr => roomIds.Contains(slr.RoomId))));

                        if (hasConflict)
                            return new ConstraintViolation { Code = constraintCode, Description = description };
                    }
                    break;

                // Add more constraint checks as needed
                default:
                    // For unhandled constraints, return null (not evaluated)
                    break;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking constraint {Constraint} for lesson {LessonId}", constraintCode, scheduledLesson.Id);
            return null;
        }
    }

    private string GetConstraintDescription(string code)
    {
        return code switch
        {
            "HC-01" => "Teacher Conflict",
            "HC-02" => "Class Conflict",
            "HC-03" => "Room Conflict",
            "HC-04" => "Subject Distribution",
            "HC-05" => "Teacher Availability",
            "HC-06" => "Room Availability",
            "HC-07" => "Class Availability",
            "HC-08" => "Consecutive Lessons",
            "HC-09" => "Daily Lesson Limit",
            "HC-10" => "Locked Lesson",
            _ => code
        };
    }

    private string GetConstraintName(string code)
    {
        return code switch
        {
            "HC-1" or "HC-01" => "Teacher Double-Booking",
            "HC-2" or "HC-02" => "Class Double-Booking",
            "HC-3" or "HC-03" => "Room Double-Booking",
            "HC-4" or "HC-04" => "Teacher Absolute Unavailability",
            "HC-5" or "HC-05" => "Class Absolute Unavailability",
            "HC-6" or "HC-06" => "Subject Absolute Unavailability",
            "HC-7" or "HC-07" => "Room Absolute Unavailability",
            "HC-8" or "HC-08" => "Max Consecutive Periods",
            "HC-9" or "HC-09" => "Max Periods Per Day",
            "HC-10" => "Locked Lessons",
            "HC-11" => "Teacher Workload Limit",
            "HC-12" => "Class Workload Limit",
            _ => code
        };
    }

    public async Task<IActionResult> OnPostStartDebugAsync([FromBody] DebugLessonMoveRequest request)
    {
        try
        {
            _logger.LogInformation("Starting debug lesson move for timetable {TimetableId}", request.TimetableId);

            // Load all lessons for this timetable with related data
            var allLessons = await _context.ScheduledLessons
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l!.LessonTeachers)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l!.LessonClasses)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l!.LessonSubjects)
                .Include(sl => sl.ScheduledLessonRooms)
                .Where(sl => sl.TimetableId == request.TimetableId)
                .ToListAsync();

            _logger.LogInformation("Loaded {Count} scheduled lessons", allLessons.Count);

            // Get periods for this timetable (load first, then order in memory due to SQLite TimeSpan limitation)
            var periods = await _context.Periods.ToListAsync();
            periods = periods.OrderBy(p => p.StartTime).ToList();

            // Load availability data
            var availabilityData = await LoadAvailabilityDataAsync();

            // Execute the algorithm
            var debugger = new SimpleLessonMoveDebugger(_context, _logger);
            var result = await debugger.TryMoveLesson(
                request,
                allLessons,
                periods,
                availabilityData);

            return new JsonResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing debug lesson move");
            return new JsonResult(new DebugLessonMoveResult
            {
                Success = false,
                Message = $"Error: {ex.Message}",
                DebugEvents = new List<DebugEvent>()
            });
        }
    }

    /// <summary>
    /// Load availability data for all resources (HC-4 to HC-7 constraint checking)
    /// Only loads absolute unavailability (importance = -3)
    /// </summary>
    private async Task<AvailabilityData> LoadAvailabilityDataAsync()
    {
        var data = new AvailabilityData();

        // Load teacher unavailability (HC-4)
        var teacherAvail = await _context.TeacherAvailabilities
            .Where(ta => ta.Importance == -3)
            .ToListAsync();

        foreach (var ta in teacherAvail)
        {
            var key = (ta.DayOfWeek, ta.PeriodId);
            if (!data.TeacherUnavailability.ContainsKey(key))
                data.TeacherUnavailability[key] = new HashSet<int>();
            data.TeacherUnavailability[key].Add(ta.TeacherId);
        }

        // Load class unavailability (HC-5)
        var classAvail = await _context.ClassAvailabilities
            .Where(ca => ca.Importance == -3)
            .ToListAsync();

        foreach (var ca in classAvail)
        {
            var key = (ca.DayOfWeek, ca.PeriodId);
            if (!data.ClassUnavailability.ContainsKey(key))
                data.ClassUnavailability[key] = new HashSet<int>();
            data.ClassUnavailability[key].Add(ca.ClassId);
        }

        // Load room unavailability (HC-6)
        var roomAvail = await _context.RoomAvailabilities
            .Where(ra => ra.Importance == -3)
            .ToListAsync();

        foreach (var ra in roomAvail)
        {
            var key = (ra.DayOfWeek, ra.PeriodId);
            if (!data.RoomUnavailability.ContainsKey(key))
                data.RoomUnavailability[key] = new HashSet<int>();
            data.RoomUnavailability[key].Add(ra.RoomId);
        }

        // Load subject unavailability (HC-7)
        var subjectAvail = await _context.SubjectAvailabilities
            .Where(sa => sa.Importance == -3)
            .ToListAsync();

        foreach (var sa in subjectAvail)
        {
            var key = (sa.DayOfWeek, sa.PeriodId);
            if (!data.SubjectUnavailability.ContainsKey(key))
                data.SubjectUnavailability[key] = new HashSet<int>();
            data.SubjectUnavailability[key].Add(sa.SubjectId);
        }

        _logger.LogInformation("Loaded availability data: {TeacherCount} teacher, {ClassCount} class, {RoomCount} room, {SubjectCount} subject unavailabilities",
            data.TeacherUnavailability.Sum(x => x.Value.Count),
            data.ClassUnavailability.Sum(x => x.Value.Count),
            data.RoomUnavailability.Sum(x => x.Value.Count),
            data.SubjectUnavailability.Sum(x => x.Value.Count));

        return data;
    }
}

/// <summary>
/// Holds availability constraint data for HC-4 to HC-7
/// </summary>
public class AvailabilityData
{
    public Dictionary<(DayOfWeek, int), HashSet<int>> TeacherUnavailability { get; set; } = new();
    public Dictionary<(DayOfWeek, int), HashSet<int>> ClassUnavailability { get; set; } = new();
    public Dictionary<(DayOfWeek, int), HashSet<int>> RoomUnavailability { get; set; } = new();
    public Dictionary<(DayOfWeek, int), HashSet<int>> SubjectUnavailability { get; set; } = new();
}

// Request model
public class DebugLessonMoveRequest
{
    public int TimetableId { get; set; }
    public List<int> SelectedLessonIds { get; set; } = new();
    public string Algorithm { get; set; } = string.Empty;
    public int MaxDepth { get; set; }
    public int MaxTimeMinutes { get; set; }
    public string DestinationDay { get; set; } = string.Empty;
    public int DestinationPeriodId { get; set; }
    public string AvoidSlotsSelected { get; set; } = string.Empty;
    public string AvoidSlotsUnlocked { get; set; } = string.Empty;
    public List<string> IgnoredConstraints { get; set; } = new();
    public bool SubstitutionMode { get; set; } = false;
}

// Result model
public class DebugLessonMoveResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<SolutionOption> Solutions { get; set; } = new();
    public List<DebugEvent> DebugEvents { get; set; } = new();
    public int NodesExplored { get; set; }
    public int MaxDepthReached { get; set; }
    public long ElapsedMs { get; set; }
    public int AttemptCount { get; set; }
}

// Solution option model
public class SolutionOption
{
    public int OptionNumber { get; set; }
    public List<Movement> Movements { get; set; } = new();
    public long FoundAtMs { get; set; }
}

// Movement model
public class Movement
{
    public int ScheduledLessonId { get; set; }  // ID of ScheduledLesson (unique instance)
    public int LessonId { get; set; }            // ID of Lesson (can have multiple instances)
    public string LessonDescription { get; set; } = string.Empty;
    public string FromSlot { get; set; } = string.Empty;
    public string ToSlot { get; set; } = string.Empty;
}

// Debug event model
public class DebugEvent
{
    public string NodeId { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string Type { get; set; } = string.Empty;
    public int Depth { get; set; }
    public int LessonId { get; set; }
    public string LessonDescription { get; set; } = string.Empty;
    public string TargetSlot { get; set; } = string.Empty;
    public string OriginalSlot { get; set; } = string.Empty;
    public List<int> Conflicts { get; set; } = new();
    public string Result { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

// Simple time slot representation
public class TimeSlot
{
    public DayOfWeek Day { get; set; }
    public int PeriodId { get; set; }

    public TimeSlot(DayOfWeek day, int periodId)
    {
        Day = day;
        PeriodId = periodId;
    }

    public override string ToString() => $"{Day}-P{PeriodId}";

    public override bool Equals(object? obj)
    {
        if (obj is TimeSlot other)
            return Day == other.Day && PeriodId == other.PeriodId;
        return false;
    }

    public override int GetHashCode() => HashCode.Combine(Day, PeriodId);
}

// Main algorithm implementation
public class SimpleLessonMoveDebugger
{
    private const int MAX_DEBUG_EVENTS = 5000; // Limit debug events to prevent page hang

    private readonly ApplicationDbContext _context;
    private readonly ILogger _logger;
    private readonly IHubContext<DebugRecursiveHub>? _hubContext;
    private readonly string? _sessionId;

    public SimpleLessonMoveDebugger(
        ApplicationDbContext context,
        ILogger logger,
        IHubContext<DebugRecursiveHub>? hubContext = null,
        string? sessionId = null)
    {
        _context = context;
        _logger = logger;
        _hubContext = hubContext;
        _sessionId = sessionId;
    }

    // Helper method to safely add debug events (with limit check)
    private void AddDebugEvent(List<DebugEvent> debugEvents, DebugEvent debugEvent)
    {
        if (debugEvents.Count < MAX_DEBUG_EVENTS)
        {
            debugEvents.Add(debugEvent);
        }
    }

    public async Task<DebugLessonMoveResult> TryMoveLesson(
        DebugLessonMoveRequest request,
        List<ScheduledLesson> allLessons,
        List<Period> periods,
        AvailabilityData? availabilityData = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var debugEvents = new List<DebugEvent>();

        // Assume we're moving the first selected lesson (lessonA)
        if (!request.SelectedLessonIds.Any())
        {
            return new DebugLessonMoveResult
            {
                Success = false,
                Message = "No lesson selected",
                DebugEvents = debugEvents,
                ElapsedMs = stopwatch.ElapsedMilliseconds
            };
        }

        var lessonAId = request.SelectedLessonIds.First();
        var lessonA = allLessons.FirstOrDefault(l => l.Id == lessonAId);

        if (lessonA == null)
        {
            return new DebugLessonMoveResult
            {
                Success = false,
                Message = $"Lesson {lessonAId} not found",
                DebugEvents = debugEvents,
                ElapsedMs = stopwatch.ElapsedMilliseconds
            };
        }

        // Validate MaxTimeMinutes
        if (request.MaxTimeMinutes <= 0)
        {
            _logger.LogWarning("Invalid MaxTimeMinutes={MaxTimeMinutes}, defaulting to 3 minutes", request.MaxTimeMinutes);
            request.MaxTimeMinutes = 3;
        }

        // Capture lessonA's original source slot
        var sourceSlot = new TimeSlot(lessonA.DayOfWeek, lessonA.PeriodId);

        // Build substitution slots and count substitution lessons per slot if substitution mode is enabled
        var substitutionSlots = new HashSet<TimeSlot>();
        var substitutionLessonsPerSlot = new Dictionary<TimeSlot, int>();
        if (request.SubstitutionMode)
        {
            // Find all slots that have lessons with "substitution" subject AND "v-res" class
            foreach (var lesson in allLessons)
            {
                var hasSubstitutionSubject = lesson.Lesson?.LessonSubjects?.Any(ls =>
                    ls.Subject?.Name?.Equals("substitution", StringComparison.OrdinalIgnoreCase) == true) ?? false;
                var hasVResClass = lesson.Lesson?.LessonClasses?.Any(lc =>
                    lc.Class?.Name?.Equals("v-res", StringComparison.OrdinalIgnoreCase) == true) ?? false;

                if (hasSubstitutionSubject && hasVResClass)
                {
                    var slot = new TimeSlot(lesson.DayOfWeek, lesson.PeriodId);
                    substitutionSlots.Add(slot);

                    // Count substitution lessons per slot
                    if (!substitutionLessonsPerSlot.ContainsKey(slot))
                        substitutionLessonsPerSlot[slot] = 0;
                    substitutionLessonsPerSlot[slot]++;
                }
            }
            _logger.LogInformation("Substitution mode enabled: Found {Count} substitution slots", substitutionSlots.Count);

            // Log slots with multiple substitution lessons
            var multiSlots = substitutionLessonsPerSlot.Where(kvp => kvp.Value > 1).ToList();
            if (multiSlots.Any())
            {
                _logger.LogInformation("Slots with multiple substitution lessons: {Slots}",
                    string.Join(", ", multiSlots.Select(kvp => $"{kvp.Key.Day}-P{kvp.Key.PeriodId}:{kvp.Value}")));
            }
        }

        // Build context
        var context = new LessonMoveContext
        {
            TimetableId = request.TimetableId,
            MaxDepth = request.MaxDepth,
            MaxTime = TimeSpan.FromMinutes(request.MaxTimeMinutes),
            Stopwatch = stopwatch,
            AllLessons = allLessons,
            Periods = periods,
            IgnoredConstraints = request.IgnoredConstraints,
            SlotsToAvoidForSelected = ParseSlots(request.AvoidSlotsSelected),
            SlotsToAvoidForUnlocked = ParseSlots(request.AvoidSlotsUnlocked),
            SelectedLessonIds = request.SelectedLessonIds, // Track originally selected lessons
            OriginalSlotsByLessonId = BuildOriginalSlotsByLessonId(allLessons), // Track original slots by LessonID
            OriginalLessonTemplateId = lessonA.LessonId, // Track initial lesson's template ID
            OriginalLessonSourceSlot = sourceSlot, // Track lessonA's source slot - don't move ANY instance of this LessonID back here
            // Copy availability data for constraint checking (HC-4 to HC-7)
            TeacherUnavailability = availabilityData?.TeacherUnavailability ?? new(),
            ClassUnavailability = availabilityData?.ClassUnavailability ?? new(),
            RoomUnavailability = availabilityData?.RoomUnavailability ?? new(),
            SubjectUnavailability = availabilityData?.SubjectUnavailability ?? new(),
            // Substitution planning mode
            SubstitutionMode = request.SubstitutionMode,
            SubstitutionSlots = substitutionSlots,
            SubstitutionLessonsPerSlot = substitutionLessonsPerSlot
        };

        // DEBUG: Log timeout configuration
        _logger.LogWarning("========== TIMEOUT CONFIG ==========");
        _logger.LogWarning("MaxTimeMinutes: {MaxTimeMinutes} minutes", request.MaxTimeMinutes);
        _logger.LogWarning("MaxTime: {MaxTime} ms", context.MaxTime.TotalMilliseconds);
        _logger.LogWarning("Stopwatch.IsRunning: {IsRunning}", stopwatch.IsRunning);
        _logger.LogWarning("Stopwatch.Elapsed: {Elapsed} ms", stopwatch.Elapsed.TotalMilliseconds);
        _logger.LogWarning("====================================");

        // Determine candidate slots for lessonA
        var candidateSlots = new List<TimeSlot>();

        if (!string.IsNullOrEmpty(request.DestinationDay) && request.DestinationPeriodId > 0)
        {
            // Only try the specified "Move To" slot
            if (Enum.TryParse<DayOfWeek>(request.DestinationDay, out var day))
            {
                var targetSlot = new TimeSlot(day, request.DestinationPeriodId);
                if (!targetSlot.Equals(sourceSlot) && !context.SlotsToAvoidForSelected.Contains(targetSlot))
                {
                    // Check availability constraints (HC-4 to HC-7) unless they are ignored
                    if (!IsSlotUnavailableForLesson(lessonA, targetSlot, context))
                    {
                        // In substitution mode, only allow slots that are substitution slots
                        if (!context.SubstitutionMode || context.SubstitutionSlots.Contains(targetSlot))
                        {
                            candidateSlots.Add(targetSlot);
                        }
                    }
                }
            }
        }
        else
        {
            // Try all slots to find multiple solutions
            foreach (var period in periods)
            {
                foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
                {
                    // Only work days: Sunday-Thursday (skip Friday and Saturday)
                    if (day == DayOfWeek.Friday || day == DayOfWeek.Saturday)
                        continue;

                    var slot = new TimeSlot(day, period.Id);

                    // Skip source slot
                    if (slot.Equals(sourceSlot))
                        continue;

                    // Skip avoid slots for selected lessons
                    if (context.SlotsToAvoidForSelected.Contains(slot))
                        continue;

                    // Check availability constraints (HC-4 to HC-7) unless they are ignored
                    if (IsSlotUnavailableForLesson(lessonA, slot, context))
                        continue;

                    // In substitution mode, only allow slots that are substitution slots
                    if (context.SubstitutionMode && !context.SubstitutionSlots.Contains(slot))
                        continue;

                    candidateSlots.Add(slot);
                }
            }
        }

        _logger.LogInformation("Trying {Count} candidate slots for lesson {LessonId}",
            candidateSlots.Count, lessonAId);

        // Initialize state
        var state = new MoveState
        {
            ProposedMoves = new Dictionary<int, TimeSlot>(),
            VisitedLessons = new HashSet<int>(),
            OriginalLessonId = lessonA.Id,
            OriginalPositions = BuildOriginalPositions(allLessons),
            AttemptCount = 0,
            MaxAttempts = 100000 // Increased to allow more exploration
        };

        // Collect multiple solutions
        var solutions = new List<SolutionOption>();
        var solutionCount = 0;
        var foundSolutionHashes = new HashSet<string>(); // Track unique solutions
        var exploredPathSignatures = new HashSet<string>(); // Track exploration paths tried
        var lastSolutionFoundMs = 0L;
        var consecutiveDuplicates = 0;
        var totalAttempts = 0;

        _logger.LogInformation("Starting search with MaxTime={MaxTime}min, MaxDepth={MaxDepth}, MaxAttempts={MaxAttempts}",
            context.MaxTime.TotalMinutes, context.MaxDepth, state.MaxAttempts);

        // Try each candidate slot and keep searching until time runs out
        foreach (var targetSlot in candidateSlots)
        {
            // For each target slot, try multiple times with varying exploration depth
            // This explores different conflict resolution paths to find multiple solutions
            int maxRetriesPerSlot = candidateSlots.Count == 1 ? 1000 : 20; // More retries if only one slot

            for (int retry = 0; retry < maxRetriesPerSlot; retry++)
            {
                totalAttempts++;

                // Check timeout more frequently
                // DEBUG: Log timeout check details
                if (totalAttempts % 10 == 0) // Log every 10 attempts
                {
                    _logger.LogInformation("TIMEOUT CHECK #{Attempt}: Elapsed={Elapsed}ms, MaxTime={MaxTime}ms, TimeoutReached={TimeoutReached}",
                        totalAttempts, context.Stopwatch.Elapsed.TotalMilliseconds, context.MaxTime.TotalMilliseconds, context.TimeoutReached);
                }

                if (context.TimeoutReached)
                {
                    _logger.LogWarning("TIME LIMIT REACHED! Found {Count} unique solutions after {TotalAttempts} total attempts ({StateAttempts} recursive attempts) in {ElapsedMs}ms",
                        solutions.Count, totalAttempts, state.AttemptCount, stopwatch.ElapsedMilliseconds);
                    break;
                }

                if (state.AttemptCount >= state.MaxAttempts)
                {
                    _logger.LogWarning("Max attempts reached. Found {Count} solutions after {TotalAttempts} total attempts",
                        solutions.Count, totalAttempts);
                    break;
                }

                // If we haven't found a new solution in a while and found many duplicates, be more aggressive with timeout
                if (consecutiveDuplicates > 50 && stopwatch.ElapsedMilliseconds > lastSolutionFoundMs + 30000)
                {
                    _logger.LogInformation("No new solutions in 30s (after {Duplicates} duplicates, {TotalAttempts} total attempts). Stopping search.",
                        consecutiveDuplicates, totalAttempts);
                    break;
                }

                // Create a signature for this exploration path to avoid retrying the same approach
                var pathSignature = $"{targetSlot}::{retry % 100}"; // Use modulo to group similar variations
                if (exploredPathSignatures.Contains(pathSignature) && retry > 100)
                {
                    // We've tried this general approach before, skip to reduce redundancy
                    continue;
                }

                // Reset state for each attempt (but keep attempt counter)
                var attemptState = new MoveState
                {
                    ProposedMoves = new Dictionary<int, TimeSlot>(),
                    VisitedLessons = new HashSet<int>(),
                    OriginalLessonId = lessonA.Id,
                    OriginalPositions = state.OriginalPositions,
                    AttemptCount = state.AttemptCount,
                    MaxAttempts = state.MaxAttempts,
                    ExplorationVariation = retry // Use retry number to vary exploration
                };

                var solution = RecursiveTryPlace(
                    lessonA,
                    targetSlot,
                    attemptState,
                    depth: 0,
                    debugEvents,
                    context,
                    parentNodeId: null);

                // Update global attempt counter
                state.AttemptCount = attemptState.AttemptCount;

                // CRITICAL: Check timeout immediately after recursive call returns to main loop
                if (context.TimeoutReached)
                {
                    _logger.LogWarning("TIME LIMIT REACHED after recursive call in main loop. Stopping with {Count} solutions found.",
                        solutions.Count);
                    break; // Exit retry loop
                }

                if (solution != null)
                {
                    // Record this path as explored (whether solution is unique or duplicate)
                    exploredPathSignatures.Add(pathSignature);

                    // CRITICAL: Validate that no LessonID ends up in its original slot
                    if (!IsValidSolution(solution, context))
                    {
                        // Solution is invalid - lesson placed back in original slot
                        _logger.LogDebug("Solution rejected: places LessonID back in original slot");
                        continue; // Skip this solution and try next
                    }

                    // Check if this is a unique solution (different movement pattern)
                    var solutionHash = GetSolutionHash(solution);

                    if (!foundSolutionHashes.Contains(solutionHash))
                    {
                        // Found a NEW unique solution! Add it to our collection
                        foundSolutionHashes.Add(solutionHash);
                        solutionCount++;
                        lastSolutionFoundMs = stopwatch.ElapsedMilliseconds;
                        consecutiveDuplicates = 0; // Reset duplicate counter

                        solutions.Add(new SolutionOption
                        {
                            OptionNumber = solutionCount,
                            Movements = solution.Movements,
                            FoundAtMs = stopwatch.ElapsedMilliseconds
                        });

                        _logger.LogInformation("Found solution #{Count} at {Ms}ms (retry {Retry}, total attempts: {TotalAttempts}, recursive: {RecursiveAttempts})",
                            solutionCount, stopwatch.ElapsedMilliseconds, retry, totalAttempts, state.AttemptCount);
                    }
                    else
                    {
                        // Found a duplicate solution - record to avoid this path in future
                        consecutiveDuplicates++;
                        if (consecutiveDuplicates % 20 == 0)
                        {
                            _logger.LogDebug("Found {Count} consecutive duplicate solutions (total attempts: {TotalAttempts})",
                                consecutiveDuplicates, totalAttempts);
                        }
                    }
                }
                else
                {
                    // No solution found on this path - mark it as explored too
                    exploredPathSignatures.Add(pathSignature);
                }
            }

            if (context.TimeoutReached || state.AttemptCount >= state.MaxAttempts)
                break;
        }

        // === SUBSTITUTION MODE: REMOVAL SEARCH ===
        // In substitution mode, also try to find solutions where L1 is REMOVED and a substitution lesson covers
        if (context.SubstitutionMode && !context.TimeoutReached)
        {
            _logger.LogInformation("=== Starting substitution removal search ===");

            // Find substitution lessons that can cover L1's slot
            var removalSolutions = FindRemovalSolutions(lessonA, sourceSlot, context, state, debugEvents);

            foreach (var removalSolution in removalSolutions)
            {
                var solutionHash = GetSolutionHash(removalSolution);
                if (!foundSolutionHashes.Contains(solutionHash))
                {
                    foundSolutionHashes.Add(solutionHash);
                    solutionCount++;

                    solutions.Add(new SolutionOption
                    {
                        OptionNumber = solutionCount,
                        Movements = removalSolution.Movements,
                        FoundAtMs = stopwatch.ElapsedMilliseconds
                    });

                    _logger.LogInformation("Found REMOVAL solution #{Count} at {Ms}ms",
                        solutionCount, stopwatch.ElapsedMilliseconds);
                }
            }

            _logger.LogInformation("=== Removal search complete: found {Count} removal solutions ===", removalSolutions.Count);
        }

        // Log final summary
        _logger.LogInformation("Search completed: Found {SolutionCount} unique solutions in {ElapsedMs}ms, " +
            "Total attempts: {TotalAttempts}, Recursive attempts: {RecursiveAttempts}, " +
            "Unique paths explored: {UniquePaths}, Explored {NodesExplored} nodes, Max depth: {MaxDepth}",
            solutions.Count, stopwatch.ElapsedMilliseconds, totalAttempts, state.AttemptCount,
            exploredPathSignatures.Count, debugEvents.Count,
            debugEvents.Any() ? debugEvents.Max(e => e.Depth) : 0);

        // Return all solutions found
        if (solutions.Any())
        {
            // Sort solutions by number of movements (ascending) - simpler solutions first
            var sortedSolutions = solutions
                .OrderBy(s => s.Movements.Count)
                .ThenBy(s => s.FoundAtMs)
                .ToList();

            // Renumber solutions after sorting
            for (int i = 0; i < sortedSolutions.Count; i++)
            {
                sortedSolutions[i].OptionNumber = i + 1;
            }

            var timeoutReached = context.TimeoutReached ? " - STOPPED: Time limit reached" : "";
            var debugTruncated = debugEvents.Count >= MAX_DEBUG_EVENTS
                ? $" [Debug events truncated at {MAX_DEBUG_EVENTS} to prevent page hang]"
                : "";
            var message = $"Found {sortedSolutions.Count} solution(s) in {stopwatch.ElapsedMilliseconds}ms " +
                $"(MaxTime: {context.MaxTime.TotalMinutes}min, MaxDepth: {context.MaxDepth}, " +
                $"Total attempts: {totalAttempts}, Recursive attempts: {state.AttemptCount}){timeoutReached}{debugTruncated}";

            return new DebugLessonMoveResult
            {
                Success = true,
                Message = message,
                Solutions = sortedSolutions,
                DebugEvents = debugEvents,
                NodesExplored = debugEvents.Count,
                MaxDepthReached = debugEvents.Any() ? debugEvents.Max(e => e.Depth) : 0,
                ElapsedMs = stopwatch.ElapsedMilliseconds,
                AttemptCount = state.AttemptCount
            };
        }

        // No solution found
        var timeoutNote = context.TimeoutReached ? " - Time limit reached" : "";
        var debugTruncatedNote = debugEvents.Count >= MAX_DEBUG_EVENTS
            ? $" [Debug events truncated at {MAX_DEBUG_EVENTS} to prevent page hang]"
            : "";
        var failMessage = $"No valid solution found after exploring all candidates{timeoutNote} " +
            $"(MaxTime: {context.MaxTime.TotalMinutes}min, MaxDepth: {context.MaxDepth}, " +
            $"ElapsedMs: {stopwatch.ElapsedMilliseconds}ms, Total attempts: {totalAttempts}, Recursive attempts: {state.AttemptCount}){debugTruncatedNote}";

        return new DebugLessonMoveResult
        {
            Success = false,
            Message = failMessage,
            Solutions = new List<SolutionOption>(),
            DebugEvents = debugEvents,
            NodesExplored = debugEvents.Count,
            MaxDepthReached = debugEvents.Any() ? debugEvents.Max(e => e.Depth) : 0,
            ElapsedMs = stopwatch.ElapsedMilliseconds,
            AttemptCount = state.AttemptCount
        };
    }

    // Core recursive method implementing the user's algorithm
    private Solution? RecursiveTryPlace(
        ScheduledLesson lessonToMove,
        TimeSlot targetSlot,
        MoveState state,
        int depth,
        List<DebugEvent> debugEvents,
        LessonMoveContext context,
        string? parentNodeId)
    {
        // ===== CRITICAL: CHECK TIMEOUT FIRST BEFORE ANY WORK =====
        // This ensures ANY recursive call returns immediately if max time is exceeded
        if (context.TimeoutReached)
        {
            // DEBUG: Log when recursive call stops due to timeout
            if (state.AttemptCount % 100 == 0)
            {
                _logger.LogInformation("RECURSIVE TIMEOUT at depth {Depth}: Elapsed={Elapsed}ms, MaxTime={MaxTime}ms",
                    depth, context.Stopwatch.Elapsed.TotalMilliseconds, context.MaxTime.TotalMilliseconds);
            }
            return null; // Return immediately without creating debug events or doing any work
        }

        var nodeId = Guid.NewGuid().ToString();

        // Get original slot
        var originalSlot = state.OriginalPositions.TryGetValue(lessonToMove.Id, out var origSlot)
            ? origSlot
            : new TimeSlot(lessonToMove.DayOfWeek, lessonToMove.PeriodId);

        // Create debug event
        AddDebugEvent(debugEvents, new DebugEvent
        {
            NodeId = nodeId,
            ParentId = parentNodeId,
            Type = "attempt",
            Depth = depth,
            LessonId = lessonToMove.Id,
            LessonDescription = GetLessonDescription(lessonToMove),
            TargetSlot = targetSlot.ToString(),
            OriginalSlot = originalSlot.ToString()
        });

        // ===== SAFETY CHECKS =====

        // 1. Check max depth
        if (depth >= context.MaxDepth)
        {
            AddDebugEvent(debugEvents, CreateFailureEvent(nodeId, depth, lessonToMove.Id, "max_depth",
                $"Max depth {context.MaxDepth} reached"));
            return null;
        }

        // 2. Check time limit again (after creating debug event for this node)
        if (context.TimeoutReached)
        {
            AddDebugEvent(debugEvents, CreateFailureEvent(nodeId, depth, lessonToMove.Id, "timeout",
                "Time limit reached"));
            return null;
        }

        // 3. Check cycle detection
        if (state.VisitedLessons.Contains(lessonToMove.Id))
        {
            AddDebugEvent(debugEvents, CreateFailureEvent(nodeId, depth, lessonToMove.Id, "cycle",
                "Cycle detected - lesson already visited"));
            return null;
        }

        // 4. DANGER CHECK: Don't accidentally move lessonA while resolving conflicts
        if (lessonToMove.Id == state.OriginalLessonId && depth > 0)
        {
            AddDebugEvent(debugEvents, CreateFailureEvent(nodeId, depth, lessonToMove.Id, "trying_to_move_original",
                "Cannot move original lesson (lessonA) during conflict resolution"));
            return null;
        }

        // 4b. CRITICAL CHECK: Don't move ANY instance of lessonA's template back to lessonA's source slot
        // This prevents any instance of the same lesson from returning to where lessonA started
        if (lessonToMove.LessonId == context.OriginalLessonTemplateId &&
            targetSlot.Equals(context.OriginalLessonSourceSlot))
        {
            AddDebugEvent(debugEvents, CreateFailureEvent(nodeId, depth, lessonToMove.Id, "moving_to_source_slot",
                $"Cannot move any instance of lessonA's template (LessonID {context.OriginalLessonTemplateId}) back to lessonA's source slot {context.OriginalLessonSourceSlot}"));
            return null;
        }

        // 5. Check if lesson is locked/fixed
        if (lessonToMove.IsLocked && !context.IgnoredConstraints.Contains("HC-10"))
        {
            AddDebugEvent(debugEvents, CreateFailureEvent(nodeId, depth, lessonToMove.Id, "locked",
                "Lesson is locked and cannot be moved"));
            return null;
        }

        // 6. Increment attempt counter
        state.AttemptCount++;
        if (state.AttemptCount >= state.MaxAttempts)
        {
            AddDebugEvent(debugEvents, CreateFailureEvent(nodeId, depth, lessonToMove.Id, "max_attempts",
                "Max attempts exceeded"));
            return null;
        }

        // ===== FIND CONFLICTS (lessonsMToN) =====

        var conflicts = FindConflictsAtSlot(lessonToMove, targetSlot, state, context);

        AddDebugEvent(debugEvents, new DebugEvent
        {
            NodeId = Guid.NewGuid().ToString(),
            ParentId = nodeId,
            Type = "conflicts_found",
            Depth = depth,
            LessonId = lessonToMove.Id,
            Conflicts = conflicts.Select(c => c.Id).ToList(),
            Message = $"Found {conflicts.Count} conflicting lesson(s)"
        });

        // ===== CHECK FOR LOCKED CONFLICTS =====

        var lockedConflicts = conflicts.Where(c => c.IsLocked).ToList();
        if (lockedConflicts.Any() && !context.IgnoredConstraints.Contains("HC-10"))
        {
            AddDebugEvent(debugEvents, CreateFailureEvent(nodeId, depth, lessonToMove.Id, "blocked_by_locked",
                $"Blocked by {lockedConflicts.Count} locked lesson(s)"));
            return null;
        }

        // ===== NO CONFLICTS = SUCCESS! =====

        if (!conflicts.Any())
        {
            AddDebugEvent(debugEvents, new DebugEvent
            {
                NodeId = Guid.NewGuid().ToString(),
                ParentId = nodeId,
                Type = "success",
                Depth = depth,
                LessonId = lessonToMove.Id,
                Result = "success",
                Message = "No conflicts - placement successful"
            });

            return new Solution
            {
                Movements = new List<Movement>
                {
                    new Movement
                    {
                        ScheduledLessonId = lessonToMove.Id,
                        LessonId = lessonToMove.LessonId,
                        LessonDescription = GetLessonDescription(lessonToMove),
                        FromSlot = originalSlot.ToString(),
                        ToSlot = targetSlot.ToString()
                    }
                }
            };
        }

        // ===== RESOLVE CONFLICTS RECURSIVELY =====

        // Clone state for this branch (central tracking store to avoid side effects)
        var newState = state.Clone();
        newState.VisitedLessons.Add(lessonToMove.Id);
        newState.ProposedMoves[lessonToMove.Id] = targetSlot;

        var allConflictSolutions = new List<Solution>();

        // For each lesson in lessonsMToN, start a new recursive call
        foreach (var conflict in conflicts)
        {
            // Check timeout before processing each conflict
            if (context.TimeoutReached)
            {
                return null; // Time exceeded, stop immediately
            }

            // Find alternative slots for this conflict
            var alternativeSlots = FindAlternativeSlotsForConflict(conflict, newState, context);

            AddDebugEvent(debugEvents, new DebugEvent
            {
                NodeId = Guid.NewGuid().ToString(),
                ParentId = nodeId,
                Type = "searching_alternatives",
                Depth = depth,
                LessonId = conflict.Id,
                LessonDescription = GetLessonDescription(conflict),
                Message = $"Found {alternativeSlots.Count} alternative slot(s) for conflicting lesson"
            });

            if (!alternativeSlots.Any())
            {
                AddDebugEvent(debugEvents, CreateFailureEvent(nodeId, depth, lessonToMove.Id, "no_alternatives",
                    $"No alternatives found for conflicting lesson {conflict.Id}"));
                return null;
            }

            // Try to place this conflict in an alternative slot (recursive call)
            // Use ExplorationVariation to try different subsets of alternatives
            int skip = (newState.ExplorationVariation * 3) % Math.Max(1, alternativeSlots.Count);
            int take = 10 + (newState.ExplorationVariation % 5); // Vary between 10-14 alternatives
            var alternativesToTry = alternativeSlots.Skip(skip).Take(take).ToList();

            Solution? conflictSolution = null;
            foreach (var altSlot in alternativesToTry)
            {
                // Check timeout before trying each alternative
                if (context.TimeoutReached)
                {
                    return null; // Time exceeded, stop immediately
                }

                conflictSolution = RecursiveTryPlace(
                    conflict,
                    altSlot,
                    newState,
                    depth + 1,
                    debugEvents,
                    context,
                    nodeId);

                // CRITICAL: Check timeout immediately after recursive call returns
                if (context.TimeoutReached)
                {
                    return null; // Time exceeded after recursive call, stop immediately
                }

                if (conflictSolution != null)
                {
                    // Update state with this conflict's solution
                    foreach (var move in conflictSolution.Movements)
                    {
                        var moveSlot = ParseSlotString(move.ToSlot);
                        if (moveSlot != null)
                        {
                            newState.ProposedMoves[move.LessonId] = moveSlot;
                        }
                    }

                    allConflictSolutions.Add(conflictSolution);
                    break; // Found solution for this conflict
                }
            }

            if (conflictSolution == null)
            {
                // Could not resolve this conflict
                AddDebugEvent(debugEvents, CreateFailureEvent(nodeId, depth, lessonToMove.Id, "cannot_resolve_conflict",
                    $"Cannot resolve conflict with lesson {conflict.Id}"));
                return null;
            }
        }

        // CRITICAL: Check timeout after resolving all conflicts, before building final solution
        if (context.TimeoutReached)
        {
            return null; // Time exceeded after conflict resolution, stop immediately
        }

        // All conflicts resolved!
        AddDebugEvent(debugEvents, new DebugEvent
        {
            NodeId = Guid.NewGuid().ToString(),
            ParentId = nodeId,
            Type = "success",
            Depth = depth,
            LessonId = lessonToMove.Id,
            Result = "success",
            Message = $"All {conflicts.Count} conflict(s) resolved successfully"
        });

        // Build complete solution
        var completeSolution = new Solution
        {
            Movements = new List<Movement>
            {
                new Movement
                {
                    ScheduledLessonId = lessonToMove.Id,
                    LessonId = lessonToMove.LessonId,
                    LessonDescription = GetLessonDescription(lessonToMove),
                    FromSlot = originalSlot.ToString(),
                    ToSlot = targetSlot.ToString()
                }
            }
        };

        // Add all conflict resolutions
        foreach (var conflictSol in allConflictSolutions)
        {
            completeSolution.Movements.AddRange(conflictSol.Movements);
        }

        return completeSolution;
    }

    // Find lessons that conflict with placing lessonToMove at targetSlot (without considering virtual state)
    // Used for removal solution search where we check against the original timetable state
    private List<ScheduledLesson> FindConflictsAtSlot(
        ScheduledLesson lessonToMove,
        TimeSlot targetSlot,
        LessonMoveContext context)
    {
        var conflicts = new List<ScheduledLesson>();

        // Get lessons currently at target slot (original positions only)
        var lessonsAtTarget = context.AllLessons.Where(l =>
            l.DayOfWeek == targetSlot.Day && l.PeriodId == targetSlot.PeriodId).ToList();

        // Get teachers, classes, and rooms for lessonToMove
        var teachers = lessonToMove.Lesson?.LessonTeachers?.Select(lt => lt.TeacherId).ToHashSet() ?? new HashSet<int>();
        var classes = lessonToMove.Lesson?.LessonClasses?.Select(lc => lc.ClassId).ToHashSet() ?? new HashSet<int>();
        var rooms = new HashSet<int?> { lessonToMove.RoomId };
        foreach (var slr in lessonToMove.ScheduledLessonRooms ?? Enumerable.Empty<ScheduledLessonRoom>())
        {
            rooms.Add(slr.RoomId);
        }

        // Check each lesson at target slot for conflicts
        foreach (var otherLesson in lessonsAtTarget)
        {
            if (otherLesson.Id == lessonToMove.Id)
                continue;

            var otherTeachers = otherLesson.Lesson?.LessonTeachers?.Select(lt => lt.TeacherId).ToHashSet() ?? new HashSet<int>();
            var otherClasses = otherLesson.Lesson?.LessonClasses?.Select(lc => lc.ClassId).ToHashSet() ?? new HashSet<int>();
            var otherRooms = new HashSet<int?> { otherLesson.RoomId };
            foreach (var slr in otherLesson.ScheduledLessonRooms ?? Enumerable.Empty<ScheduledLessonRoom>())
            {
                otherRooms.Add(slr.RoomId);
            }

            bool hasConflict = false;

            // Check for special exemptions before declaring conflicts
            // Teacher conflict (skip if intern teacher "xy")
            if (!context.IgnoredConstraints.Contains("HC-1"))
            {
                var lessonHasIntern = lessonToMove.Lesson?.LessonTeachers?.Any(lt =>
                    ConstraintDefinitions.SpecialCases.IsInternTeacher(lt.Teacher)) ?? false;
                var otherHasIntern = otherLesson.Lesson?.LessonTeachers?.Any(lt =>
                    ConstraintDefinitions.SpecialCases.IsInternTeacher(lt.Teacher)) ?? false;

                if (!lessonHasIntern && !otherHasIntern && teachers.Overlaps(otherTeachers))
                    hasConflict = true;
            }

            // Class conflict (skip if reserve class "v-res" or "Team")
            if (!context.IgnoredConstraints.Contains("HC-2"))
            {
                var lessonHasSpecialClass = lessonToMove.Lesson?.LessonClasses?.Any(lc =>
                    ConstraintDefinitions.SpecialCases.IsSpecialClass(lc.Class)) ?? false;
                var otherHasSpecialClass = otherLesson.Lesson?.LessonClasses?.Any(lc =>
                    ConstraintDefinitions.SpecialCases.IsSpecialClass(lc.Class)) ?? false;

                if (!lessonHasSpecialClass && !otherHasSpecialClass && classes.Overlaps(otherClasses))
                    hasConflict = true;
            }

            // Room conflict (skip if team room "Teamraum")
            if (!context.IgnoredConstraints.Contains("HC-3"))
            {
                var lessonHasSpecialRoom = ConstraintDefinitions.SpecialCases.IsSpecialRoom(lessonToMove.Room);
                var otherHasSpecialRoom = ConstraintDefinitions.SpecialCases.IsSpecialRoom(otherLesson.Room);

                if (!lessonHasSpecialRoom && !otherHasSpecialRoom &&
                    rooms.Any(r => r.HasValue) && otherRooms.Any(r => r.HasValue) &&
                    rooms.Overlaps(otherRooms))
                    hasConflict = true;
            }

            if (hasConflict)
                conflicts.Add(otherLesson);
        }

        return conflicts;
    }

    // Find lessons that conflict with placing lessonToMove at targetSlot (with virtual state)
    private List<ScheduledLesson> FindConflictsAtSlot(
        ScheduledLesson lessonToMove,
        TimeSlot targetSlot,
        MoveState state,
        LessonMoveContext context)
    {
        var conflicts = new List<ScheduledLesson>();

        // Get virtual state (consider proposed moves)
        var virtualState = state.GetVirtualState();

        // Get lessons at target slot (considering proposed moves)
        var lessonsAtTarget = context.AllLessons.Where(l =>
        {
            // Check if this lesson has been moved in our proposed state
            if (state.ProposedMoves.TryGetValue(l.Id, out var proposedSlot))
            {
                return proposedSlot.Equals(targetSlot);
            }
            else
            {
                // Use actual position
                return l.DayOfWeek == targetSlot.Day && l.PeriodId == targetSlot.PeriodId;
            }
        }).ToList();

        // Get teachers, classes, and rooms for lessonToMove
        var teachers = lessonToMove.Lesson?.LessonTeachers?.Select(lt => lt.TeacherId).ToHashSet() ?? new HashSet<int>();
        var classes = lessonToMove.Lesson?.LessonClasses?.Select(lc => lc.ClassId).ToHashSet() ?? new HashSet<int>();
        var rooms = new HashSet<int?> { lessonToMove.RoomId };
        foreach (var slr in lessonToMove.ScheduledLessonRooms ?? Enumerable.Empty<ScheduledLessonRoom>())
        {
            rooms.Add(slr.RoomId);
        }

        // Check each lesson at target slot for conflicts
        foreach (var otherLesson in lessonsAtTarget)
        {
            if (otherLesson.Id == lessonToMove.Id)
                continue; // Skip self

            var otherTeachers = otherLesson.Lesson?.LessonTeachers?.Select(lt => lt.TeacherId).ToHashSet() ?? new HashSet<int>();
            var otherClasses = otherLesson.Lesson?.LessonClasses?.Select(lc => lc.ClassId).ToHashSet() ?? new HashSet<int>();
            var otherRooms = new HashSet<int?> { otherLesson.RoomId };
            foreach (var slr in otherLesson.ScheduledLessonRooms ?? Enumerable.Empty<ScheduledLessonRoom>())
            {
                otherRooms.Add(slr.RoomId);
            }

            // Check for conflicts
            bool hasConflict = false;

            // Teacher conflict
            if (teachers.Overlaps(otherTeachers))
                hasConflict = true;

            // Class conflict
            if (classes.Overlaps(otherClasses))
                hasConflict = true;

            // Room conflict (only if both have rooms specified)
            if (rooms.Any(r => r.HasValue) && otherRooms.Any(r => r.HasValue))
            {
                if (rooms.Overlaps(otherRooms))
                    hasConflict = true;
            }

            if (hasConflict)
            {
                conflicts.Add(otherLesson);
            }
        }

        return conflicts;
    }

    // Find alternative slots for a conflicting lesson
    private List<TimeSlot> FindAlternativeSlotsForConflict(
        ScheduledLesson conflict,
        MoveState state,
        LessonMoveContext context)
    {
        var alternatives = new List<TimeSlot>();

        // Get source slot of conflict
        var sourceSlot = state.OriginalPositions.TryGetValue(conflict.Id, out var origSlot)
            ? origSlot
            : new TimeSlot(conflict.DayOfWeek, conflict.PeriodId);

        // Check if this conflicting lesson is one of the originally selected lessons
        bool isSelectedLesson = context.SelectedLessonIds.Contains(conflict.Id);

        // Try ALL slots (let recursion handle conflicts)
        foreach (var period in context.Periods)
        {
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                // Only work days: Sunday-Thursday (skip Friday and Saturday)
                if (day == DayOfWeek.Friday || day == DayOfWeek.Saturday)
                    continue;

                var slot = new TimeSlot(day, period.Id);

                // Skip source slot (don't move back to where it came from)
                if (slot.Equals(sourceSlot))
                    continue;

                // Apply appropriate avoid list based on whether this is a selected lesson or not
                if (isSelectedLesson)
                {
                    // This is one of the originally selected lessons - use "avoid for selected" list
                    if (context.SlotsToAvoidForSelected.Contains(slot))
                        continue;
                }
                else
                {
                    // This is an unlocked lesson - use "avoid for unlocked" list
                    if (context.SlotsToAvoidForUnlocked.Contains(slot))
                        continue;
                }

                // Skip if slot is already occupied in virtual state by another lesson in this solution branch
                if (state.ProposedMoves.Any(kv => kv.Key != conflict.Id && kv.Value.Equals(slot)))
                    continue;

                // Check availability constraints (HC-4 to HC-7) unless they are ignored
                if (IsSlotUnavailableForLesson(conflict, slot, context))
                    continue;

                // In substitution mode, restrict slots but allow swap/coverage behavior
                if (context.SubstitutionMode && !context.SubstitutionSlots.Contains(slot))
                {
                    // EXCEPTION: Allow substitution lessons to move to the original selected lesson's slot
                    // This enables "coverage" behavior where L1 is removed and L2 covers L1's position
                    // BUT only if the substitution lesson's original slot has 2+ substitution lessons
                    // (so another substitution lesson remains in that slot)
                    bool isSubstitutionLesson = IsSubstitutionLesson(conflict);
                    bool isOriginalSelectedSlot = slot.Equals(context.OriginalLessonSourceSlot);

                    if (!(isSubstitutionLesson && isOriginalSelectedSlot))
                        continue;

                    // Check if the substitution lesson can leave its slot (must have another substitution lesson there)
                    var conflictOriginalSlot = new TimeSlot(conflict.DayOfWeek, conflict.PeriodId);
                    if (context.SubstitutionLessonsPerSlot.TryGetValue(conflictOriginalSlot, out var count))
                    {
                        if (count < 2)
                        {
                            // Only 1 substitution lesson in this slot - can't leave or slot will be empty
                            continue;
                        }
                    }
                    else
                    {
                        // No count found (shouldn't happen for substitution lessons) - skip to be safe
                        continue;
                    }
                }

                // Add this slot - recursion will handle any conflicts at this slot
                alternatives.Add(slot);
            }
        }

        return alternatives;
    }

    /// <summary>
    /// Check if a lesson is a substitution lesson (has "substitution" subject)
    /// </summary>
    private bool IsSubstitutionLesson(ScheduledLesson lesson)
    {
        return lesson.Lesson?.LessonSubjects?.Any(ls =>
            ls.Subject?.Name?.Equals("substitution", StringComparison.OrdinalIgnoreCase) == true) ?? false;
    }

    /// <summary>
    /// Find "removal" solutions in substitution mode.
    /// These are solutions where L1 is removed (unscheduled) and a substitution lesson moves to cover L1's slot.
    /// A substitution lesson can only leave its slot if another substitution lesson remains there.
    /// </summary>
    private List<SolutionOption> FindRemovalSolutions(
        ScheduledLesson lessonToRemove,
        TimeSlot slotToCover,
        LessonMoveContext context,
        MoveState state,
        List<DebugEvent> debugEvents)
    {
        var solutions = new List<SolutionOption>();

        _logger.LogInformation("Looking for substitution lessons that can cover slot {Day}-P{Period}",
            slotToCover.Day, slotToCover.PeriodId);

        // Find slots that have 2+ substitution lessons (so one can leave)
        var eligibleSlots = context.SubstitutionLessonsPerSlot
            .Where(kvp => kvp.Value >= 2)
            .Select(kvp => kvp.Key)
            .ToList();

        _logger.LogInformation("Found {Count} slots with 2+ substitution lessons: {Slots}",
            eligibleSlots.Count,
            string.Join(", ", eligibleSlots.Select(s => $"{s.Day}-P{s.PeriodId}")));

        // Find substitution lessons in those slots
        foreach (var eligibleSlot in eligibleSlots)
        {
            var substitutionLessonsInSlot = context.AllLessons
                .Where(sl => sl.DayOfWeek == eligibleSlot.Day &&
                            sl.PeriodId == eligibleSlot.PeriodId &&
                            IsSubstitutionLesson(sl))
                .ToList();

            _logger.LogDebug("Slot {Day}-P{Period} has {Count} substitution lessons",
                eligibleSlot.Day, eligibleSlot.PeriodId, substitutionLessonsInSlot.Count);

            foreach (var subLesson in substitutionLessonsInSlot)
            {
                // Check if this substitution lesson can move to the target slot
                // 1. Check availability constraints
                if (IsSlotUnavailableForLesson(subLesson, slotToCover, context))
                {
                    _logger.LogDebug("Substitution lesson {Id} cannot cover {Day}-P{Period}: availability constraint",
                        subLesson.Id, slotToCover.Day, slotToCover.PeriodId);
                    continue;
                }

                // 2. Check for conflicts at the target slot (excluding the lesson being removed)
                var conflictsAtTarget = FindConflictsAtSlot(subLesson, slotToCover, context)
                    .Where(c => c.Id != lessonToRemove.Id) // Exclude L1 since it's being removed
                    .ToList();

                if (conflictsAtTarget.Any())
                {
                    _logger.LogDebug("Substitution lesson {Id} cannot cover {Day}-P{Period}: conflicts with {Conflicts}",
                        subLesson.Id, slotToCover.Day, slotToCover.PeriodId,
                        string.Join(", ", conflictsAtTarget.Select(c => c.Id)));
                    continue;
                }

                // This substitution lesson can cover! Create a solution.
                _logger.LogInformation("Found valid removal solution: Sub lesson {SubId} from {FromDay}-P{FromPeriod} can cover {ToDay}-P{ToPeriod}. L1 ({L1Id}) will be removed.",
                    subLesson.Id, eligibleSlot.Day, eligibleSlot.PeriodId,
                    slotToCover.Day, slotToCover.PeriodId, lessonToRemove.Id);

                var movements = new List<Movement>
                {
                    // First show L1 being removed (moving to "REMOVED" state)
                    new Movement
                    {
                        ScheduledLessonId = lessonToRemove.Id,
                        LessonId = lessonToRemove.LessonId,
                        LessonDescription = GetLessonDescription(lessonToRemove) + " [REMOVED]",
                        FromSlot = $"{slotToCover.Day}, Period {slotToCover.PeriodId}",
                        ToSlot = "REMOVED (Unscheduled)"
                    },
                    // Then show the substitution lesson moving to cover
                    new Movement
                    {
                        ScheduledLessonId = subLesson.Id,
                        LessonId = subLesson.LessonId,
                        LessonDescription = GetLessonDescription(subLesson) + " [COVERS]",
                        FromSlot = $"{eligibleSlot.Day}, Period {eligibleSlot.PeriodId}",
                        ToSlot = $"{slotToCover.Day}, Period {slotToCover.PeriodId}"
                    }
                };

                solutions.Add(new SolutionOption
                {
                    OptionNumber = 0, // Will be renumbered later
                    Movements = movements,
                    FoundAtMs = context.Stopwatch.ElapsedMilliseconds
                });

                // Add debug event
                if (debugEvents.Count < MAX_DEBUG_EVENTS)
                {
                    debugEvents.Add(new DebugEvent
                    {
                        NodeId = Guid.NewGuid().ToString(),
                        ParentId = null,
                        Depth = 0,
                        Type = "removal_solution",
                        LessonId = subLesson.Id,
                        TargetSlot = $"{slotToCover.Day}, Period {slotToCover.PeriodId}",
                        Result = "success",
                        Message = $"Substitution lesson {subLesson.Id} covers removed lesson {lessonToRemove.Id}",
                        Conflicts = new List<int>()
                    });
                }
            }
        }

        return solutions;
    }

    /// <summary>
    /// Check if a slot is unavailable for a lesson due to availability constraints (HC-4 to HC-7)
    /// Returns true if the slot should be skipped (unavailable)
    /// </summary>
    private bool IsSlotUnavailableForLesson(ScheduledLesson lesson, TimeSlot slot, LessonMoveContext context)
    {
        var key = (slot.Day, slot.PeriodId);

        // HC-4: Teacher Absolute Unavailability
        if (!context.IgnoredConstraints.Contains("HC-4"))
        {
            var teacherIds = lesson.Lesson?.LessonTeachers?.Select(lt => lt.TeacherId).ToHashSet() ?? new HashSet<int>();
            if (context.TeacherUnavailability.TryGetValue(key, out var unavailableTeachers))
            {
                if (teacherIds.Any(tid => unavailableTeachers.Contains(tid)))
                {
                    return true; // Teacher is unavailable at this slot
                }
            }
        }

        // HC-5: Class Absolute Unavailability
        if (!context.IgnoredConstraints.Contains("HC-5"))
        {
            var classIds = lesson.Lesson?.LessonClasses?.Select(lc => lc.ClassId).ToHashSet() ?? new HashSet<int>();
            if (context.ClassUnavailability.TryGetValue(key, out var unavailableClasses))
            {
                if (classIds.Any(cid => unavailableClasses.Contains(cid)))
                {
                    return true; // Class is unavailable at this slot
                }
            }
        }

        // HC-6: Room Absolute Unavailability
        if (!context.IgnoredConstraints.Contains("HC-6"))
        {
            var roomIds = new HashSet<int>();
            if (lesson.RoomId.HasValue)
                roomIds.Add(lesson.RoomId.Value);
            foreach (var slr in lesson.ScheduledLessonRooms ?? Enumerable.Empty<ScheduledLessonRoom>())
                roomIds.Add(slr.RoomId);

            if (roomIds.Any() && context.RoomUnavailability.TryGetValue(key, out var unavailableRooms))
            {
                if (roomIds.Any(rid => unavailableRooms.Contains(rid)))
                {
                    return true; // Room is unavailable at this slot
                }
            }
        }

        // HC-7: Subject Absolute Unavailability
        if (!context.IgnoredConstraints.Contains("HC-7"))
        {
            var subjectIds = lesson.Lesson?.LessonSubjects?.Select(ls => ls.SubjectId).ToHashSet() ?? new HashSet<int>();
            if (context.SubjectUnavailability.TryGetValue(key, out var unavailableSubjects))
            {
                if (subjectIds.Any(sid => unavailableSubjects.Contains(sid)))
                {
                    return true; // Subject is unavailable at this slot
                }
            }
        }

        return false; // Slot is available
    }

    // Helper methods
    private string GetSolutionHash(Solution solution)
    {
        // Create a unique hash based on all movements in the solution
        // Sort by ScheduledLesson ID to ensure consistent ordering
        var movementsStr = string.Join("|",
            solution.Movements
                .OrderBy(m => m.ScheduledLessonId)
                .Select(m => $"{m.ScheduledLessonId}:{m.ToSlot}"));
        return movementsStr;
    }

    private string GetSolutionHash(SolutionOption solution)
    {
        // Create a unique hash based on all movements in the solution
        // Sort by ScheduledLesson ID to ensure consistent ordering
        var movementsStr = string.Join("|",
            solution.Movements
                .OrderBy(m => m.ScheduledLessonId)
                .Select(m => $"{m.ScheduledLessonId}:{m.ToSlot}"));
        return movementsStr;
    }

    private Dictionary<int, TimeSlot> BuildOriginalPositions(List<ScheduledLesson> allLessons)
    {
        return allLessons.ToDictionary(
            l => l.Id,
            l => new TimeSlot(l.DayOfWeek, l.PeriodId));
    }

    private Dictionary<int, List<TimeSlot>> BuildOriginalSlotsByLessonId(List<ScheduledLesson> allLessons)
    {
        var originalSlots = new Dictionary<int, List<TimeSlot>>();

        foreach (var scheduledLesson in allLessons)
        {
            var lessonId = scheduledLesson.LessonId;
            var slot = new TimeSlot(scheduledLesson.DayOfWeek, scheduledLesson.PeriodId);

            if (!originalSlots.ContainsKey(lessonId))
            {
                originalSlots[lessonId] = new List<TimeSlot>();
            }

            originalSlots[lessonId].Add(slot);
        }

        return originalSlots;
    }

    // Validate that no lesson (by LessonID) ends up in a slot where that LessonID was originally scheduled
    private bool IsValidSolution(Solution solution, LessonMoveContext context)
    {
        foreach (var movement in solution.Movements)
        {
            var lessonId = movement.LessonId;

            // Parse the destination slot
            var toSlotParts = movement.ToSlot.Split(" - ");
            if (toSlotParts.Length != 2)
                continue;

            if (!Enum.TryParse<DayOfWeek>(toSlotParts[0], out var toDay))
                continue;

            if (!int.TryParse(toSlotParts[1].Replace("Period ", ""), out var toPeriodId))
                continue;

            var destinationSlot = new TimeSlot(toDay, toPeriodId);

            // Check if this LessonID was originally in the destination slot
            if (context.OriginalSlotsByLessonId.TryGetValue(lessonId, out var originalSlots))
            {
                if (originalSlots.Any(slot => slot.Equals(destinationSlot)))
                {
                    // This solution places a LessonID back into one of its original slots - INVALID
                    _logger.LogDebug("Solution rejected: LessonID {LessonId} (ScheduledLesson {ScheduledLessonId}) would be placed back into original slot {Slot}",
                        lessonId, movement.ScheduledLessonId, destinationSlot);
                    return false;
                }
            }
        }

        return true;
    }

    private string GetLessonDescription(ScheduledLesson lesson)
    {
        var subject = lesson.Lesson?.LessonSubjects?.FirstOrDefault()?.Subject?.Name ?? "Unknown";
        var className = lesson.Lesson?.LessonClasses?.FirstOrDefault()?.Class?.Name ?? "Unknown";
        var teacher = lesson.Lesson?.LessonTeachers?.FirstOrDefault()?.Teacher?.Name ?? "Unknown";
        return $"{subject} - {className} - {teacher}";
    }

    private DebugEvent CreateFailureEvent(string nodeId, int depth, int lessonId, string result, string message)
    {
        return new DebugEvent
        {
            NodeId = Guid.NewGuid().ToString(),
            ParentId = nodeId,
            Type = "failure",
            Depth = depth,
            LessonId = lessonId,
            Result = result,
            Message = message
        };
    }

    private List<TimeSlot> ParseSlots(string slotsString)
    {
        var slots = new List<TimeSlot>();
        if (string.IsNullOrEmpty(slotsString))
            return slots;

        foreach (var slotStr in slotsString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = slotStr.Split(',');
            if (parts.Length == 2 && Enum.TryParse<DayOfWeek>(parts[0], out var day) && int.TryParse(parts[1], out var periodId))
            {
                slots.Add(new TimeSlot(day, periodId));
            }
        }

        return slots;
    }

    private TimeSlot? ParseSlotString(string slotStr)
    {
        // Format: "Monday-P1"
        var parts = slotStr.Split('-');
        if (parts.Length == 2 && Enum.TryParse<DayOfWeek>(parts[0], out var day))
        {
            var periodPart = parts[1].Replace("P", "");
            if (int.TryParse(periodPart, out var periodId))
            {
                return new TimeSlot(day, periodId);
            }
        }
        return null;
    }
}

// Solution holder
public class Solution
{
    public List<Movement> Movements { get; set; } = new();
}

// Move state with central tracking store
public class MoveState
{
    public Dictionary<int, TimeSlot> ProposedMoves { get; set; } = new();
    public HashSet<int> VisitedLessons { get; set; } = new();
    public int OriginalLessonId { get; set; }
    public Dictionary<int, TimeSlot> OriginalPositions { get; set; } = new();
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; }
    public int ExplorationVariation { get; set; } = 0; // Varies exploration to find different solutions

    public MoveState Clone()
    {
        return new MoveState
        {
            ProposedMoves = new Dictionary<int, TimeSlot>(ProposedMoves),
            VisitedLessons = new HashSet<int>(VisitedLessons),
            OriginalLessonId = OriginalLessonId,
            OriginalPositions = OriginalPositions, // Shared reference (immutable)
            AttemptCount = AttemptCount,
            MaxAttempts = MaxAttempts,
            ExplorationVariation = ExplorationVariation
        };
    }

    public Dictionary<string, HashSet<int>> GetVirtualState()
    {
        var occupied = new Dictionary<string, HashSet<int>>();
        foreach (var (lessonId, slot) in ProposedMoves)
        {
            var key = $"{slot.Day},{slot.PeriodId}";
            if (!occupied.ContainsKey(key))
                occupied[key] = new HashSet<int>();
            occupied[key].Add(lessonId);
        }
        return occupied;
    }
}

// Context passed through recursion
public class LessonMoveContext
{
    public int TimetableId { get; set; }
    public int MaxDepth { get; set; }
    public TimeSpan MaxTime { get; set; }
    public Stopwatch Stopwatch { get; set; } = new();
    public List<ScheduledLesson> AllLessons { get; set; } = new();
    public List<Period> Periods { get; set; } = new();
    public List<string> IgnoredConstraints { get; set; } = new();
    public List<TimeSlot> SlotsToAvoidForSelected { get; set; } = new();
    public List<TimeSlot> SlotsToAvoidForUnlocked { get; set; } = new();
    public List<int> SelectedLessonIds { get; set; } = new(); // Track which lessons were originally selected

    // Track all original slots where each LessonID was scheduled
    // Key: LessonID, Value: List of original time slots for that lesson
    public Dictionary<int, List<TimeSlot>> OriginalSlotsByLessonId { get; set; } = new();

    // Track the LessonID (template) of the initial lessonA and its source slot
    // We prevent moving ANY instance of this LessonID back to lessonA's initial slot
    public int OriginalLessonTemplateId { get; set; }
    public TimeSlot OriginalLessonSourceSlot { get; set; } = new TimeSlot(DayOfWeek.Sunday, 0);

    // Availability data for constraint checking (HC-4 to HC-7)
    // Key: (DayOfWeek, PeriodId), Value: Set of TeacherIds that are absolutely unavailable (importance = -3)
    public Dictionary<(DayOfWeek, int), HashSet<int>> TeacherUnavailability { get; set; } = new();
    // Key: (DayOfWeek, PeriodId), Value: Set of ClassIds that are absolutely unavailable
    public Dictionary<(DayOfWeek, int), HashSet<int>> ClassUnavailability { get; set; } = new();
    // Key: (DayOfWeek, PeriodId), Value: Set of RoomIds that are absolutely unavailable
    public Dictionary<(DayOfWeek, int), HashSet<int>> RoomUnavailability { get; set; } = new();
    // Key: (DayOfWeek, PeriodId), Value: Set of SubjectIds that are absolutely unavailable
    public Dictionary<(DayOfWeek, int), HashSet<int>> SubjectUnavailability { get; set; } = new();

    // Substitution planning mode - only allow movement to slots with "substitution" subject and "v-res" class
    public bool SubstitutionMode { get; set; } = false;
    // Set of time slots that contain lessons with "substitution" subject and "v-res" class
    public HashSet<TimeSlot> SubstitutionSlots { get; set; } = new();
    // Count of substitution lessons per slot - used to determine if a substitution lesson can leave its slot
    public Dictionary<TimeSlot, int> SubstitutionLessonsPerSlot { get; set; } = new();

    public bool TimeoutReached => Stopwatch.Elapsed >= MaxTime;
}

// Request model for constraint evaluation
public class EvaluateConstraintsRequest
{
    public int TimetableId { get; set; }
    public List<MovementForEvaluation> Movements { get; set; } = new();
    public List<string> IgnoredConstraints { get; set; } = new();
}

public class MovementForEvaluation
{
    public int ScheduledLessonId { get; set; }
    public int LessonId { get; set; }
    public string FromSlot { get; set; } = string.Empty;
    public string ToSlot { get; set; } = string.Empty;
}

public class ConstraintViolation
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

// Request model for saving solution as draft timetable
public class SaveAsDraftRequest
{
    public int TimetableId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<MovementForEvaluation> Movements { get; set; } = new();
}

// Display model for selected lesson info
public class SelectedLessonInfo
{
    public int ScheduledLessonId { get; set; }
    public int LessonId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public string Teacher { get; set; } = string.Empty;
    public string CurrentSlot { get; set; } = string.Empty;
}

// Display model for constraint info
public class ConstraintInfo
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
