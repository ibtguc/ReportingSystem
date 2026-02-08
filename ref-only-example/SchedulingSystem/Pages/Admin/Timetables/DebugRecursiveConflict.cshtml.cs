using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;
using SchedulingSystem.Services.LessonMovement;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchedulingSystem.Pages.Admin.Timetables
{
    [Authorize]
    public class DebugRecursiveConflictModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly RecursiveConflictResolutionAlgorithm _algorithm;

        public DebugRecursiveConflictModel(
            ApplicationDbContext context,
            RecursiveConflictResolutionAlgorithm algorithm)
        {
            _context = context;
            _algorithm = algorithm;
        }

        // Input parameters
        public int TimetableId { get; set; }
        public string SelectedLessonIds { get; set; } = string.Empty;
        public string Algorithm { get; set; } = "recursive-conflict-resolution";
        public int MaxDepth { get; set; } = 10;
        public int MaxTimeMinutes { get; set; } = 3;
        public string IgnoredConstraints { get; set; } = string.Empty;
        public string DestinationDay { get; set; } = string.Empty;
        public int? DestinationPeriodId { get; set; }
        public string AvoidSlotsSelected { get; set; } = string.Empty;
        public string AvoidSlotsUnlocked { get; set; } = string.Empty;

        // Data for display
        public Timetable? Timetable { get; set; }
        public List<ScheduledLesson> SelectedLessons { get; set; } = new();
        public string SessionId { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(
            int timetableId,
            string selectedLessonIds,
            string algorithm = "recursive-conflict-resolution",
            int maxDepth = 10,
            int maxTimeMinutes = 3,
            string ignoredConstraints = "",
            string destinationDay = "",
            int? destinationPeriodId = null,
            string avoidSlotsSelected = "",
            string avoidSlotsUnlocked = "")
        {
            // Store parameters
            TimetableId = timetableId;
            SelectedLessonIds = selectedLessonIds;
            Algorithm = algorithm;
            MaxDepth = maxDepth;
            MaxTimeMinutes = maxTimeMinutes;
            IgnoredConstraints = ignoredConstraints;
            DestinationDay = destinationDay;
            DestinationPeriodId = destinationPeriodId;
            AvoidSlotsSelected = avoidSlotsSelected;
            AvoidSlotsUnlocked = avoidSlotsUnlocked;

            // Generate session ID for SignalR
            SessionId = Guid.NewGuid().ToString();

            // Load timetable
            Timetable = await _context.Timetables
                .FirstOrDefaultAsync(t => t.Id == timetableId);

            if (Timetable == null)
            {
                return NotFound();
            }

            // Load selected lessons
            var lessonIds = selectedLessonIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => int.TryParse(id, out var parsed) ? parsed : 0)
                .Where(id => id > 0)
                .ToList();

            SelectedLessons = await _context.ScheduledLessons
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l.LessonSubjects)
                        .ThenInclude(ls => ls.Subject)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l.LessonClasses)
                        .ThenInclude(lc => lc.Class)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l.LessonTeachers)
                        .ThenInclude(lt => lt.Teacher)
                .Include(sl => sl.Period)
                .Where(sl => lessonIds.Contains(sl.Id))
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostStartDebugAsync([FromBody] DebugRequest request)
        {
            try
            {
                // Parse selected lesson IDs
                var lessonIds = request.SelectedLessonIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => int.TryParse(id, out var parsed) ? parsed : 0)
                    .Where(id => id > 0)
                    .ToList();

                if (!lessonIds.Any())
                {
                    return BadRequest(new { error = "No valid lesson IDs provided" });
                }

                // Parse ignored constraints
                var ignoredConstraints = string.IsNullOrEmpty(request.IgnoredConstraints)
                    ? new List<string>()
                    : request.IgnoredConstraints.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

                // Calculate maxIterations from time limit
                int maxIterations = request.MaxTimeMinutes * 60 * 1000 / 6;

                // Run the algorithm synchronously and collect all debug events
                var result = await _algorithm.FindAlternativesWithDebugAsync(
                    timetableId: request.TimetableId,
                    selectedLessonId: lessonIds.First(), // Use first lesson as the selected lesson
                    maxIterations: maxIterations,
                    maxSolutions: 50,
                    ignoredConstraints: ignoredConstraints,
                    cancellationToken: default);

                // Return the complete debug result
                return new JsonResult(new
                {
                    success = true,
                    nodesExplored = result.NodesExplored,
                    maxDepth = result.MaxDepth,
                    solutionsFound = result.Solutions.Count,
                    elapsedMs = result.ElapsedMs,
                    debugEvents = result.DebugEvents
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        public class DebugRequest
        {
            public string SessionId { get; set; } = string.Empty;
            public int TimetableId { get; set; }
            public string SelectedLessonIds { get; set; } = string.Empty;
            public int MaxDepth { get; set; }
            public int MaxTimeMinutes { get; set; }
            public string IgnoredConstraints { get; set; } = string.Empty;
        }
    }
}
