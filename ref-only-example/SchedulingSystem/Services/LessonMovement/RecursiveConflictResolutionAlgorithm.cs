using SchedulingSystem.Data;
using SchedulingSystem.Models;
using SchedulingSystem.Services.Constraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using SchedulingSystem.Hubs;

namespace SchedulingSystem.Services.LessonMovement
{
    /// <summary>
    /// Implements Recursive Conflict Resolution algorithm for timetable optimization.
    /// This algorithm tries to place a lesson in a target slot, identifies all conflicting lessons,
    /// and recursively finds alternative positions for each conflict.
    ///
    /// Key Features:
    /// - Handles multiple simultaneous conflicts (lessonsMToN)
    /// - Immutable state pattern to prevent side effects
    /// - Cycle prevention with visited tracking
    /// - Early exit heuristics for optimization
    /// - Memoization for repeated (lesson, slot) lookups
    /// </summary>
    public class RecursiveConflictResolutionAlgorithm
    {
        private readonly ApplicationDbContext _context;
        private readonly AvailableSlotFinder _slotFinder;
        private readonly IConstraintValidator _constraintValidator;
        private readonly ILogger<RecursiveConflictResolutionAlgorithm> _logger;
        private readonly IHubContext<DebugRecursiveHub> _debugHub;

        // Memoization cache: (lessonId, targetSlot) -> list of valid destinations
        private readonly Dictionary<(int, TimeSlot), List<TimeSlot>> _destinationCache = new();

        public RecursiveConflictResolutionAlgorithm(
            ApplicationDbContext context,
            AvailableSlotFinder slotFinder,
            IConstraintValidator constraintValidator,
            ILogger<RecursiveConflictResolutionAlgorithm> logger,
            IHubContext<DebugRecursiveHub> debugHub)
        {
            _context = context;
            _slotFinder = slotFinder;
            _constraintValidator = constraintValidator;
            _logger = logger;
            _debugHub = debugHub;
        }

        /// <summary>
        /// Find alternative placements with full debug information
        /// </summary>
        public async Task<RecursiveDebugResult> FindAlternativesWithDebugAsync(
            int timetableId,
            int selectedLessonId,
            int maxIterations = 1000,
            int maxSolutions = 50,
            List<string>? ignoredConstraints = null,
            CancellationToken cancellationToken = default)
        {
            var debugEvents = new List<RecursiveDebugEvent>();
            var solutions = await FindAlternativesInternalAsync(
                timetableId,
                selectedLessonId,
                maxIterations,
                maxSolutions,
                ignoredConstraints,
                cancellationToken,
                debugEvents);

            return new RecursiveDebugResult
            {
                Solutions = solutions.solutions,
                DebugEvents = debugEvents,
                NodesExplored = debugEvents.Count,
                MaxDepth = debugEvents.Any() ? debugEvents.Max(e => e.Depth) : 0,
                ElapsedMs = solutions.elapsedMs
            };
        }

        /// <summary>
        /// Find alternative placements using Recursive Conflict Resolution (backward compatible)
        /// </summary>
        public async Task<List<KempeChainSolution>> FindAlternativesAsync(
            int timetableId,
            int selectedLessonId,
            int maxIterations = 1000,
            int maxSolutions = 50,
            List<string>? ignoredConstraints = null,
            CancellationToken cancellationToken = default,
            string? debugSessionId = null)
        {
            var result = await FindAlternativesInternalAsync(
                timetableId,
                selectedLessonId,
                maxIterations,
                maxSolutions,
                ignoredConstraints,
                cancellationToken,
                null); // No debug events collection

            return result.solutions;
        }

        /// <summary>
        /// Internal method that does the actual work
        /// </summary>
        private async Task<(List<KempeChainSolution> solutions, long elapsedMs)> FindAlternativesInternalAsync(
            int timetableId,
            int selectedLessonId,
            int maxIterations,
            int maxSolutions,
            List<string>? ignoredConstraints,
            CancellationToken cancellationToken,
            List<RecursiveDebugEvent>? debugEvents)
        {
            // Start time tracking for global timeout enforcement
            var stopwatch = Stopwatch.StartNew();
            var maxTimeMs = maxIterations * 6; // Rough estimate: maxIterations * 6ms per iteration

            _logger.LogInformation($"Starting Recursive Conflict Resolution with {maxTimeMs}ms time limit");

            // Clear memoization cache for this run
            _destinationCache.Clear();

            // Load timetable data
            var allLessons = await LoadTimetableDataAsync(timetableId);
            var selectedLesson = allLessons.FirstOrDefault(l => l.Id == selectedLessonId);

            if (selectedLesson == null)
            {
                _logger.LogWarning($"Selected lesson {selectedLessonId} not found");
                return (new List<KempeChainSolution>(), 0);
            }

            // Store original positions to prevent returning lessons to start
            var originalPositions = allLessons.ToDictionary(
                l => l.Id,
                l => new TimeSlot(l.DayOfWeek, l.PeriodId));

            var currentSlot = new TimeSlot(selectedLesson.DayOfWeek, selectedLesson.PeriodId);

            // Build mapping of LessonID to all its original slots (prevents circular moves)
            var originalSlotsByLessonId = BuildOriginalSlotsByLessonId(allLessons);

            // Track the selected lesson's template ID and source slot
            var originalLessonTemplateId = selectedLesson.LessonId;
            var originalLessonSourceSlot = currentSlot;

            // Find all potential destination slots
            var destinations = await _slotFinder.FindAvailableSlotsAsync(
                timetableId,
                selectedLesson.LessonId,
                selectedLessonId,
                new List<(DayOfWeek, int)>(),
                includeCurrentSlot: false,
                ignoredConstraints);

            _logger.LogInformation($"Recursive Conflict Resolution: Found {destinations.Count} potential destination slots");

            var solutions = new List<KempeChainSolution>();
            int iterationsUsed = 0;

            // Build initial occupied slots map for conflict counting
            var initialOccupiedSlots = BuildOccupiedSlotsMap(allLessons);

            // Sort destinations by: 1) No hard violations, 2) Fewest conflicts, 3) Best quality
            // This minimizes recursion breadth by trying easier slots first
            var sortedDestinations = destinations
                .Where(d => !(d.DayOfWeek == currentSlot.Day && d.PeriodId == currentSlot.PeriodId))
                .Select(d => new
                {
                    Destination = d,
                    TargetSlot = new TimeSlot(d.DayOfWeek, d.PeriodId),
                    ConflictCount = CountConflictsInSlot(selectedLesson, new TimeSlot(d.DayOfWeek, d.PeriodId), allLessons, initialOccupiedSlots)
                })
                .OrderBy(x => x.Destination.HasHardConstraintViolations ? 1 : 0)  // No hard violations first
                .ThenBy(x => x.ConflictCount)                                       // Fewest conflicts second
                .ThenByDescending(x => x.Destination.QualityScore)                 // Best quality third
                .ToList();

            _logger.LogInformation($"Sorted {sortedDestinations.Count} destinations by conflict count (range: {sortedDestinations.FirstOrDefault()?.ConflictCount ?? 0} to {sortedDestinations.LastOrDefault()?.ConflictCount ?? 0} conflicts)");

            // Try each destination slot (sorted by fewest conflicts first)
            foreach (var item in sortedDestinations)
            {
                // Check time limit - return solutions found so far if time is up
                if (stopwatch.ElapsedMilliseconds >= maxTimeMs)
                {
                    _logger.LogInformation($"Time limit reached ({stopwatch.ElapsedMilliseconds}ms). Returning {solutions.Count} solutions found so far.");
                    break;
                }

                if (solutions.Count >= maxSolutions || iterationsUsed >= maxIterations)
                    break;

                // Check cancellation token but don't throw - just break and return what we have
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation($"Cancellation requested. Returning {solutions.Count} solutions found so far.");
                    break;
                }

                var destination = item.Destination;
                var targetSlot = item.TargetSlot;

                _logger.LogDebug($"Attempting slot {targetSlot} with {item.ConflictCount} conflicts (quality: {destination.QualityScore:F2})");

                // Create initial state
                var initialState = new RecursionState
                {
                    ProposedMoves = new Dictionary<int, TimeSlot>(),
                    VisitedLessons = new HashSet<int>(),
                    OccupiedSlots = BuildOccupiedSlotsMap(allLessons),
                    CurrentDepth = 0,
                    OriginalSelectedLessonId = selectedLessonId,
                    OriginalPositions = originalPositions,
                    IgnoredConstraints = ignoredConstraints ?? new List<string>(),
                    OriginalSlotsByLessonId = originalSlotsByLessonId,
                    OriginalLessonTemplateId = originalLessonTemplateId,
                    OriginalLessonSourceSlot = originalLessonSourceSlot
                };

                _logger.LogDebug($"Trying to move lesson {selectedLessonId} to {targetSlot}");

                // Try to recursively resolve conflicts
                var solution = await RecursiveResolveAsync(
                    selectedLesson,
                    targetSlot,
                    allLessons,
                    initialState,
                    maxDepth: 10, // Max recursion depth (corresponds to "max unfixed lessons to move")
                    stopwatch,
                    maxTimeMs,
                    cancellationToken,
                    debugEvents,
                    null); // No parent node for root attempts

                iterationsUsed++;

                if (solution != null && solution.Movements.Count > 0)
                {
                    // Check for duplicates
                    if (!IsDuplicateSolution(solutions, solution))
                    {
                        solutions.Add(solution);
                        _logger.LogInformation($"Found solution #{solutions.Count} with {solution.Movements.Count} movements");
                    }
                }
            }

            _logger.LogInformation($"Recursive Conflict Resolution: Found {solutions.Count} solutions after {iterationsUsed} iterations");

            var finalSolutions = solutions
                .OrderByDescending(s => s.QualityScore)
                .Take(maxSolutions)
                .ToList();

            return (finalSolutions, stopwatch.ElapsedMilliseconds);
        }

        /// <summary>
        /// Core recursive method to resolve conflicts
        /// </summary>
        private async Task<KempeChainSolution?> RecursiveResolveAsync(
            ScheduledLesson lessonToMove,
            TimeSlot targetSlot,
            List<ScheduledLesson> allLessons,
            RecursionState state,
            int maxDepth,
            Stopwatch stopwatch,
            long maxTimeMs,
            CancellationToken cancellationToken,
            List<RecursiveDebugEvent>? debugEvents = null,
            string? parentNodeId = null)
        {
            // Generate unique node ID for debug tree
            var nodeId = Guid.NewGuid().ToString();
            var currentOriginalSlot = state.OriginalPositions.TryGetValue(lessonToMove.Id, out var origSlot)
                ? origSlot
                : new TimeSlot(lessonToMove.DayOfWeek, lessonToMove.PeriodId);

            // === SAFETY CHECKS ===

            // 0. Check time limit - return null if time is up (graceful timeout)
            if (stopwatch.ElapsedMilliseconds >= maxTimeMs)
            {
                _logger.LogDebug($"Time limit reached in recursive call ({stopwatch.ElapsedMilliseconds}ms)");

                // Add debug event
                debugEvents?.Add(new RecursiveDebugEvent
                {
                    NodeId = nodeId,
                    ParentId = parentNodeId,
                    Type = "attempt",
                    Depth = state.CurrentDepth,
                    LessonId = lessonToMove.Id,
                    LessonDescription = GetLessonDescription(lessonToMove),
                    TargetSlot = targetSlot.ToString(),
                    OriginalPosition = currentOriginalSlot.ToString(),
                    VisitedLessons = state.VisitedLessons.ToArray(),
                    ProposedMoves = state.ProposedMoves.Select(kv => new ProposedMoveInfo
                    {
                        LessonId = kv.Key,
                        Slot = kv.Value.ToString()
                    }).ToList(),
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    Result = "timeout",
                    Message = "Time limit reached"
                });

                return null;
            }

            // 1. Check depth limit
            if (state.CurrentDepth >= maxDepth)
            {
                _logger.LogDebug($"Max depth {maxDepth} reached");

                debugEvents?.Add(new RecursiveDebugEvent
                {
                    NodeId = nodeId,
                    ParentId = parentNodeId,
                    Type = "attempt",
                    Depth = state.CurrentDepth,
                    LessonId = lessonToMove.Id,
                    LessonDescription = GetLessonDescription(lessonToMove),
                    TargetSlot = targetSlot.ToString(),
                    OriginalPosition = currentOriginalSlot.ToString(),
                    VisitedLessons = state.VisitedLessons.ToArray(),
                    ProposedMoves = state.ProposedMoves.Select(kv => new ProposedMoveInfo
                    {
                        LessonId = kv.Key,
                        Slot = kv.Value.ToString()
                    }).ToList(),
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    Result = "max_depth",
                    Message = $"Max depth {maxDepth} reached"
                });

                return null;
            }

            // 2. Prevent cycles - check if we've visited this lesson
            if (state.VisitedLessons.Contains(lessonToMove.Id))
            {
                _logger.LogDebug($"Cycle detected: lesson {lessonToMove.Id} already visited");

                debugEvents?.Add(new RecursiveDebugEvent
                {
                    NodeId = nodeId,
                    ParentId = parentNodeId,
                    Type = "attempt",
                    Depth = state.CurrentDepth,
                    LessonId = lessonToMove.Id,
                    LessonDescription = GetLessonDescription(lessonToMove),
                    TargetSlot = targetSlot.ToString(),
                    OriginalPosition = currentOriginalSlot.ToString(),
                    VisitedLessons = state.VisitedLessons.ToArray(),
                    ProposedMoves = state.ProposedMoves.Select(kv => new ProposedMoveInfo
                    {
                        LessonId = kv.Key,
                        Slot = kv.Value.ToString()
                    }).ToList(),
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    Result = "cycle",
                    Message = "Cycle detected"
                });

                return null;
            }

            // 3. Check if moving to original position (not allowed)
            if (state.OriginalPositions.TryGetValue(lessonToMove.Id, out var originalSlot) &&
                targetSlot.Equals(originalSlot) &&
                lessonToMove.Id != state.OriginalSelectedLessonId) // Allow selected lesson to return to original
            {
                _logger.LogDebug($"Cannot move lesson {lessonToMove.Id} back to original position");

                debugEvents?.Add(new RecursiveDebugEvent
                {
                    NodeId = nodeId,
                    ParentId = parentNodeId,
                    Type = "attempt",
                    Depth = state.CurrentDepth,
                    LessonId = lessonToMove.Id,
                    LessonDescription = GetLessonDescription(lessonToMove),
                    TargetSlot = targetSlot.ToString(),
                    OriginalPosition = currentOriginalSlot.ToString(),
                    VisitedLessons = state.VisitedLessons.ToArray(),
                    ProposedMoves = state.ProposedMoves.Select(kv => new ProposedMoveInfo
                    {
                        LessonId = kv.Key,
                        Slot = kv.Value.ToString()
                    }).ToList(),
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    Result = "original_position",
                    Message = "Cannot move back to original position"
                });

                return null;
            }

            // 3b. NEW CHECK: Prevent moving ANY instance of the original lesson's template back to the source slot
            // This prevents any instance of the same lesson from returning to where the selected lesson started
            if (lessonToMove.LessonId == state.OriginalLessonTemplateId &&
                targetSlot.Equals(state.OriginalLessonSourceSlot))
            {
                _logger.LogDebug($"Cannot move lesson template {lessonToMove.LessonId} back to original source slot {state.OriginalLessonSourceSlot}");

                debugEvents?.Add(new RecursiveDebugEvent
                {
                    NodeId = nodeId,
                    ParentId = parentNodeId,
                    Type = "attempt",
                    Depth = state.CurrentDepth,
                    LessonId = lessonToMove.Id,
                    LessonDescription = GetLessonDescription(lessonToMove),
                    TargetSlot = targetSlot.ToString(),
                    OriginalPosition = currentOriginalSlot.ToString(),
                    VisitedLessons = state.VisitedLessons.ToArray(),
                    ProposedMoves = state.ProposedMoves.Select(kv => new ProposedMoveInfo
                    {
                        LessonId = kv.Key,
                        Slot = kv.Value.ToString()
                    }).ToList(),
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    Result = "moving_to_source_slot",
                    Message = $"Cannot move lesson template {state.OriginalLessonTemplateId} back to source slot {state.OriginalLessonSourceSlot}"
                });

                return null;
            }

            // 4. Check if lesson is locked
            if (lessonToMove.IsLocked)
            {
                _logger.LogDebug($"Lesson {lessonToMove.Id} is locked, cannot move");

                debugEvents?.Add(new RecursiveDebugEvent
                {
                    NodeId = nodeId,
                    ParentId = parentNodeId,
                    Type = "attempt",
                    Depth = state.CurrentDepth,
                    LessonId = lessonToMove.Id,
                    LessonDescription = GetLessonDescription(lessonToMove),
                    TargetSlot = targetSlot.ToString(),
                    OriginalPosition = currentOriginalSlot.ToString(),
                    VisitedLessons = state.VisitedLessons.ToArray(),
                    ProposedMoves = state.ProposedMoves.Select(kv => new ProposedMoveInfo
                    {
                        LessonId = kv.Key,
                        Slot = kv.Value.ToString()
                    }).ToList(),
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    Result = "locked",
                    Message = "Lesson is locked"
                });

                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // === ADD TO VISITED AND PROPOSED MOVES ===
            // Create a new state with this lesson marked as visited and proposed
            var updatedState = state.Clone();
            updatedState.VisitedLessons.Add(lessonToMove.Id);
            updatedState.ProposedMoves[lessonToMove.Id] = targetSlot;

            // === FIND CONFLICTS IN TARGET SLOT ===

            var currentOccupiedSlots = ApplyProposedMoves(updatedState.OccupiedSlots, updatedState.ProposedMoves);
            var slotKey = targetSlot.ToString();

            var conflictingLessons = FindConflictingLessons(
                lessonToMove,
                targetSlot,
                allLessons,
                currentOccupiedSlots,
                updatedState.ProposedMoves);

            // === CASE 1: No conflicts - direct move ===
            if (conflictingLessons.Count == 0)
            {
                _logger.LogDebug($"Direct move: lesson {lessonToMove.Id} to {targetSlot} (no conflicts)");

                // Build movements from all proposed moves in the updated state
                var movements = new List<KempeChainMove>();

                foreach (var (lessonId, slot) in updatedState.ProposedMoves)
                {
                    var lesson = allLessons.First(l => l.Id == lessonId);
                    movements.Add(new KempeChainMove
                    {
                        ScheduledLessonId = lessonId,
                        LessonId = lesson.LessonId,
                        IsSelectedLesson = lessonId == updatedState.OriginalSelectedLessonId,
                        FromSlot = updatedState.OriginalPositions[lessonId],
                        ToSlot = slot,
                        LessonDescription = GetLessonDescription(lesson)
                    });
                }

                var qualityScore = CalculateQualityScore(movements.Count, updatedState.CurrentDepth);

                // Add debug event for success
                debugEvents?.Add(new RecursiveDebugEvent
                {
                    NodeId = nodeId,
                    ParentId = parentNodeId,
                    Type = "attempt",
                    Depth = state.CurrentDepth,
                    LessonId = lessonToMove.Id,
                    LessonDescription = GetLessonDescription(lessonToMove),
                    TargetSlot = targetSlot.ToString(),
                    OriginalPosition = currentOriginalSlot.ToString(),
                    Conflicts = 0,
                    ConflictingLessons = new List<ConflictingLessonInfo>(),
                    VisitedLessons = updatedState.VisitedLessons.ToArray(),
                    ProposedMoves = updatedState.ProposedMoves.Select(kv => new ProposedMoveInfo
                    {
                        LessonId = kv.Key,
                        Slot = kv.Value.ToString()
                    }).ToList(),
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    QualityScore = qualityScore,
                    Result = "success",
                    Message = "Direct move - no conflicts"
                });

                var solution = new KempeChainSolution
                {
                    Movements = movements,
                    ChainSize = movements.Count,
                    QualityScore = qualityScore
                };

                // Validate solution doesn't create circular moves
                if (!IsValidSolution(solution, updatedState, allLessons))
                {
                    _logger.LogDebug($"Solution rejected due to circular move validation");
                    return null;
                }

                return solution;
            }

            // === CASE 2: Conflicts exist - check if any are locked ===
            var lockedConflicts = conflictingLessons.Where(l => l.IsLocked).ToList();
            if (lockedConflicts.Any())
            {
                _logger.LogDebug($"Cannot resolve: {lockedConflicts.Count} locked lessons in target slot");

                // Add debug event
                debugEvents?.Add(new RecursiveDebugEvent
                {
                    NodeId = nodeId,
                    ParentId = parentNodeId,
                    Type = "attempt",
                    Depth = state.CurrentDepth,
                    LessonId = lessonToMove.Id,
                    LessonDescription = GetLessonDescription(lessonToMove),
                    TargetSlot = targetSlot.ToString(),
                    OriginalPosition = currentOriginalSlot.ToString(),
                    Conflicts = conflictingLessons.Count,
                    ConflictingLessons = conflictingLessons.Select(cl => new ConflictingLessonInfo
                    {
                        LessonId = cl.Id,
                        Description = GetLessonDescription(cl),
                        Slot = new TimeSlot(cl.DayOfWeek, cl.PeriodId).ToString(),
                        IsLocked = cl.IsLocked
                    }).ToList(),
                    VisitedLessons = updatedState.VisitedLessons.ToArray(),
                    ProposedMoves = updatedState.ProposedMoves.Select(kv => new ProposedMoveInfo
                    {
                        LessonId = kv.Key,
                        Slot = kv.Value.ToString()
                    }).ToList(),
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    Result = "locked_conflicts",
                    Message = $"{lockedConflicts.Count} locked lessons in target slot"
                });

                return null;
            }

            // === CASE 3: Resolve conflicts recursively ===
            _logger.LogDebug($"Found {conflictingLessons.Count} conflicting lessons in {targetSlot}");

            // Add debug event for conflicts found - this node will have children
            debugEvents?.Add(new RecursiveDebugEvent
            {
                NodeId = nodeId,
                ParentId = parentNodeId,
                Type = "attempt",
                Depth = state.CurrentDepth,
                LessonId = lessonToMove.Id,
                LessonDescription = GetLessonDescription(lessonToMove),
                TargetSlot = targetSlot.ToString(),
                OriginalPosition = currentOriginalSlot.ToString(),
                Conflicts = conflictingLessons.Count,
                ConflictingLessons = conflictingLessons.Select(cl => new ConflictingLessonInfo
                {
                    LessonId = cl.Id,
                    Description = GetLessonDescription(cl),
                    Slot = new TimeSlot(cl.DayOfWeek, cl.PeriodId).ToString(),
                    IsLocked = cl.IsLocked
                }).ToList(),
                VisitedLessons = updatedState.VisitedLessons.ToArray(),
                ProposedMoves = updatedState.ProposedMoves.Select(kv => new ProposedMoveInfo
                {
                    LessonId = kv.Key,
                    Slot = kv.Value.ToString()
                }).ToList(),
                ElapsedMs = stopwatch.ElapsedMilliseconds,
                Result = "resolving",
                Message = $"Resolving {conflictingLessons.Count} conflicts recursively..."
            });

            // EARLY EXIT HEURISTIC: Check if any conflicting lesson has no valid destinations
            foreach (var conflictingLesson in conflictingLessons)
            {
                var validDestinations = await FindValidDestinationsAsync(
                    conflictingLesson,
                    allLessons,
                    updatedState,
                    cancellationToken);

                if (validDestinations.Count == 0)
                {
                    _logger.LogDebug($"Early exit: Conflicting lesson {conflictingLesson.Id} has no valid destinations");
                    return null;
                }
            }

            // HEURISTIC: Sort conflicts by number of available destinations (most constrained first)
            var conflictsWithDestinations = new List<(ScheduledLesson Lesson, List<TimeSlot> Destinations)>();
            foreach (var conflict in conflictingLessons)
            {
                var dests = await FindValidDestinationsAsync(conflict, allLessons, updatedState, cancellationToken);
                conflictsWithDestinations.Add((conflict, dests));
            }

            // Sort: fewest destinations first (Most Constrained Variable heuristic)
            conflictsWithDestinations = conflictsWithDestinations
                .OrderBy(c => c.Destinations.Count)
                .ToList();

            // Try to resolve conflicts sequentially with backtracking
            var resolvedMoves = await ResolveConflictsSequentiallyAsync(
                conflictsWithDestinations,
                lessonToMove,
                targetSlot,
                allLessons,
                updatedState,
                maxDepth,
                stopwatch,
                maxTimeMs,
                cancellationToken,
                debugEvents,
                nodeId);

            if (resolvedMoves == null)
            {
                _logger.LogDebug($"Could not resolve all conflicts for lesson {lessonToMove.Id} to {targetSlot}");
                return null;
            }

            // Success! Build solution from all resolved moves
            // The resolvedMoves already include all the movements needed
            var finalSolution = new KempeChainSolution
            {
                Movements = resolvedMoves.DistinctBy(m => m.ScheduledLessonId).ToList(),
                ChainSize = resolvedMoves.Count,
                QualityScore = CalculateQualityScore(resolvedMoves.Count, updatedState.CurrentDepth)
            };

            // Validate solution doesn't create circular moves
            if (!IsValidSolution(finalSolution, updatedState, allLessons))
            {
                _logger.LogDebug($"Solution rejected due to circular move validation");
                return null;
            }

            return finalSolution;
        }

        /// <summary>
        /// Resolve multiple conflicts sequentially with backtracking (Approach A)
        /// </summary>
        private async Task<List<KempeChainMove>?> ResolveConflictsSequentiallyAsync(
            List<(ScheduledLesson Lesson, List<TimeSlot> Destinations)> conflictsWithDestinations,
            ScheduledLesson originalLesson,
            TimeSlot originalTarget,
            List<ScheduledLesson> allLessons,
            RecursionState state,
            int maxDepth,
            Stopwatch stopwatch,
            long maxTimeMs,
            CancellationToken cancellationToken,
            List<RecursiveDebugEvent>? debugEvents = null,
            string? parentNodeId = null)
        {
            // Check time limit before processing
            if (stopwatch.ElapsedMilliseconds >= maxTimeMs)
            {
                _logger.LogDebug($"Time limit reached in conflict resolution");
                return null;
            }

            // Base case: no more conflicts to resolve
            if (conflictsWithDestinations.Count == 0)
                return new List<KempeChainMove>();

            // Take first conflict
            var (conflictLesson, destinations) = conflictsWithDestinations[0];
            var remainingConflicts = conflictsWithDestinations.Skip(1).ToList();

            // Try each destination for this conflict
            foreach (var destinationSlot in destinations.Take(10)) // Limit to top 10 to prevent explosion
            {
                // Check time limit and cancellation - return null if time is up
                if (stopwatch.ElapsedMilliseconds >= maxTimeMs || cancellationToken.IsCancellationRequested)
                {
                    _logger.LogDebug($"Time limit or cancellation in conflict loop");
                    return null;
                }

                // Check if this conflict was already moved in a previous step
                if (state.ProposedMoves.ContainsKey(conflictLesson.Id))
                {
                    _logger.LogDebug($"Conflict lesson {conflictLesson.Id} already has a proposed move, skipping");
                    continue;
                }

                // Create new state - DON'T add to VisitedLessons yet, RecursiveResolveAsync will do that
                // Increment depth: we're going one level deeper in conflict resolution cascade
                // This tracks how many "hops" away from the original selected lesson we are
                var newState = state.Clone(newDepth: state.CurrentDepth + 1);

                // Recursively resolve this conflict
                // RecursiveResolveAsync will add the lesson to VisitedLessons internally
                var subSolution = await RecursiveResolveAsync(
                    conflictLesson,
                    destinationSlot,
                    allLessons,
                    newState,
                    maxDepth,
                    stopwatch,
                    maxTimeMs,
                    cancellationToken,
                    debugEvents,
                    parentNodeId); // parentNodeId is the node that's resolving conflicts

                if (subSolution != null)
                {
                    // Successfully resolved this conflict, try remaining conflicts
                    var remainingSolutions = await ResolveConflictsSequentiallyAsync(
                        remainingConflicts,
                        originalLesson,
                        originalTarget,
                        allLessons,
                        newState,
                        maxDepth,
                        stopwatch,
                        maxTimeMs,
                        cancellationToken,
                        debugEvents,
                        parentNodeId);

                    if (remainingSolutions != null)
                    {
                        // Success! All conflicts resolved
                        var allMoves = subSolution.Movements.ToList();
                        allMoves.AddRange(remainingSolutions);
                        return allMoves;
                    }
                }

                // This destination didn't work, try next one (backtracking)
            }

            // Could not resolve this conflict with any destination
            return null;
        }

        /// <summary>
        /// Count how many lessons would conflict with placing lessonToMove in targetSlot
        /// Used for prioritizing destination slots with fewer conflicts
        /// </summary>
        private int CountConflictsInSlot(
            ScheduledLesson lessonToMove,
            TimeSlot targetSlot,
            List<ScheduledLesson> allLessons,
            Dictionary<string, HashSet<int>> occupiedSlots)
        {
            var conflicts = FindConflictingLessons(
                lessonToMove,
                targetSlot,
                allLessons,
                occupiedSlots,
                new Dictionary<int, TimeSlot>());  // No proposed moves for initial count

            return conflicts.Count;
        }

        /// <summary>
        /// Find all lessons that would conflict with placing lessonToMove in targetSlot
        /// </summary>
        private List<ScheduledLesson> FindConflictingLessons(
            ScheduledLesson lessonToMove,
            TimeSlot targetSlot,
            List<ScheduledLesson> allLessons,
            Dictionary<string, HashSet<int>> occupiedSlots,
            Dictionary<int, TimeSlot> proposedMoves)
        {
            var conflicts = new HashSet<ScheduledLesson>();
            var slotKey = targetSlot.ToString();

            // Get lessons currently in the target slot (considering proposed moves)
            var lessonsInSlot = new List<ScheduledLesson>();

            if (occupiedSlots.TryGetValue(slotKey, out var lessonIds))
            {
                foreach (var lessonId in lessonIds)
                {
                    var lesson = allLessons.FirstOrDefault(l => l.Id == lessonId);
                    if (lesson != null && lesson.Id != lessonToMove.Id)
                    {
                        lessonsInSlot.Add(lesson);
                    }
                }
            }

            // Check for resource conflicts (teacher, class, room)
            var teachers = lessonToMove.Lesson.LessonTeachers.Select(lt => lt.TeacherId).ToHashSet();
            var classes = lessonToMove.Lesson.LessonClasses.Select(lc => lc.ClassId).ToHashSet();
            var rooms = new HashSet<int?> { lessonToMove.RoomId };

            // Add multi-room assignments
            foreach (var slr in lessonToMove.ScheduledLessonRooms)
            {
                rooms.Add(slr.RoomId);
            }

            foreach (var otherLesson in lessonsInSlot)
            {
                // Check teacher conflicts
                var otherTeachers = otherLesson.Lesson.LessonTeachers.Select(lt => lt.TeacherId).ToHashSet();
                if (teachers.Overlaps(otherTeachers))
                {
                    conflicts.Add(otherLesson);
                    continue;
                }

                // Check class conflicts
                var otherClasses = otherLesson.Lesson.LessonClasses.Select(lc => lc.ClassId).ToHashSet();
                if (classes.Overlaps(otherClasses))
                {
                    conflicts.Add(otherLesson);
                    continue;
                }

                // Check room conflicts
                var otherRooms = new HashSet<int?> { otherLesson.RoomId };
                foreach (var slr in otherLesson.ScheduledLessonRooms)
                {
                    otherRooms.Add(slr.RoomId);
                }

                if (rooms.Overlaps(otherRooms) && rooms.Any(r => r.HasValue))
                {
                    conflicts.Add(otherLesson);
                }
            }

            return conflicts.ToList();
        }

        /// <summary>
        /// Find valid destinations for a lesson using memoization
        /// </summary>
        private async Task<List<TimeSlot>> FindValidDestinationsAsync(
            ScheduledLesson lesson,
            List<ScheduledLesson> allLessons,
            RecursionState state,
            CancellationToken cancellationToken)
        {
            var currentSlot = new TimeSlot(lesson.DayOfWeek, lesson.PeriodId);

            // Check memoization cache
            var cacheKey = (lesson.Id, currentSlot);
            if (_destinationCache.TryGetValue(cacheKey, out var cachedDestinations))
            {
                return cachedDestinations;
            }

            // Find available slots
            var availableSlots = await _slotFinder.FindAvailableSlotsAsync(
                lesson.TimetableId ?? 0,
                lesson.LessonId,
                lesson.Id,
                new List<(DayOfWeek, int)>(),
                includeCurrentSlot: false,
                state.IgnoredConstraints);

            // Get current occupied slots for conflict counting
            var currentOccupiedSlots = ApplyProposedMoves(state.OccupiedSlots, state.ProposedMoves);

            // Sort by: 1) No hard violations, 2) Fewest conflicts, 3) Best quality
            // This ensures we try easier slots first at all recursion levels
            var destinations = availableSlots
                .Where(s => !s.HasHardConstraintViolations)
                .Where(s => s.DayOfWeek != currentSlot.Day || s.PeriodId != currentSlot.PeriodId)
                .Where(s => !state.OriginalPositions.TryGetValue(lesson.Id, out var orig) || !new TimeSlot(s.DayOfWeek, s.PeriodId).Equals(orig))
                .Select(s => new
                {
                    Slot = new TimeSlot(s.DayOfWeek, s.PeriodId),
                    Quality = s.QualityScore,
                    ConflictCount = CountConflictsInSlot(lesson, new TimeSlot(s.DayOfWeek, s.PeriodId), allLessons, currentOccupiedSlots)
                })
                .OrderBy(x => x.ConflictCount)          // Fewest conflicts first
                .ThenByDescending(x => x.Quality)        // Best quality second
                .Select(x => x.Slot)
                .ToList();

            // Cache result
            _destinationCache[cacheKey] = destinations;

            return destinations;
        }

        /// <summary>
        /// Build a map of occupied slots from current lesson positions
        /// </summary>
        private Dictionary<string, HashSet<int>> BuildOccupiedSlotsMap(List<ScheduledLesson> allLessons)
        {
            var map = new Dictionary<string, HashSet<int>>();

            foreach (var lesson in allLessons)
            {
                var slotKey = new TimeSlot(lesson.DayOfWeek, lesson.PeriodId).ToString();

                if (!map.ContainsKey(slotKey))
                    map[slotKey] = new HashSet<int>();

                map[slotKey].Add(lesson.Id);
            }

            return map;
        }

        /// <summary>
        /// Apply proposed moves to occupied slots map (virtual state)
        /// Optimized: Only removes from original slot instead of scanning all slots
        /// </summary>
        private Dictionary<string, HashSet<int>> ApplyProposedMoves(
            Dictionary<string, HashSet<int>> occupiedSlots,
            Dictionary<int, TimeSlot> proposedMoves)
        {
            var result = new Dictionary<string, HashSet<int>>();

            // Copy original slots
            foreach (var (key, lessons) in occupiedSlots)
            {
                result[key] = new HashSet<int>(lessons);
            }

            // Apply proposed moves efficiently
            foreach (var (lessonId, newSlot) in proposedMoves)
            {
                // Find and remove from original slot
                // Note: We could optimize this further by tracking original positions,
                // but for now we search (still better than removing from all slots)
                string? originalKey = null;
                foreach (var (key, lessons) in result)
                {
                    if (lessons.Contains(lessonId))
                    {
                        originalKey = key;
                        lessons.Remove(lessonId);
                        break; // Lesson can only be in one slot originally
                    }
                }

                // Add to new slot
                var newKey = newSlot.ToString();
                if (!result.ContainsKey(newKey))
                    result[newKey] = new HashSet<int>();

                result[newKey].Add(lessonId);
            }

            return result;
        }

        /// <summary>
        /// Check if solution is duplicate
        /// </summary>
        private bool IsDuplicateSolution(List<KempeChainSolution> existingSolutions, KempeChainSolution newSolution)
        {
            var newSignature = string.Join("|", newSolution.Movements
                .OrderBy(m => m.ScheduledLessonId)
                .Select(m => $"{m.ScheduledLessonId}:{m.ToSlot}"));

            foreach (var existing in existingSolutions)
            {
                var existingSignature = string.Join("|", existing.Movements
                    .OrderBy(m => m.ScheduledLessonId)
                    .Select(m => $"{m.ScheduledLessonId}:{m.ToSlot}"));

                if (newSignature == existingSignature)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Calculate quality score for a solution
        /// </summary>
        private double CalculateQualityScore(int movementCount, int depth)
        {
            // Prefer fewer movements and shallower depth
            return 1000.0 / (movementCount + 1) - (depth * 10);
        }

        /// <summary>
        /// Get lesson description for display
        /// </summary>
        private string GetLessonDescription(ScheduledLesson lesson)
        {
            var subjects = string.Join(", ", lesson.Lesson.LessonSubjects
                .OrderByDescending(ls => ls.IsPrimary)
                .Select(ls => ls.Subject.Name));

            var classes = string.Join(", ", lesson.Lesson.LessonClasses
                .OrderByDescending(lc => lc.IsPrimary)
                .Select(lc => lc.Class.Name));

            return $"{subjects} - {classes}";
        }

        /// <summary>
        /// Build mapping of LessonID to all its original time slots
        /// This prevents circular moves where a lesson ends up back in any of its original positions
        /// </summary>
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

        /// <summary>
        /// Validate that a solution doesn't create circular moves
        /// Checks that no lesson (by LessonID template) ends up in ANY of its original time slots
        /// </summary>
        private bool IsValidSolution(KempeChainSolution solution, RecursionState state, List<ScheduledLesson> allLessons)
        {
            // Create lookup for scheduled lesson ID to lesson ID
            var scheduledLessonToLessonId = allLessons.ToDictionary(sl => sl.Id, sl => sl.LessonId);

            foreach (var movement in solution.Movements)
            {
                // Get the LessonID for this ScheduledLesson
                if (!scheduledLessonToLessonId.TryGetValue(movement.ScheduledLessonId, out var lessonId))
                    continue;

                // Get the destination slot from the movement (ToSlot is already a TimeSlot object)
                var destinationSlot = movement.ToSlot;

                // Check if this LessonID was originally in the destination slot
                if (state.OriginalSlotsByLessonId.TryGetValue(lessonId, out var originalSlots))
                {
                    if (originalSlots.Any(slot => slot.Equals(destinationSlot)))
                    {
                        _logger.LogDebug($"Solution rejected: LessonID {lessonId} (ScheduledLesson {movement.ScheduledLessonId}) would be placed back into original slot {destinationSlot}");
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Load timetable data with all relationships
        /// </summary>
        private async Task<List<ScheduledLesson>> LoadTimetableDataAsync(int timetableId)
        {
            return await _context.ScheduledLessons
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l.LessonTeachers)
                        .ThenInclude(lt => lt.Teacher)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l.LessonClasses)
                        .ThenInclude(lc => lc.Class)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l.LessonSubjects)
                        .ThenInclude(ls => ls.Subject)
                .Include(sl => sl.Room)
                .Include(sl => sl.Period)
                .Include(sl => sl.ScheduledLessonRooms)
                    .ThenInclude(slr => slr.Room)
                .Where(sl => sl.TimetableId == timetableId)
                .AsSplitQuery()
                .ToListAsync();
        }
    }

    /// <summary>
    /// Immutable state for recursion - prevents side effects
    /// </summary>
    public class RecursionState
    {
        // Proposed moves: lessonId -> new TimeSlot
        public Dictionary<int, TimeSlot> ProposedMoves { get; init; } = new();

        // Lessons already visited in this branch (cycle prevention)
        public HashSet<int> VisitedLessons { get; init; } = new();

        // Current occupied slots: slotKey -> set of lessonIds
        public Dictionary<string, HashSet<int>> OccupiedSlots { get; init; } = new();

        // Current recursion depth (how many levels of conflict resolution we've gone through)
        // Depth 0 = original selected lesson
        // Depth 1 = resolving conflicts of the original lesson
        // Depth 2 = resolving conflicts of conflicts, etc.
        public int CurrentDepth { get; init; }

        // Original selected lesson ID (to track which lesson initiated the search)
        public int OriginalSelectedLessonId { get; init; }

        // Original positions of all lessons (to prevent returning to start)
        public Dictionary<int, TimeSlot> OriginalPositions { get; init; } = new();

        // Constraints to ignore during validation
        public List<string> IgnoredConstraints { get; init; } = new();

        // NEW: Track all original slots where each LessonID (template) was scheduled
        // Key: LessonID, Value: List of original time slots for that lesson template
        public Dictionary<int, List<TimeSlot>> OriginalSlotsByLessonId { get; init; } = new();

        // NEW: Track the LessonID (template) of the initial selected lesson
        public int OriginalLessonTemplateId { get; init; }

        // NEW: Track the source slot of the initial selected lesson
        // We prevent moving ANY instance of OriginalLessonTemplateId back to this slot
        public TimeSlot OriginalLessonSourceSlot { get; init; } = new TimeSlot(DayOfWeek.Sunday, 0);

        /// <summary>
        /// Clone state for new recursion branch (immutable pattern)
        /// </summary>
        public RecursionState Clone(int? newDepth = null)
        {
            return new RecursionState
            {
                ProposedMoves = new Dictionary<int, TimeSlot>(ProposedMoves),
                VisitedLessons = new HashSet<int>(VisitedLessons),
                OccupiedSlots = OccupiedSlots.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new HashSet<int>(kvp.Value)),
                CurrentDepth = newDepth ?? CurrentDepth,
                OriginalSelectedLessonId = OriginalSelectedLessonId,
                OriginalPositions = OriginalPositions, // Shared reference OK (never modified)
                IgnoredConstraints = IgnoredConstraints, // Shared reference OK (never modified)
                OriginalSlotsByLessonId = OriginalSlotsByLessonId, // Shared reference OK (never modified)
                OriginalLessonTemplateId = OriginalLessonTemplateId,
                OriginalLessonSourceSlot = OriginalLessonSourceSlot
            };
        }
    }
}
