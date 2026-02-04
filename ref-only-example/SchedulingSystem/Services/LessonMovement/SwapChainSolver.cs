using SchedulingSystem.Data;
using SchedulingSystem.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SchedulingSystem.Services.LessonMovement
{
    /// <summary>
    /// Represents a single move in a swap chain
    /// </summary>
    public class MoveStep
    {
        public int ScheduledLessonId { get; set; }
        public string LessonDescription { get; set; } = "";
        public DayOfWeek FromDay { get; set; }
        public int FromPeriodId { get; set; }
        public string FromPeriodName { get; set; } = "";
        public int? FromRoomId { get; set; }
        public string? FromRoomName { get; set; }
        public DayOfWeek ToDay { get; set; }
        public int ToPeriodId { get; set; }
        public string ToPeriodName { get; set; } = "";
        public int? ToRoomId { get; set; }
        public string? ToRoomName { get; set; }
        public int StepNumber { get; set; }
    }

    /// <summary>
    /// Represents a complete swap chain solution
    /// </summary>
    public class SwapChain
    {
        public List<MoveStep> Steps { get; set; } = new();
        public int TotalMoves => Steps.Count;
        public double QualityScore { get; set; }
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Configuration for swap chain search
    /// </summary>
    public class SwapChainConfig
    {
        public int MaxDepth { get; set; } = 3;
        public int MaxSolutions { get; set; } = 5;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        public List<(DayOfWeek Day, int PeriodId)> ExcludeSlots { get; set; } = new();
        public List<string> IgnoredConstraintCodes { get; set; } = new();
    }

    /// <summary>
    /// Service to find multi-step swap chains to move a lesson to a desired slot
    /// Uses breadth-first search with pruning and optimization
    /// </summary>
    public class SwapChainSolver
    {
        private readonly ApplicationDbContext _context;
        private readonly AvailableSlotFinder _slotFinder;

        public SwapChainSolver(
            ApplicationDbContext context,
            AvailableSlotFinder slotFinder)
        {
            _context = context;
            _slotFinder = slotFinder;
        }

        /// <summary>
        /// Find swap chains to move a lesson to target slot
        /// </summary>
        public async Task<List<SwapChain>> FindSwapChainsAsync(
            int timetableId,
            int scheduledLessonId,
            DayOfWeek targetDay,
            int targetPeriodId,
            int? targetRoomId,
            SwapChainConfig? config = null)
        {
            config ??= new SwapChainConfig();

            var stopwatch = Stopwatch.StartNew();
            var solutions = new List<SwapChain>();
            var cts = new CancellationTokenSource(config.Timeout);

            try
            {
                // Get the lesson to move
                var sourceLesson = await _context.ScheduledLessons
                    .Include(sl => sl.Lesson)
                        .ThenInclude(l => l.LessonTeachers).ThenInclude(lt => lt.Teacher)
                    .Include(sl => sl.Lesson)
                        .ThenInclude(l => l.LessonClasses).ThenInclude(lc => lc.Class)
                    .Include(sl => sl.Lesson)
                        .ThenInclude(l => l.LessonSubjects).ThenInclude(ls => ls.Subject)
                    .Include(sl => sl.Lesson)
                        .ThenInclude(l => l.LessonAssignments).ThenInclude(la => la.Teacher)
                    .Include(sl => sl.Lesson)
                        .ThenInclude(l => l.LessonAssignments).ThenInclude(la => la.Subject)
                    .Include(sl => sl.Lesson)
                        .ThenInclude(l => l.LessonAssignments).ThenInclude(la => la.Class)
                    .Include(sl => sl.ScheduledLessonRooms).ThenInclude(slr => slr.Room)
                    .Include(sl => sl.ScheduledLessonRooms).ThenInclude(slr => slr.RoomAssignments).ThenInclude(ra => ra.LessonAssignment)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(sl => sl.Id == scheduledLessonId);

                if (sourceLesson == null)
                    throw new ArgumentException($"Scheduled lesson {scheduledLessonId} not found");

                // Check if lesson is locked
                if (sourceLesson.IsLocked)
                {
                    return new List<SwapChain>
                    {
                        new SwapChain
                        {
                            IsValid = false,
                            ErrorMessage = "Cannot move locked lesson"
                        }
                    };
                }

                // Check if target slot is in exclude list
                if (config.ExcludeSlots.Contains((targetDay, targetPeriodId)))
                {
                    return new List<SwapChain>
                    {
                        new SwapChain
                        {
                            IsValid = false,
                            ErrorMessage = "Target slot is in excluded slots list"
                        }
                    };
                }

                // Check if target slot is the same as current slot (no movement needed)
                if (sourceLesson.DayOfWeek == targetDay &&
                    sourceLesson.PeriodId == targetPeriodId)
                {
                    // Only allow if room is different (room change only)
                    bool isSameRoom = false;

                    if (targetRoomId.HasValue)
                    {
                        // Check if source lesson uses this room
                        isSameRoom = sourceLesson.RoomId == targetRoomId ||
                                    sourceLesson.ScheduledLessonRooms.Any(slr => slr.RoomId == targetRoomId);
                    }
                    else if (!sourceLesson.RoomId.HasValue && !sourceLesson.ScheduledLessonRooms.Any())
                    {
                        // Both source and target have no room
                        isSameRoom = true;
                    }

                    if (isSameRoom)
                    {
                        return new List<SwapChain>
                        {
                            new SwapChain
                            {
                                IsValid = false,
                                ErrorMessage = "Target slot is the same as current slot - no movement needed"
                            }
                        };
                    }
                }

                // First, check if we can move directly (no swap needed)
                var directMove = await TryDirectMoveAsync(
                    timetableId,
                    sourceLesson,
                    targetDay,
                    targetPeriodId,
                    targetRoomId,
                    config);

                if (directMove != null && directMove.IsValid)
                {
                    solutions.Add(directMove);
                    return solutions;
                }

                // Need to find swap chains
                solutions = await SearchSwapChainsAsync(
                    timetableId,
                    sourceLesson,
                    targetDay,
                    targetPeriodId,
                    targetRoomId,
                    config,
                    cts.Token);

                return solutions
                    .Where(s => s.IsValid)
                    .OrderBy(s => s.TotalMoves)
                    .ThenByDescending(s => s.QualityScore)
                    .Take(config.MaxSolutions)
                    .ToList();
            }
            catch (OperationCanceledException)
            {
                return new List<SwapChain>
                {
                    new SwapChain
                    {
                        IsValid = false,
                        ErrorMessage = $"Search timeout after {config.Timeout.TotalSeconds} seconds. Try limiting excluded slots or increasing timeout."
                    }
                };
            }
            finally
            {
                stopwatch.Stop();
                Console.WriteLine($"SwapChainSolver completed in {stopwatch.ElapsedMilliseconds}ms");
            }
        }

        /// <summary>
        /// Try direct move without any swaps
        /// </summary>
        private async Task<SwapChain?> TryDirectMoveAsync(
            int timetableId,
            ScheduledLesson sourceLesson,
            DayOfWeek targetDay,
            int targetPeriodId,
            int? targetRoomId,
            SwapChainConfig config)
        {
            // Check if target slot is available
            var availableSlots = await _slotFinder.FindAvailableSlotsAsync(
                timetableId,
                sourceLesson.LessonId,
                sourceLesson.Id,
                includeCurrentSlot: false,
                ignoredConstraintCodes: config.IgnoredConstraintCodes);

            var targetSlot = availableSlots.FirstOrDefault(s =>
                s.DayOfWeek == targetDay &&
                s.PeriodId == targetPeriodId &&
                s.RoomId == targetRoomId);

            if (targetSlot != null && !targetSlot.HasHardConstraintViolations)
            {
                var period = await _context.Periods.FindAsync(targetPeriodId);
                var room = targetRoomId.HasValue ? await _context.Rooms.FindAsync(targetRoomId.Value) : null;
                var fromPeriod = await _context.Periods.FindAsync(sourceLesson.PeriodId);
                var fromRoom = sourceLesson.RoomId.HasValue ? await _context.Rooms.FindAsync(sourceLesson.RoomId.Value) : null;

                return new SwapChain
                {
                    Steps = new List<MoveStep>
                    {
                        new MoveStep
                        {
                            ScheduledLessonId = sourceLesson.Id,
                            LessonDescription = GetLessonDescription(sourceLesson),
                            FromDay = sourceLesson.DayOfWeek,
                            FromPeriodId = sourceLesson.PeriodId,
                            FromPeriodName = fromPeriod?.Name ?? $"Period {sourceLesson.PeriodId}",
                            FromRoomId = sourceLesson.RoomId,
                            FromRoomName = fromRoom?.Name,
                            ToDay = targetDay,
                            ToPeriodId = targetPeriodId,
                            ToPeriodName = period?.Name ?? $"Period {targetPeriodId}",
                            ToRoomId = targetRoomId,
                            ToRoomName = room?.Name,
                            StepNumber = 1
                        }
                    },
                    QualityScore = targetSlot.QualityScore,
                    IsValid = true
                };
            }

            return null;
        }

        /// <summary>
        /// Search for swap chains using BFS
        /// </summary>
        private async Task<List<SwapChain>> SearchSwapChainsAsync(
            int timetableId,
            ScheduledLesson sourceLesson,
            DayOfWeek targetDay,
            int targetPeriodId,
            int? targetRoomId,
            SwapChainConfig config,
            CancellationToken cancellationToken)
        {
            var solutions = new List<SwapChain>();
            var queue = new Queue<SearchNode>();
            var visited = new HashSet<string>();

            // Initial state
            var initialNode = new SearchNode
            {
                MovedLessons = new Dictionary<int, (DayOfWeek Day, int PeriodId, int? RoomId)>(),
                Steps = new List<MoveStep>(),
                Depth = 0
            };

            queue.Enqueue(initialNode);

            while (queue.Count > 0 && solutions.Count < config.MaxSolutions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var currentNode = queue.Dequeue();

                // Check if we've reached max depth
                if (currentNode.Depth >= config.MaxDepth)
                    continue;

                // Get the lesson that needs to move
                ScheduledLesson lessonToMove;
                DayOfWeek desiredDay;
                int desiredPeriodId;
                int? desiredRoomId;

                if (currentNode.Depth == 0)
                {
                    // First move: source lesson to target
                    lessonToMove = sourceLesson;
                    desiredDay = targetDay;
                    desiredPeriodId = targetPeriodId;
                    desiredRoomId = targetRoomId;
                }
                else
                {
                    // Subsequent moves: need to find blocking lesson at target
                    var blockingLesson = await GetBlockingLessonAsync(
                        timetableId,
                        sourceLesson.LessonId,
                        targetDay,
                        targetPeriodId,
                        targetRoomId,
                        currentNode.MovedLessons);

                    if (blockingLesson == null)
                    {
                        // No blocking lesson - we found a solution!
                        var solution = await BuildSolutionAsync(currentNode, sourceLesson, targetDay, targetPeriodId, targetRoomId);
                        if (solution.IsValid)
                            solutions.Add(solution);
                        continue;
                    }

                    lessonToMove = blockingLesson;
                    desiredDay = targetDay;
                    desiredPeriodId = targetPeriodId;
                    desiredRoomId = targetRoomId;
                }

                // Skip locked lessons
                if (lessonToMove.IsLocked)
                    continue;

                // Find available slots for the blocking lesson
                var excludeSlots = config.ExcludeSlots.ToList();

                // Also exclude slots occupied by lessons in current chain
                excludeSlots.AddRange(currentNode.MovedLessons.Values.Select(v => (v.Day, v.PeriodId)));

                var availableSlots = await _slotFinder.FindAvailableSlotsAsync(
                    timetableId,
                    lessonToMove.LessonId,
                    lessonToMove.Id,
                    excludeSlots,
                    includeCurrentSlot: false);

                // Try each available slot
                foreach (var slot in availableSlots.Where(s => !s.HasHardConstraintViolations).Take(10))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Create new node with this move
                    var newNode = new SearchNode
                    {
                        MovedLessons = new Dictionary<int, (DayOfWeek, int, int?)>(currentNode.MovedLessons),
                        Steps = new List<MoveStep>(currentNode.Steps),
                        Depth = currentNode.Depth + 1
                    };

                    // Add this move
                    var period = await _context.Periods.FindAsync(slot.PeriodId);
                    var room = slot.RoomId.HasValue ? await _context.Rooms.FindAsync(slot.RoomId.Value) : null;
                    var fromPeriod = await _context.Periods.FindAsync(lessonToMove.PeriodId);
                    var fromRoom = lessonToMove.RoomId.HasValue ? await _context.Rooms.FindAsync(lessonToMove.RoomId.Value) : null;

                    var step = new MoveStep
                    {
                        ScheduledLessonId = lessonToMove.Id,
                        LessonDescription = GetLessonDescription(lessonToMove),
                        FromDay = lessonToMove.DayOfWeek,
                        FromPeriodId = lessonToMove.PeriodId,
                        FromPeriodName = fromPeriod?.Name ?? $"Period {lessonToMove.PeriodId}",
                        FromRoomId = lessonToMove.RoomId,
                        FromRoomName = fromRoom?.Name,
                        ToDay = slot.DayOfWeek,
                        ToPeriodId = slot.PeriodId,
                        ToPeriodName = period?.Name ?? $"Period {slot.PeriodId}",
                        ToRoomId = slot.RoomId,
                        ToRoomName = room?.Name,
                        StepNumber = newNode.Depth
                    };

                    newNode.Steps.Add(step);
                    newNode.MovedLessons[lessonToMove.Id] = (slot.DayOfWeek, slot.PeriodId, slot.RoomId);

                    // Check for cycles
                    var stateKey = GetStateKey(newNode);
                    if (visited.Contains(stateKey))
                        continue;

                    visited.Add(stateKey);
                    queue.Enqueue(newNode);
                }
            }

            return solutions;
        }

        /// <summary>
        /// Get the lesson blocking the target slot
        /// </summary>
        private async Task<ScheduledLesson?> GetBlockingLessonAsync(
            int timetableId,
            int lessonId,
            DayOfWeek day,
            int periodId,
            int? roomId,
            Dictionary<int, (DayOfWeek Day, int PeriodId, int? RoomId)> movedLessons)
        {
            // Get lesson details to check for conflicts
            var lesson = await _context.Lessons
                .Include(l => l.LessonTeachers)
                .Include(l => l.LessonClasses)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
                return null;

            var teacherIds = lesson.LessonTeachers.Select(lt => lt.TeacherId).ToList();
            var classIds = lesson.LessonClasses.Select(lc => lc.ClassId).ToList();

            // Find scheduled lessons at target slot
            var scheduledLessons = await _context.ScheduledLessons
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l.LessonTeachers)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l.LessonClasses)
                .Include(sl => sl.ScheduledLessonRooms)
                .Where(sl => sl.TimetableId == timetableId &&
                            sl.DayOfWeek == day &&
                            sl.PeriodId == periodId)
                .ToListAsync();

            // Check for conflicts considering moved lessons
            foreach (var scheduled in scheduledLessons)
            {
                // Skip if this lesson has already been moved in current chain
                if (movedLessons.ContainsKey(scheduled.Id))
                    continue;

                // Check teacher conflicts
                var scheduledTeacherIds = scheduled.Lesson.LessonTeachers.Select(lt => lt.TeacherId);
                if (teacherIds.Any(tid => scheduledTeacherIds.Contains(tid)))
                    return scheduled;

                // Check class conflicts
                var scheduledClassIds = scheduled.Lesson.LessonClasses.Select(lc => lc.ClassId);
                if (classIds.Any(cid => scheduledClassIds.Contains(cid)))
                    return scheduled;

                // Check room conflicts
                if (roomId.HasValue)
                {
                    if (scheduled.RoomId == roomId.Value)
                        return scheduled;
                    if (scheduled.ScheduledLessonRooms.Any(slr => slr.RoomId == roomId.Value))
                        return scheduled;
                }
            }

            return null;
        }

        /// <summary>
        /// Build final solution from search node
        /// </summary>
        private async Task<SwapChain> BuildSolutionAsync(
            SearchNode node,
            ScheduledLesson sourceLesson,
            DayOfWeek targetDay,
            int targetPeriodId,
            int? targetRoomId)
        {
            var steps = new List<MoveStep>(node.Steps);

            // Add final move of source lesson to target
            var period = await _context.Periods.FindAsync(targetPeriodId);
            var room = targetRoomId.HasValue ? await _context.Rooms.FindAsync(targetRoomId.Value) : null;
            var fromPeriod = await _context.Periods.FindAsync(sourceLesson.PeriodId);
            var fromRoom = sourceLesson.RoomId.HasValue ? await _context.Rooms.FindAsync(sourceLesson.RoomId.Value) : null;

            steps.Add(new MoveStep
            {
                ScheduledLessonId = sourceLesson.Id,
                LessonDescription = GetLessonDescription(sourceLesson),
                FromDay = sourceLesson.DayOfWeek,
                FromPeriodId = sourceLesson.PeriodId,
                FromPeriodName = fromPeriod?.Name ?? $"Period {sourceLesson.PeriodId}",
                FromRoomId = sourceLesson.RoomId,
                FromRoomName = fromRoom?.Name,
                ToDay = targetDay,
                ToPeriodId = targetPeriodId,
                ToPeriodName = period?.Name ?? $"Period {targetPeriodId}",
                ToRoomId = targetRoomId,
                ToRoomName = room?.Name,
                StepNumber = steps.Count + 1
            });

            return new SwapChain
            {
                Steps = steps,
                QualityScore = 100.0 - (steps.Count * 5.0), // Prefer fewer moves
                IsValid = true
            };
        }

        /// <summary>
        /// Get lesson description for display
        /// </summary>
        private string GetLessonDescription(ScheduledLesson scheduledLesson)
        {
            var lesson = scheduledLesson.Lesson;
            var teachers = string.Join(", ", lesson.LessonTeachers.Select(lt => lt.Teacher.Name));
            var classes = string.Join(", ", lesson.LessonClasses.Select(lc => lc.Class.Name));
            var subjects = string.Join(", ", lesson.LessonSubjects.Select(ls => ls.Subject.Code));

            return $"{subjects} - {teachers} - {classes}";
        }

        /// <summary>
        /// Generate unique state key for cycle detection
        /// </summary>
        private string GetStateKey(SearchNode node)
        {
            var positions = node.MovedLessons
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => $"{kvp.Key}:{kvp.Value.Day}:{kvp.Value.PeriodId}:{kvp.Value.RoomId}");

            return string.Join("|", positions);
        }

        /// <summary>
        /// Internal search node for BFS
        /// </summary>
        private class SearchNode
        {
            public Dictionary<int, (DayOfWeek Day, int PeriodId, int? RoomId)> MovedLessons { get; set; } = new();
            public List<MoveStep> Steps { get; set; } = new();
            public int Depth { get; set; }
        }
    }
}
