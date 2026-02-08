using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;
using SchedulingSystem.Services;
using SchedulingSystem.Services.LessonMovement;
using SchedulingSystem.Hubs;
using System.Collections.Concurrent;

namespace SchedulingSystem.Pages.Admin.Timetables;

public class MoveLessonsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly TimetableConflictService _conflictService;
    private readonly LessonMovementService _movementService;
    private readonly SimpleTimetableGenerationService _generationService;
    private readonly IHubContext<TimetableGenerationHub> _hubContext;
    private readonly ILogger<MoveLessonsModel> _logger;

    // Static dictionary to store cancellation tokens for active generation sessions
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> _activeSessions = new();

    public MoveLessonsModel(
        ApplicationDbContext context,
        TimetableConflictService conflictService,
        LessonMovementService movementService,
        SimpleTimetableGenerationService generationService,
        IHubContext<TimetableGenerationHub> hubContext,
        ILogger<MoveLessonsModel> logger)
    {
        _context = context;
        _conflictService = conflictService;
        _movementService = movementService;
        _generationService = generationService;
        _hubContext = hubContext;
        _logger = logger;
    }

    [BindProperty]
    public int? SelectedTimetableId { get; set; }

    public List<SelectListItem> TimetableList { get; set; } = new();
    public Timetable? Timetable { get; set; }
    public List<Period> Periods { get; set; } = new();
    public List<ScheduledLesson> ScheduledLessons { get; set; } = new();
    public List<Room> AvailableRooms { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    // Availability data for conflict detection (Importance = -3 means absolute unavailability)
    // Key format: "TeacherId:Day:PeriodId" or "ClassId:Day:PeriodId" etc.
    public Dictionary<string, bool> TeacherUnavailability { get; set; } = new();
    public Dictionary<string, bool> ClassUnavailability { get; set; } = new();
    public Dictionary<string, bool> SubjectUnavailability { get; set; } = new();
    public Dictionary<string, bool> RoomUnavailability { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        // Load all timetables for dropdown
        await LoadTimetableListAsync();

        // If id is provided in query string, use it
        if (id.HasValue)
        {
            SelectedTimetableId = id.Value;
        }
        // If no timetable is selected, automatically select the first one
        else if (!SelectedTimetableId.HasValue && TimetableList.Any())
        {
            SelectedTimetableId = int.Parse(TimetableList.First().Value);
        }

        // Load timetable data if a timetable is selected
        if (SelectedTimetableId.HasValue)
        {
            await LoadTimetableDataAsync(SelectedTimetableId.Value);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Load all timetables for dropdown
        await LoadTimetableListAsync();

        // Load timetable data if a timetable is selected
        if (SelectedTimetableId.HasValue)
        {
            await LoadTimetableDataAsync(SelectedTimetableId.Value);
        }

        return Page();
    }

    private async Task LoadTimetableListAsync()
    {
        var timetables = await _context.Timetables
            .Include(t => t.SchoolYear)
            .Include(t => t.Term)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();

        TimetableList = timetables.Select(t => new SelectListItem
        {
            Value = t.Id.ToString(),
            Text = $"{t.Name} - {t.SchoolYear?.Name}" +
                   (t.Term != null ? $" - {t.Term.Name}" : "") +
                   $" ({t.Status})",
            Selected = t.Id == SelectedTimetableId
        }).ToList();
    }

    private async Task LoadTimetableDataAsync(int timetableId)
    {
        // Load timetable
        Timetable = await _context.Timetables
            .Include(t => t.SchoolYear)
            .Include(t => t.Term)
            .FirstOrDefaultAsync(t => t.Id == timetableId);

        if (Timetable == null)
        {
            return;
        }

        // Load periods (excluding breaks)
        Periods = await _context.Periods
            .Where(p => !p.IsBreak)
            .OrderBy(p => p.PeriodNumber)
            .ToListAsync();

        // Load all scheduled lessons for this timetable with LessonAssignments
        var scheduledLessonsList = await _context.ScheduledLessons
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonSubjects)
                    .ThenInclude(ls => ls.Subject)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonTeachers)
                    .ThenInclude(lt => lt.Teacher)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonClasses)
                    .ThenInclude(lc => lc.Class)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonAssignments)
                    .ThenInclude(la => la.Teacher)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonAssignments)
                    .ThenInclude(la => la.Subject)
            .Include(sl => sl.Lesson)
                .ThenInclude(l => l!.LessonAssignments)
                    .ThenInclude(la => la.Class)
            .Include(sl => sl.Period)
            .Include(sl => sl.Room)
            .Include(sl => sl.ScheduledLessonRooms)
                .ThenInclude(slr => slr.Room)
            .Include(sl => sl.ScheduledLessonRooms)
                .ThenInclude(slr => slr.RoomAssignments)
                    .ThenInclude(ra => ra.LessonAssignment)
                        .ThenInclude(la => la!.Teacher)
            .Include(sl => sl.ScheduledLessonRooms)
                .ThenInclude(slr => slr.RoomAssignments)
                    .ThenInclude(ra => ra.LessonAssignment)
                        .ThenInclude(la => la!.Subject)
            .Include(sl => sl.ScheduledLessonRooms)
                .ThenInclude(slr => slr.RoomAssignments)
                    .ThenInclude(ra => ra.LessonAssignment)
                        .ThenInclude(la => la!.Class)
            .Where(sl => sl.TimetableId == timetableId)
            .AsSplitQuery()
            .ToListAsync();

        // Deduplicate by ScheduledLesson.Id
        ScheduledLessons = scheduledLessonsList
            .DistinctBy(sl => sl.Id)
            .ToList();

        // Load available rooms
        AvailableRooms = await _context.Rooms
            .Where(r => r.IsActive)
            .OrderBy(r => r.RoomNumber)
            .ToListAsync();

        // Load absolute unavailability data (Importance = -3)
        await LoadUnavailabilityDataAsync();
    }

    private async Task LoadUnavailabilityDataAsync()
    {
        // Load teacher unavailability (Importance = -3)
        var teacherUnavail = await _context.TeacherAvailabilities
            .Where(ta => ta.Importance == -3)
            .Select(ta => new { ta.TeacherId, ta.DayOfWeek, ta.PeriodId })
            .ToListAsync();
        foreach (var item in teacherUnavail)
        {
            TeacherUnavailability[$"{item.TeacherId}:{(int)item.DayOfWeek}:{item.PeriodId}"] = true;
        }

        // Load class unavailability (Importance = -3)
        var classUnavail = await _context.ClassAvailabilities
            .Where(ca => ca.Importance == -3)
            .Select(ca => new { ca.ClassId, ca.DayOfWeek, ca.PeriodId })
            .ToListAsync();
        foreach (var item in classUnavail)
        {
            ClassUnavailability[$"{item.ClassId}:{(int)item.DayOfWeek}:{item.PeriodId}"] = true;
        }

        // Load subject unavailability (Importance = -3)
        var subjectUnavail = await _context.SubjectAvailabilities
            .Where(sa => sa.Importance == -3)
            .Select(sa => new { sa.SubjectId, sa.DayOfWeek, sa.PeriodId })
            .ToListAsync();
        foreach (var item in subjectUnavail)
        {
            SubjectUnavailability[$"{item.SubjectId}:{(int)item.DayOfWeek}:{item.PeriodId}"] = true;
        }

        // Load room unavailability (Importance = -3)
        var roomUnavail = await _context.RoomAvailabilities
            .Where(ra => ra.Importance == -3)
            .Select(ra => new { ra.RoomId, ra.DayOfWeek, ra.PeriodId })
            .ToListAsync();
        foreach (var item in roomUnavail)
        {
            RoomUnavailability[$"{item.RoomId}:{(int)item.DayOfWeek}:{item.PeriodId}"] = true;
        }
    }

    public async Task<IActionResult> OnPostToggleLockAsync(int lessonId, int timetableId)
    {
        try
        {
            var scheduledLesson = await _context.ScheduledLessons
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l!.LessonSubjects)
                        .ThenInclude(ls => ls.Subject)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l!.LessonClasses)
                        .ThenInclude(lc => lc.Class)
                .AsSplitQuery()
                .FirstOrDefaultAsync(sl => sl.Id == lessonId);

            if (scheduledLesson == null)
            {
                ErrorMessage = "Lesson not found.";
                return RedirectToPage(new { id = timetableId });
            }

            // Toggle the lock status
            scheduledLesson.IsLocked = !scheduledLesson.IsLocked;
            await _context.SaveChangesAsync();

            var status = scheduledLesson.IsLocked ? "locked" : "unlocked";
            var subjectName = scheduledLesson.Lesson!.LessonSubjects.FirstOrDefault()?.Subject?.Name ?? "N/A";
            var className = scheduledLesson.Lesson!.LessonClasses.FirstOrDefault()?.Class?.Name ?? "N/A";
            SuccessMessage = $"{subjectName} - {className} is now {status}. " +
                           (scheduledLesson.IsLocked ? "It won't be moved during regeneration." : "It can be moved during regeneration.");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error toggling lock status: {ex.Message}";
        }

        return RedirectToPage(new { id = timetableId });
    }

    // Helper method to get lessons for a specific day/period
    public List<ScheduledLesson> GetLessonsForSlot(DayOfWeek day, int periodId)
    {
        return ScheduledLessons
            .Where(sl => sl.DayOfWeek == day && sl.PeriodId == periodId)
            .DistinctBy(sl => sl.Id)
            .ToList();
    }

    // ==================== LESSON MOVEMENT API ENDPOINTS ====================

    /// <summary>
    /// Get all available slots for a scheduled lesson (AJAX endpoint)
    /// </summary>
    public async Task<IActionResult> OnGetAvailableSlotsAsync(
        int scheduledLessonId,
        string? excludeSlots = null)
    {
        try
        {
            // Parse exclude slots if provided (format: "Sunday,1;Monday,2")
            var excludeList = new List<(DayOfWeek Day, int PeriodId)>();
            if (!string.IsNullOrEmpty(excludeSlots))
            {
                var slots = excludeSlots.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var slot in slots)
                {
                    var parts = slot.Split(',');
                    if (parts.Length == 2 &&
                        Enum.TryParse<DayOfWeek>(parts[0], out var day) &&
                        int.TryParse(parts[1], out var periodId))
                    {
                        excludeList.Add((day, periodId));
                    }
                }
            }

            var availableSlots = await _movementService.GetAvailableSlotsAsync(
                scheduledLessonId,
                excludeList.Any() ? excludeList : null);

            // Get period names for display
            var periods = await _context.Periods.ToDictionaryAsync(p => p.Id, p => p.Name);

            var result = availableSlots.Select(s => new
            {
                dayOfWeek = s.DayOfWeek.ToString(),
                periodId = s.PeriodId,
                periodName = periods.GetValueOrDefault(s.PeriodId, $"Period {s.PeriodId}"),
                roomId = s.RoomId,
                roomName = s.RoomName,
                qualityScore = s.QualityScore,
                hasHardViolations = s.HasHardConstraintViolations,
                hardViolations = s.HardViolations,
                softViolations = s.SoftViolations,
                isCurrentSlot = s.IsCurrentSlot
            });

            return new JsonResult(new { success = true, slots = result });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get available slots grouped by quality (AJAX endpoint)
    /// </summary>
    public async Task<IActionResult> OnGetAvailableSlotsGroupedAsync(
        int scheduledLessonId,
        string? excludeSlots = null)
    {
        try
        {
            // Parse exclude slots
            var excludeList = new List<(DayOfWeek Day, int PeriodId)>();
            if (!string.IsNullOrEmpty(excludeSlots))
            {
                var slots = excludeSlots.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var slot in slots)
                {
                    var parts = slot.Split(',');
                    if (parts.Length == 2 &&
                        Enum.TryParse<DayOfWeek>(parts[0], out var day) &&
                        int.TryParse(parts[1], out var periodId))
                    {
                        excludeList.Add((day, periodId));
                    }
                }
            }

            var groupedSlots = await _movementService.GetAvailableSlotsGroupedAsync(
                scheduledLessonId,
                excludeList.Any() ? excludeList : null);

            // Get period names for display
            var periods = await _context.Periods.ToDictionaryAsync(p => p.Id, p => p.Name);

            var formatSlots = (List<AvailableSlot> slots) =>
                slots.Select(s => new
                {
                    dayOfWeek = s.DayOfWeek.ToString(),
                    periodId = s.PeriodId,
                    periodName = periods.GetValueOrDefault(s.PeriodId, $"Period {s.PeriodId}"),
                    roomId = s.RoomId,
                    roomName = s.RoomName,
                    qualityScore = s.QualityScore,
                    hasHardViolations = s.HasHardConstraintViolations,
                    softViolations = s.SoftViolations
                }).ToList();

            return new JsonResult(new
            {
                success = true,
                perfect = formatSlots(groupedSlots.Perfect),
                good = formatSlots(groupedSlots.Good),
                acceptable = formatSlots(groupedSlots.Acceptable),
                poor = formatSlots(groupedSlots.Poor),
                unavailable = formatSlots(groupedSlots.Unavailable),
                totalAvailable = groupedSlots.TotalAvailableCount
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Find swap chains to move a lesson to target slot (AJAX endpoint)
    /// </summary>
    public async Task<IActionResult> OnGetFindSwapChainsAsync(
        int scheduledLessonId,
        string targetDay,
        int targetPeriodId,
        int? targetRoomId = null,
        int maxDepth = 3,
        int timeoutSeconds = 30,
        string? excludeSlots = null)
    {
        try
        {
            // Parse target day
            if (!Enum.TryParse<DayOfWeek>(targetDay, out var day))
            {
                return new JsonResult(new { success = false, error = "Invalid day of week" });
            }

            // Parse exclude slots
            var excludeList = new List<(DayOfWeek Day, int PeriodId)>();
            if (!string.IsNullOrEmpty(excludeSlots))
            {
                var slots = excludeSlots.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var slot in slots)
                {
                    var parts = slot.Split(',');
                    if (parts.Length == 2 &&
                        Enum.TryParse<DayOfWeek>(parts[0], out var excludeDay) &&
                        int.TryParse(parts[1], out var periodId))
                    {
                        excludeList.Add((excludeDay, periodId));
                    }
                }
            }

            var config = new SwapChainConfig
            {
                MaxDepth = maxDepth,
                Timeout = TimeSpan.FromSeconds(timeoutSeconds),
                ExcludeSlots = excludeList
            };

            var swapChains = await _movementService.FindSwapChainsAsync(
                scheduledLessonId,
                day,
                targetPeriodId,
                targetRoomId,
                config);

            var result = swapChains.Select(chain => new
            {
                isValid = chain.IsValid,
                totalMoves = chain.TotalMoves,
                qualityScore = chain.QualityScore,
                errorMessage = chain.ErrorMessage,
                steps = chain.Steps.Select(step => new
                {
                    stepNumber = step.StepNumber,
                    scheduledLessonId = step.ScheduledLessonId,
                    lessonDescription = step.LessonDescription,
                    from = new
                    {
                        day = step.FromDay.ToString(),
                        periodId = step.FromPeriodId,
                        periodName = step.FromPeriodName,
                        roomId = step.FromRoomId,
                        roomName = step.FromRoomName
                    },
                    to = new
                    {
                        day = step.ToDay.ToString(),
                        periodId = step.ToPeriodId,
                        periodName = step.ToPeriodName,
                        roomId = step.ToRoomId,
                        roomName = step.ToRoomName
                    }
                })
            });

            return new JsonResult(new { success = true, swapChains = result });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Execute a swap chain (POST endpoint)
    /// </summary>
    public async Task<IActionResult> OnPostExecuteSwapChainAsync([FromBody] SwapChainRequest request)
    {
        try
        {
            // Reconstruct the swap chain from the request
            var swapChain = new SwapChain
            {
                Steps = request.Steps.Select(s => new MoveStep
                {
                    ScheduledLessonId = s.ScheduledLessonId,
                    LessonDescription = s.LessonDescription,
                    FromDay = Enum.Parse<DayOfWeek>(s.FromDay),
                    FromPeriodId = s.FromPeriodId,
                    FromPeriodName = s.FromPeriodName,
                    FromRoomId = s.FromRoomId,
                    FromRoomName = s.FromRoomName,
                    ToDay = Enum.Parse<DayOfWeek>(s.ToDay),
                    ToPeriodId = s.ToPeriodId,
                    ToPeriodName = s.ToPeriodName,
                    ToRoomId = s.ToRoomId,
                    ToRoomName = s.ToRoomName,
                    StepNumber = s.StepNumber
                }).ToList(),
                IsValid = true
            };

            var result = await _movementService.ExecuteSwapChainAsync(swapChain, request.Force);

            if (result.Success)
            {
                return new JsonResult(new
                {
                    success = true,
                    message = $"Successfully executed {result.TotalMoves} move(s)",
                    totalMoves = result.TotalMoves
                });
            }
            else
            {
                return new JsonResult(new
                {
                    success = false,
                    error = result.ErrorMessage
                });
            }
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Validate if a move is possible (AJAX endpoint)
    /// </summary>
    public async Task<IActionResult> OnGetValidateMoveAsync(
        int scheduledLessonId,
        string targetDay,
        int targetPeriodId,
        int? targetRoomId = null)
    {
        try
        {
            // Parse target day
            if (!Enum.TryParse<DayOfWeek>(targetDay, out var day))
            {
                return new JsonResult(new { success = false, error = "Invalid day of week" });
            }

            var validation = await _movementService.ValidateMoveAsync(
                scheduledLessonId,
                day,
                targetPeriodId,
                targetRoomId);

            return new JsonResult(new
            {
                success = true,
                isValid = validation.IsValid,
                isLocked = validation.IsLocked,
                hasHardViolations = validation.HasHardConstraintViolations,
                qualityScore = validation.QualityScore,
                errorMessage = validation.ErrorMessage,
                hardViolations = validation.HardViolations,
                softViolations = validation.SoftViolations
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Determine movement strategy (direct move vs requires swaps) (AJAX endpoint)
    /// </summary>
    public async Task<IActionResult> OnGetMovementStrategyAsync(
        int scheduledLessonId,
        string targetDay,
        int targetPeriodId,
        int? targetRoomId = null)
    {
        try
        {
            // Parse target day
            if (!Enum.TryParse<DayOfWeek>(targetDay, out var day))
            {
                return new JsonResult(new { success = false, error = "Invalid day of week" });
            }

            var strategy = await _movementService.DetermineMovementStrategyAsync(
                scheduledLessonId,
                day,
                targetPeriodId,
                targetRoomId);

            return new JsonResult(new
            {
                success = true,
                strategyType = strategy.StrategyType.ToString(),
                canMoveDirectly = strategy.CanMoveDirectly,
                requiresSwaps = strategy.RequiresSwaps,
                errorMessage = strategy.ErrorMessage,
                validation = strategy.Validation != null ? new
                {
                    isValid = strategy.Validation.IsValid,
                    isLocked = strategy.Validation.IsLocked,
                    hasHardViolations = strategy.Validation.HasHardConstraintViolations,
                    qualityScore = strategy.Validation.QualityScore,
                    hardViolations = strategy.Validation.HardViolations,
                    softViolations = strategy.Validation.SoftViolations
                } : null
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    // ==================== TIMETABLE GENERATION API ENDPOINTS ====================

    /// <summary>
    /// Generate timetable options (POST endpoint)
    /// </summary>
    public async Task<IActionResult> OnPostGenerateOptionsAsync([FromBody] GenerateOptionsRequest request)
    {
        try
        {
            // Log incoming request
            _logger.LogInformation("=== GENERATE OPTIONS REQUEST START ===");
            _logger.LogInformation($"SessionId: {request.SessionId}");
            _logger.LogInformation($"TimetableId: {request.TimetableId}");
            _logger.LogInformation($"SelectedLessonIds: {string.Join(",", request.SelectedLessonIds)}");
            _logger.LogInformation($"DestinationDay: {request.DestinationDay}");
            _logger.LogInformation($"DestinationPeriodId: {request.DestinationPeriodId}");
            _logger.LogInformation($"MaxUnfixedLessons: {request.MaxUnfixedLessons}");
            _logger.LogInformation($"MaxTimeMinutes: {request.MaxTimeMinutes}");
            _logger.LogInformation($"IgnoredConstraints: {string.Join(",", request.IgnoredConstraintCodes ?? new List<string>())}");

            // Use session ID from frontend (already joined to SignalR group)
            var sessionId = request.SessionId;

            if (string.IsNullOrEmpty(sessionId))
            {
                _logger.LogError("Session ID is empty");
                return new JsonResult(new { success = false, error = "Session ID is required" });
            }

            // Create cancellation token source with timeout
            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(request.MaxTimeMinutes + 1));
            _activeSessions[sessionId] = cts;

            // Parse destination slot if provided
            (DayOfWeek Day, int PeriodId)? destination = null;
            if (!string.IsNullOrEmpty(request.DestinationDay) && request.DestinationPeriodId.HasValue)
            {
                if (Enum.TryParse<DayOfWeek>(request.DestinationDay, out var day))
                {
                    destination = (day, request.DestinationPeriodId.Value);
                }
            }

            // Parse avoid slots
            var avoidSlotsSelected = ParseSlotList(request.AvoidSlotsSelected);
            var avoidSlotsUnlocked = ParseSlotList(request.AvoidSlotsUnlocked);

            // Create generation request
            var generationRequest = new GenerationRequest
            {
                TimetableId = request.TimetableId,
                SelectedLessonIds = request.SelectedLessonIds,
                DestinationSlot = destination,
                AvoidSlotsForSelected = avoidSlotsSelected,
                AvoidSlotsForUnlocked = avoidSlotsUnlocked,
                MaxUnfixedLessonsToMove = request.MaxUnfixedLessons,
                MaxTimeMinutes = request.MaxTimeMinutes,
                IgnoredConstraintCodes = request.IgnoredConstraintCodes ?? new List<string>(),
                SelectedAlgorithm = request.SelectedAlgorithm
            };

            // Progress reporter
            var progress = new Progress<GenerationProgress>(async p =>
            {
                await _hubContext.Clients.Group(sessionId).SendAsync("ProgressUpdate",
                    p.OptionsGenerated,
                    p.CombinationsExplored,
                    p.ElapsedTime.TotalSeconds,
                    p.StatusMessage);
            });

            // Start generation and save options as draft timetables
            _logger.LogInformation("Starting generation service (will save options as draft timetables)...");
            var savedOptions = await _generationService.GenerateAndSaveOptionsAsync(
                generationRequest,
                progress,
                cts.Token);

            _logger.LogInformation($"Generation completed. Saved {savedOptions.Count} options as draft timetables");

            // Clean up session
            _activeSessions.TryRemove(sessionId, out _);

            // Format results for JSON
            _logger.LogInformation("Formatting results for JSON...");
            var results = savedOptions.Select(o => new
            {
                timetableId = o.TimetableId,
                optionNumber = o.OptionNumber,
                qualityScore = o.QualityScore,
                totalMovedLessons = o.TotalMovedLessons,
                unfixedLessonsMoved = o.UnfixedLessonsMoved,
                softViolationCount = o.SoftViolationCount,
                movements = o.Movements.Select(m => new
                {
                    scheduledLessonId = m.ScheduledLessonId,
                    lessonId = m.LessonId,
                    lessonDescription = m.LessonDescription,
                    isSelectedLesson = m.IsSelectedLesson,
                    from = new
                    {
                        day = m.FromDay.ToString(),
                        periodId = m.FromPeriodId,
                        periodName = m.FromPeriodName,
                        roomId = m.FromRoomId
                    },
                    to = new
                    {
                        day = m.ToDay.ToString(),
                        periodId = m.ToPeriodId,
                        periodName = m.ToPeriodName,
                        roomId = m.ToRoomId
                    }
                }).ToList(),
                softViolations = o.SoftViolations,
                timetableState = o.TimetableState
            });

            return new JsonResult(new
            {
                success = true,
                sessionId = sessionId,
                optionsCount = savedOptions.Count,
                options = results
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Generation was cancelled by user");
            return new JsonResult(new
            {
                success = true,
                message = "Generation was cancelled by user",
                optionsCount = 0,
                options = new List<object>()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERROR in GenerateOptionsAsync: {Message}", ex.Message);
            _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
            _logger.LogError("Inner exception: {InnerException}", ex.InnerException?.Message);
            return new JsonResult(new
            {
                success = false,
                error = ex.Message,
                stackTrace = ex.StackTrace,
                innerError = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Stop an active generation session (POST endpoint)
    /// </summary>
    public IActionResult OnPostStopGenerationAsync([FromBody] StopGenerationRequest request)
    {
        if (_activeSessions.TryRemove(request.SessionId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();

            return new JsonResult(new
            {
                success = true,
                message = "Generation stopped successfully"
            });
        }

        return new JsonResult(new
        {
            success = false,
            error = "Session not found or already completed"
        });
    }

    /// <summary>
    /// Helper to parse slot list from string format
    /// </summary>
    private List<(DayOfWeek Day, int PeriodId)> ParseSlotList(string? slotsString)
    {
        var result = new List<(DayOfWeek Day, int PeriodId)>();

        if (string.IsNullOrEmpty(slotsString))
            return result;

        var slots = slotsString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var slot in slots)
        {
            var parts = slot.Split(',');
            if (parts.Length == 2 &&
                Enum.TryParse<DayOfWeek>(parts[0], out var day) &&
                int.TryParse(parts[1], out var periodId))
            {
                result.Add((day, periodId));
            }
        }

        return result;
    }

    // Request models
    public class GenerateOptionsRequest
    {
        public string SessionId { get; set; } = "";
        public int TimetableId { get; set; }
        public List<int> SelectedLessonIds { get; set; } = new();
        public string? DestinationDay { get; set; }
        public int? DestinationPeriodId { get; set; }
        public string? AvoidSlotsSelected { get; set; }
        public string? AvoidSlotsUnlocked { get; set; }
        public string SelectedAlgorithm { get; set; } = "kempe-chain";
        public int MaxUnfixedLessons { get; set; } = 6;
        public int MaxTimeMinutes { get; set; } = 5;
        public List<string> IgnoredConstraintCodes { get; set; } = new();
    }

    public class StopGenerationRequest
    {
        public string SessionId { get; set; } = "";
    }

    /// <summary>
    /// Save an option as a permanent timetable (POST endpoint)
    /// </summary>
    public async Task<IActionResult> OnPostSaveOptionAsync([FromBody] SaveOptionRequest request)
    {
        try
        {
            _logger.LogInformation($"SaveOption called with {request.Movements.Count} movements for base timetable {request.BaseTimetableId}");

            // Load the base timetable
            var baseTimetable = await _context.Timetables
                .Include(t => t.ScheduledLessons)
                    .ThenInclude(sl => sl.Lesson)
                .Include(t => t.ScheduledLessons)
                    .ThenInclude(sl => sl.ScheduledLessonRooms)
                .Include(t => t.SchoolYear)
                .FirstOrDefaultAsync(t => t.Id == request.BaseTimetableId);

            if (baseTimetable == null)
            {
                return new JsonResult(new { success = false, error = "Base timetable not found" });
            }

            // Create new timetable using the same school year and term as the base timetable
            var newTimetable = new Timetable
            {
                Name = request.Name,
                SchoolYearId = baseTimetable.SchoolYearId,
                TermId = baseTimetable.TermId,
                CreatedDate = DateTime.UtcNow,
                Status = TimetableStatus.Draft,
                Notes = $"Generated from {baseTimetable.Name} via Move Lessons option"
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
            _logger.LogInformation($"Applying {request.Movements.Count} movements to new timetable {newTimetable.Id}");
            _logger.LogInformation($"Movement ScheduledLessonIds: {string.Join(", ", request.Movements.Select(m => m.ScheduledLessonId).Take(10))}");
            _logger.LogInformation($"ScheduledLessons map keys: {string.Join(", ", scheduledLessonsMap.Keys.Take(10))}");
            int appliedCount = 0;
            int notFoundCount = 0;

            foreach (var movement in request.Movements)
            {
                if (scheduledLessonsMap.TryGetValue(movement.ScheduledLessonId, out var sl))
                {
                    _logger.LogInformation($"  Applying movement: ScheduledLesson {movement.ScheduledLessonId} from {movement.FromDay}:{movement.FromPeriodId} to {movement.ToDay}:{movement.ToPeriodId}");

                    sl.DayOfWeek = Enum.Parse<DayOfWeek>(movement.ToDay);
                    sl.PeriodId = movement.ToPeriodId;
                    if (movement.ToRoomId.HasValue)
                    {
                        sl.RoomId = movement.ToRoomId;
                    }
                    appliedCount++;
                }
                else
                {
                    _logger.LogWarning($"  Movement for ScheduledLesson {movement.ScheduledLessonId} NOT FOUND in map!");
                    notFoundCount++;
                }
            }

            _logger.LogInformation($"Applied {appliedCount} movements, {notFoundCount} not found");
            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                timetableId = newTimetable.Id,
                message = $"Timetable '{request.Name}' saved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving option as timetable");
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Save an option temporarily for comparison (POST endpoint)
    /// </summary>
    public async Task<IActionResult> OnPostSaveOptionTempAsync([FromBody] SaveOptionTempRequest request)
    {
        try
        {
            // Load the base timetable
            var baseTimetable = await _context.Timetables
                .Include(t => t.ScheduledLessons)
                    .ThenInclude(sl => sl.ScheduledLessonRooms)
                .Include(t => t.SchoolYear)
                .FirstOrDefaultAsync(t => t.Id == request.BaseTimetableId);

            if (baseTimetable == null)
            {
                return new JsonResult(new { success = false, error = "Base timetable not found" });
            }

            // Create temporary timetable
            var tempTimetable = new Timetable
            {
                Name = $"TEMP_Option{request.OptionNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}",
                SchoolYearId = baseTimetable.SchoolYearId,
                TermId = baseTimetable.TermId,
                CreatedDate = DateTime.UtcNow,
                Status = TimetableStatus.Draft,
                Notes = "Temporary timetable for comparison - will be auto-deleted"
            };

            _context.Timetables.Add(tempTimetable);
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
                    TimetableId = tempTimetable.Id,
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
            _logger.LogInformation($"Applying {request.Movements.Count} movements to temp timetable {tempTimetable.Id}");
            _logger.LogInformation($"Scheduled lessons map contains {scheduledLessonsMap.Count} entries: {string.Join(", ", scheduledLessonsMap.Keys.Take(10))}");
            int appliedCount = 0;
            int notFoundCount = 0;

            foreach (var movement in request.Movements)
            {
                if (scheduledLessonsMap.TryGetValue(movement.ScheduledLessonId, out var sl))
                {
                    _logger.LogInformation($"  Applying movement: ScheduledLesson {movement.ScheduledLessonId} from {movement.FromDay}:{movement.FromPeriodId} to {movement.ToDay}:{movement.ToPeriodId}");

                    sl.DayOfWeek = Enum.Parse<DayOfWeek>(movement.ToDay);
                    sl.PeriodId = movement.ToPeriodId;
                    if (movement.ToRoomId.HasValue)
                    {
                        sl.RoomId = movement.ToRoomId;
                    }
                    appliedCount++;
                }
                else
                {
                    _logger.LogWarning($"  Movement for ScheduledLesson {movement.ScheduledLessonId} NOT FOUND in map!");
                    notFoundCount++;
                }
            }

            _logger.LogInformation($"Applied {appliedCount} movements, {notFoundCount} not found");
            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                tempTimetableId = tempTimetable.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving temporary option");
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get all option timetables for a base timetable (GET endpoint)
    /// </summary>
    public async Task<IActionResult> OnGetOptionTimetablesAsync(int baseTimetableId)
    {
        try
        {
            var baseTimetable = await _context.Timetables
                .FirstOrDefaultAsync(t => t.Id == baseTimetableId);

            if (baseTimetable == null)
            {
                return new JsonResult(new { success = false, error = "Base timetable not found" });
            }

            // Find all option timetables created from this base timetable
            // They have names starting with the base timetable name followed by "_Option"
            var optionTimetables = await _context.Timetables
                .Where(t => t.Name.StartsWith(baseTimetable.Name + "_Option") &&
                           t.SchoolYearId == baseTimetable.SchoolYearId &&
                           t.Status == TimetableStatus.Draft)
                .OrderByDescending(t => t.CreatedDate)
                .Select(t => new
                {
                    id = t.Id,
                    name = t.Name,
                    createdDate = t.CreatedDate,
                    notes = t.Notes
                })
                .ToListAsync();

            return new JsonResult(new { success = true, options = optionTimetables });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting option timetables");
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Delete multiple option timetables (POST endpoint)
    /// </summary>
    public async Task<IActionResult> OnPostDeleteMultipleOptionsAsync([FromBody] DeleteMultipleOptionsRequest request)
    {
        try
        {
            var timetables = await _context.Timetables
                .Include(t => t.ScheduledLessons)
                .Where(t => request.TimetableIds.Contains(t.Id))
                .ToListAsync();

            if (!timetables.Any())
            {
                return new JsonResult(new { success = false, error = "No timetables found" });
            }

            int deletedCount = 0;
            foreach (var timetable in timetables)
            {
                // Delete all scheduled lessons first
                _context.ScheduledLessons.RemoveRange(timetable.ScheduledLessons);

                // Delete the timetable
                _context.Timetables.Remove(timetable);
                deletedCount++;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Deleted {deletedCount} option timetables");

            return new JsonResult(new { success = true, message = $"Successfully deleted {deletedCount} option timetable(s)", deletedCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting multiple option timetables");
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a generated option timetable (POST endpoint)
    /// </summary>
    public async Task<IActionResult> OnPostDeleteOptionAsync([FromBody] DeleteOptionRequest request)
    {
        try
        {
            var timetable = await _context.Timetables
                .Include(t => t.ScheduledLessons)
                .FirstOrDefaultAsync(t => t.Id == request.TimetableId);

            if (timetable == null)
            {
                return new JsonResult(new { success = false, error = "Timetable not found" });
            }

            // Delete all scheduled lessons first
            _context.ScheduledLessons.RemoveRange(timetable.ScheduledLessons);

            // Delete the timetable
            _context.Timetables.Remove(timetable);

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Deleted option timetable {timetable.Id} ({timetable.Name})");

            return new JsonResult(new { success = true, message = $"Timetable '{timetable.Name}' deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting option timetable");
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    // Request models
    public class DeleteOptionRequest
    {
        public int TimetableId { get; set; }
    }

    public class DeleteMultipleOptionsRequest
    {
        public List<int> TimetableIds { get; set; } = new();
    }

    public class SaveOptionRequest
    {
        public int BaseTimetableId { get; set; }
        public string Name { get; set; } = "";
        public List<MovementDto> Movements { get; set; } = new();
    }

    public class SaveOptionTempRequest
    {
        public int BaseTimetableId { get; set; }
        public int OptionNumber { get; set; }
        public List<MovementDto> Movements { get; set; } = new();
    }

    public class MovementDto
    {
        public int ScheduledLessonId { get; set; }
        public int LessonId { get; set; } // Lesson template ID
        public string LessonDescription { get; set; } = "";
        public bool IsSelectedLesson { get; set; }
        public string FromDay { get; set; } = "";
        public int FromPeriodId { get; set; }
        public string FromPeriodName { get; set; } = "";
        public string ToDay { get; set; } = "";
        public int ToPeriodId { get; set; }
        public string ToPeriodName { get; set; } = "";
        public int? FromRoomId { get; set; }
        public int? ToRoomId { get; set; }
    }

    // Request model for swap chain execution
    public class SwapChainRequest
    {
        public List<MoveStepDto> Steps { get; set; } = new();
        public bool Force { get; set; }
    }

    public class MoveStepDto
    {
        public int ScheduledLessonId { get; set; }
        public string LessonDescription { get; set; } = "";
        public string FromDay { get; set; } = "";
        public int FromPeriodId { get; set; }
        public string FromPeriodName { get; set; } = "";
        public int? FromRoomId { get; set; }
        public string? FromRoomName { get; set; }
        public string ToDay { get; set; } = "";
        public int ToPeriodId { get; set; }
        public string ToPeriodName { get; set; } = "";
        public int? ToRoomId { get; set; }
        public string? ToRoomName { get; set; }
        public int StepNumber { get; set; }
    }
}
