using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;
using SchedulingSystem.Hubs;
using System.Diagnostics;

namespace SchedulingSystem.Pages.Admin.Timetables;

public class DebugLessonMoveLiveModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DebugLessonMoveLiveModel> _logger;
    private readonly IHubContext<DebugRecursiveHub> _hubContext;

    public DebugLessonMoveLiveModel(
        ApplicationDbContext context,
        ILogger<DebugLessonMoveLiveModel> logger,
        IHubContext<DebugRecursiveHub> hubContext)
    {
        _context = context;
        _logger = logger;
        _hubContext = hubContext;
    }

    // Session ID for SignalR communication
    public string SessionId { get; set; } = string.Empty;

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

    public Task<IActionResult> OnGetAsync()
    {
        // Generate a unique session ID for this debug session
        SessionId = Guid.NewGuid().ToString();
        _logger.LogInformation("Created debug session {SessionId} for timetable {TimetableId}", SessionId, TimetableId);

        // Return the page immediately - the algorithm will run in the background
        // and send updates via SignalR
        return Task.FromResult<IActionResult>(Page());
    }

    // POST endpoint to start the algorithm in a background task
    public async Task<IActionResult> OnPostStartAsync([FromBody] string sessionId)
    {
        try
        {
            SessionId = sessionId;
            _logger.LogInformation("Starting background algorithm for session {SessionId}", SessionId);

            // Start the algorithm in a background task (fire and forget)
            _ = Task.Run(async () => await RunAlgorithmAsync(sessionId));

            return new JsonResult(new { success = true, message = "Algorithm started" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting algorithm");
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }

    private async Task RunAlgorithmAsync(string sessionId)
    {
        try
        {
            _logger.LogInformation("Running algorithm for session {SessionId}", sessionId);

            // Send starting message
            await _hubContext.Clients.Group(sessionId).SendAsync("AlgorithmStarted", new
            {
                sessionId,
                timestamp = DateTime.UtcNow
            });

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
                .Include(sl => sl.ScheduledLessonRooms)
                .Where(sl => sl.TimetableId == TimetableId)
                .ToListAsync();

            _logger.LogInformation("Loaded {Count} scheduled lessons for session {SessionId}", allLessons.Count, sessionId);

            // Get periods for this timetable
            var periods = await _context.Periods.ToListAsync();
            periods = periods.OrderBy(p => p.StartTime).ToList();

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
                IgnoredConstraints = IgnoredConstraintsList
            };

            // Execute the algorithm with SignalR updates
            var debugger = new SimpleLessonMoveDebugger(_context, _logger, _hubContext, sessionId);
            var result = await debugger.TryMoveLesson(request, allLessons, periods);

            // Send completion message
            await _hubContext.Clients.Group(sessionId).SendAsync("AlgorithmCompleted", new
            {
                sessionId,
                success = result.Success,
                message = result.Message,
                elapsedMs = result.ElapsedMs,
                solutionCount = result.Solutions?.Count ?? 0,
                timestamp = DateTime.UtcNow
            });

            _logger.LogInformation("Algorithm completed for session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running algorithm for session {SessionId}", sessionId);

            // Send error message
            await _hubContext.Clients.Group(sessionId).SendAsync("AlgorithmError", new
            {
                sessionId,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
