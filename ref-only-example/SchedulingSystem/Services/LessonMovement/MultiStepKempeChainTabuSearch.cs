using SchedulingSystem.Data;
using SchedulingSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SchedulingSystem.Services.LessonMovement
{
    /// <summary>
    /// Implements Multi-Step Kempe Chain + Tabu Search for timetable optimization.
    /// Unlike the basic Kempe Chain which only swaps between 2 timeslots,
    /// this variation explores cascading movements across 3+ timeslots.
    /// </summary>
    public class MultiStepKempeChainTabuSearch
    {
        private readonly ApplicationDbContext _context;
        private readonly AvailableSlotFinder _slotFinder;
        private readonly ILogger<MultiStepKempeChainTabuSearch> _logger;

        public MultiStepKempeChainTabuSearch(
            ApplicationDbContext context,
            AvailableSlotFinder slotFinder,
            ILogger<MultiStepKempeChainTabuSearch> logger)
        {
            _context = context;
            _slotFinder = slotFinder;
            _logger = logger;
        }

        /// <summary>
        /// Find alternative placements using multi-step Kempe chains
        /// </summary>
        public async Task<List<KempeChainSolution>> FindAlternativesAsync(
            int timetableId,
            int selectedLessonId,
            int maxIterations = 1000,
            int maxSolutions = 50,
            List<string>? ignoredConstraints = null,
            CancellationToken cancellationToken = default)
        {
            // Load timetable data
            var allLessons = await LoadTimetableDataAsync(timetableId);
            var selectedLesson = allLessons.FirstOrDefault(l => l.Id == selectedLessonId);

            if (selectedLesson == null)
                return new List<KempeChainSolution>();

            // Store original positions of all lessons
            var originalPositions = allLessons.ToDictionary(
                l => l.Id,
                l => new TimeSlot(l.DayOfWeek, l.PeriodId));

            // Build conflict graph
            var graph = BuildConflictGraph(allLessons);

            // Get current slot
            var currentSlot = new TimeSlot(selectedLesson.DayOfWeek, selectedLesson.PeriodId);

            // Find available destination slots
            var destinations = await _slotFinder.FindAvailableSlotsAsync(
                timetableId,
                selectedLesson.LessonId,
                selectedLessonId,
                new List<(DayOfWeek, int)>(),
                includeCurrentSlot: false,
                ignoredConstraints);

            // Filter out current slot and sort by quality
            var targetSlots = destinations
                .Where(d => d.DayOfWeek != currentSlot.Day || d.PeriodId != currentSlot.PeriodId)
                .OrderByDescending(d => d.QualityScore)
                .Select(d => new TimeSlot(d.DayOfWeek, d.PeriodId))
                .ToList();

            _logger.LogInformation($"Multi-Step Kempe Chain: Found {targetSlots.Count} potential destination slots to explore");

            var solutions = new List<KempeChainSolution>();
            int chainsExplored = 0;
            int chainsSkippedTooLarge = 0;
            int chainsSkippedOriginalPosition = 0;
            int solutionsCreated = 0;
            int duplicatesSkipped = 0;

            // For each promising destination, explore multi-step chains
            foreach (var targetSlot in targetSlots)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (solutions.Count >= maxSolutions)
                {
                    _logger.LogInformation($"Multi-Step Kempe Chain: Reached maxSolutions limit ({maxSolutions})");
                    break;
                }

                // Try to find multi-step path from current to target slot
                var multiStepSolutions = FindMultiStepPaths(
                    selectedLessonId,
                    currentSlot,
                    targetSlot,
                    allLessons,
                    graph,
                    originalPositions,
                    maxDepth: 5); // Allow chains with up to 5 steps

                chainsExplored++;

                foreach (var solution in multiStepSolutions)
                {
                    if (solutions.Count >= maxSolutions)
                        break;

                    // Check chain size limit
                    if (solution.Movements.Count > 50)
                    {
                        chainsSkippedTooLarge++;
                        continue;
                    }

                    // Check for duplicates
                    if (!IsDuplicateSolution(solutions, solution))
                    {
                        solutions.Add(solution);
                        solutionsCreated++;
                    }
                    else
                    {
                        duplicatesSkipped++;
                    }
                }
            }

            _logger.LogInformation($"Multi-Step Kempe Chain Summary: Explored {chainsExplored} target slots, " +
                $"Skipped {chainsSkippedTooLarge} too large chains, " +
                $"Created {solutionsCreated} unique solutions, " +
                $"Skipped {duplicatesSkipped} duplicates");

            return solutions
                .OrderByDescending(s => s.QualityScore)
                .Take(maxSolutions)
                .ToList();
        }

        /// <summary>
        /// Find multi-step paths from current slot to target slot using iterative deepening
        /// </summary>
        private List<KempeChainSolution> FindMultiStepPaths(
            int selectedLessonId,
            TimeSlot currentSlot,
            TimeSlot targetSlot,
            List<ScheduledLesson> allLessons,
            ConflictGraph graph,
            Dictionary<int, TimeSlot> originalPositions,
            int maxDepth)
        {
            var solutions = new List<KempeChainSolution>();

            // Try different chain depths (2, 3, 4, 5 steps)
            for (int depth = 2; depth <= maxDepth; depth++)
            {
                var depthSolutions = ExploreChainDepth(
                    selectedLessonId,
                    currentSlot,
                    targetSlot,
                    depth,
                    allLessons,
                    graph,
                    originalPositions);

                solutions.AddRange(depthSolutions);

                // If we found solutions at this depth, we might continue to explore deeper
                // to find more diverse solutions
                if (solutions.Count >= 3)
                    break; // Found enough solutions for this target
            }

            return solutions;
        }

        /// <summary>
        /// Explore chains of a specific depth
        /// </summary>
        private List<KempeChainSolution> ExploreChainDepth(
            int selectedLessonId,
            TimeSlot startSlot,
            TimeSlot targetSlot,
            int depth,
            List<ScheduledLesson> allLessons,
            ConflictGraph graph,
            Dictionary<int, TimeSlot> originalPositions)
        {
            var solutions = new List<KempeChainSolution>();

            // Get all available slots (excluding start and target)
            var allSlots = allLessons
                .Select(l => new TimeSlot(l.DayOfWeek, l.PeriodId))
                .Distinct()
                .Where(s => !s.Equals(startSlot) && !s.Equals(targetSlot))
                .ToList();

            // For depth 2: Simple 2-way swap (start <-> target)
            if (depth == 2)
            {
                var chain = graph.ExtractKempeChain(selectedLessonId, startSlot, targetSlot);
                var solution = CreateSolutionFromChain(
                    chain,
                    selectedLessonId,
                    allLessons,
                    graph,
                    originalPositions);

                if (solution != null)
                    solutions.Add(solution);

                return solutions;
            }

            // For depth 3+: Explore intermediate slots
            // Example depth 3: start -> intermediate -> target
            if (depth == 3)
            {
                foreach (var intermediateSlot in allSlots.Take(10)) // Limit exploration
                {
                    var multiStepSolution = TryMultiStepChain(
                        selectedLessonId,
                        new[] { startSlot, intermediateSlot, targetSlot },
                        allLessons,
                        graph,
                        originalPositions);

                    if (multiStepSolution != null)
                    {
                        solutions.Add(multiStepSolution);
                        if (solutions.Count >= 2)
                            break; // Limit solutions per depth
                    }
                }
            }
            // For depth 4+: Create sequences with multiple intermediate slots
            else if (depth == 4)
            {
                foreach (var intermediate1 in allSlots.Take(5))
                {
                    foreach (var intermediate2 in allSlots.Take(5))
                    {
                        if (intermediate1.Equals(intermediate2))
                            continue;

                        var multiStepSolution = TryMultiStepChain(
                            selectedLessonId,
                            new[] { startSlot, intermediate1, intermediate2, targetSlot },
                            allLessons,
                            graph,
                            originalPositions);

                        if (multiStepSolution != null)
                        {
                            solutions.Add(multiStepSolution);
                            if (solutions.Count >= 1)
                                break;
                        }
                    }
                    if (solutions.Count >= 1)
                        break;
                }
            }

            return solutions;
        }

        /// <summary>
        /// Try to create a multi-step chain solution through a sequence of slots
        /// </summary>
        private KempeChainSolution? TryMultiStepChain(
            int selectedLessonId,
            TimeSlot[] slotSequence,
            List<ScheduledLesson> allLessons,
            ConflictGraph graph,
            Dictionary<int, TimeSlot> originalPositions)
        {
            var allMovements = new List<KempeChainMove>();
            var involvedLessons = new HashSet<int>();

            // Process each pair of consecutive slots in the sequence
            for (int i = 0; i < slotSequence.Length - 1; i++)
            {
                var fromSlot = slotSequence[i];
                var toSlot = slotSequence[i + 1];

                // For the first step, use the selected lesson as anchor
                // For subsequent steps, find any lesson in the current slot that needs to move
                int anchorLesson = i == 0 ? selectedLessonId : FindAnchorLesson(fromSlot, toSlot, graph, involvedLessons);

                if (anchorLesson == 0)
                    return null; // Couldn't find a suitable anchor

                var chain = graph.ExtractKempeChain(anchorLesson, fromSlot, toSlot);

                // Convert chain to movements
                var chainMovements = ConvertChainToMovements(
                    chain,
                    selectedLessonId,
                    allLessons,
                    originalPositions);

                if (chainMovements == null)
                    return null; // Chain would move a lesson back to original position

                allMovements.AddRange(chainMovements);

                // Track involved lessons
                foreach (var move in chainMovements)
                {
                    involvedLessons.Add(move.ScheduledLessonId);
                }
            }

            // Verify selected lesson actually moves to the final target
            var selectedMove = allMovements.FirstOrDefault(m => m.IsSelectedLesson);
            if (selectedMove == null || !selectedMove.ToSlot.Equals(slotSequence[slotSequence.Length - 1]))
                return null;

            // Remove duplicate movements (a lesson might appear multiple times)
            var uniqueMovements = MergeMovements(allMovements);

            return new KempeChainSolution
            {
                Movements = uniqueMovements,
                ChainSize = uniqueMovements.Count,
                QualityScore = CalculateQualityScore(uniqueMovements, selectedLessonId, slotSequence.Length)
            };
        }

        /// <summary>
        /// Find an anchor lesson in fromSlot that conflicts with lessons in toSlot
        /// </summary>
        private int FindAnchorLesson(TimeSlot fromSlot, TimeSlot toSlot, ConflictGraph graph, HashSet<int> involvedLessons)
        {
            var lessonsInFrom = graph.GetLessonsInSlot(fromSlot);
            var lessonsInTo = graph.GetLessonsInSlot(toSlot);

            // Prefer lessons already involved in the chain
            foreach (var lessonId in lessonsInFrom.Where(l => involvedLessons.Contains(l)))
            {
                var conflicts = graph.GetConflictingLessons(lessonId);
                if (conflicts.Any(c => lessonsInTo.Contains(c)))
                    return lessonId;
            }

            // Otherwise, find any lesson with conflicts
            foreach (var lessonId in lessonsInFrom)
            {
                var conflicts = graph.GetConflictingLessons(lessonId);
                if (conflicts.Any(c => lessonsInTo.Contains(c)))
                    return lessonId;
            }

            return 0;
        }

        /// <summary>
        /// Convert a Kempe chain to list of movements
        /// </summary>
        private List<KempeChainMove>? ConvertChainToMovements(
            KempeChain chain,
            int selectedLessonId,
            List<ScheduledLesson> allLessons,
            Dictionary<int, TimeSlot> originalPositions)
        {
            var movements = new List<KempeChainMove>();

            // Swap lessons in slot1 to slot2
            foreach (var lessonId in chain.LessonsInSlot1)
            {
                var lesson = allLessons.FirstOrDefault(l => l.Id == lessonId);
                if (lesson == null) continue;

                // Check if this lesson would be moving back to its original position
                if (originalPositions.TryGetValue(lessonId, out var originalSlot))
                {
                    if (chain.Slot2.Equals(originalSlot))
                    {
                        _logger.LogDebug($"Multi-step: Rejecting chain - lesson {lessonId} would return to original position");
                        return null;
                    }
                }

                movements.Add(new KempeChainMove
                {
                    ScheduledLessonId = lessonId,
                    IsSelectedLesson = lessonId == selectedLessonId,
                    FromSlot = chain.Slot1,
                    ToSlot = chain.Slot2,
                    LessonDescription = GetLessonDescription(lesson)
                });
            }

            // Swap lessons in slot2 to slot1
            foreach (var lessonId in chain.LessonsInSlot2)
            {
                var lesson = allLessons.FirstOrDefault(l => l.Id == lessonId);
                if (lesson == null) continue;

                // Check if this lesson would be moving back to its original position
                if (originalPositions.TryGetValue(lessonId, out var originalSlot))
                {
                    if (chain.Slot1.Equals(originalSlot))
                    {
                        _logger.LogDebug($"Multi-step: Rejecting chain - lesson {lessonId} would return to original position");
                        return null;
                    }
                }

                movements.Add(new KempeChainMove
                {
                    ScheduledLessonId = lessonId,
                    IsSelectedLesson = lessonId == selectedLessonId,
                    FromSlot = chain.Slot2,
                    ToSlot = chain.Slot1,
                    LessonDescription = GetLessonDescription(lesson)
                });
            }

            return movements;
        }

        /// <summary>
        /// Merge movements - if a lesson moves multiple times, combine into single movement
        /// </summary>
        private List<KempeChainMove> MergeMovements(List<KempeChainMove> movements)
        {
            var lessonMovements = new Dictionary<int, (TimeSlot from, TimeSlot to, bool isSelected, string desc)>();

            foreach (var move in movements)
            {
                if (!lessonMovements.ContainsKey(move.ScheduledLessonId))
                {
                    lessonMovements[move.ScheduledLessonId] = (
                        move.FromSlot,
                        move.ToSlot,
                        move.IsSelectedLesson,
                        move.LessonDescription
                    );
                }
                else
                {
                    // Update final destination
                    var current = lessonMovements[move.ScheduledLessonId];
                    lessonMovements[move.ScheduledLessonId] = (
                        current.from, // Keep original from
                        move.ToSlot,  // Update to
                        current.isSelected,
                        current.desc
                    );
                }
            }

            // Filter out moves where from == to (lesson didn't actually move)
            return lessonMovements
                .Where(kvp => !kvp.Value.from.Equals(kvp.Value.to))
                .Select(kvp => new KempeChainMove
                {
                    ScheduledLessonId = kvp.Key,
                    IsSelectedLesson = kvp.Value.isSelected,
                    FromSlot = kvp.Value.from,
                    ToSlot = kvp.Value.to,
                    LessonDescription = kvp.Value.desc
                })
                .ToList();
        }

        /// <summary>
        /// Build conflict graph from timetable data
        /// </summary>
        private ConflictGraph BuildConflictGraph(List<ScheduledLesson> lessons)
        {
            var graph = new ConflictGraph();

            // Add all lessons to graph
            foreach (var lesson in lessons)
            {
                var slot = new TimeSlot(lesson.DayOfWeek, lesson.PeriodId);
                graph.AddLesson(lesson.Id, slot);
            }

            // Add conflict edges
            for (int i = 0; i < lessons.Count; i++)
            {
                for (int j = i + 1; j < lessons.Count; j++)
                {
                    if (HasResourceConflict(lessons[i], lessons[j]))
                    {
                        graph.AddConflict(lessons[i].Id, lessons[j].Id);
                    }
                }
            }

            return graph;
        }

        /// <summary>
        /// Check if two lessons share resources
        /// </summary>
        private bool HasResourceConflict(ScheduledLesson lesson1, ScheduledLesson lesson2)
        {
            // Check teacher conflicts
            var teachers1 = lesson1.Lesson.LessonTeachers.Select(lt => lt.TeacherId).ToHashSet();
            var teachers2 = lesson2.Lesson.LessonTeachers.Select(lt => lt.TeacherId).ToHashSet();
            if (teachers1.Overlaps(teachers2))
                return true;

            // Check class conflicts
            var classes1 = lesson1.Lesson.LessonClasses.Select(lc => lc.ClassId).ToHashSet();
            var classes2 = lesson2.Lesson.LessonClasses.Select(lc => lc.ClassId).ToHashSet();
            if (classes1.Overlaps(classes2))
                return true;

            // Check room conflicts
            if (lesson1.RoomId.HasValue && lesson2.RoomId.HasValue &&
                lesson1.RoomId == lesson2.RoomId)
                return true;

            return false;
        }

        /// <summary>
        /// Create a solution from a Kempe chain
        /// </summary>
        private KempeChainSolution? CreateSolutionFromChain(
            KempeChain chain,
            int selectedLessonId,
            List<ScheduledLesson> allLessons,
            ConflictGraph graph,
            Dictionary<int, TimeSlot> originalPositions)
        {
            var movements = ConvertChainToMovements(chain, selectedLessonId, allLessons, originalPositions);

            if (movements == null)
                return null;

            // Verify selected lesson actually moves
            var selectedMove = movements.FirstOrDefault(m => m.IsSelectedLesson);
            if (selectedMove == null || selectedMove.FromSlot.Equals(selectedMove.ToSlot))
                return null;

            return new KempeChainSolution
            {
                Movements = movements,
                ChainSize = chain.ChainSize,
                QualityScore = CalculateQualityScore(movements, selectedLessonId, 2)
            };
        }

        /// <summary>
        /// Calculate quality score for a solution
        /// </summary>
        private double CalculateQualityScore(List<KempeChainMove> movements, int selectedLessonId, int chainDepth)
        {
            var additionalMoves = movements.Count(m => !m.IsSelectedLesson);
            var score = 100.0;

            // Penalty for each additional lesson moved
            score -= additionalMoves * 5.0;

            // Bonus for direct moves (no swaps needed)
            if (additionalMoves == 0)
                score += 50.0;
            else if (additionalMoves <= 2)
                score += 20.0;

            // Small penalty for deeper chains (they're more complex)
            score -= (chainDepth - 2) * 2.0;

            // But bonus for finding multi-step solutions (they're more creative)
            if (chainDepth >= 3)
                score += 10.0;

            return Math.Max(0, score);
        }

        /// <summary>
        /// Check if solution is duplicate
        /// </summary>
        private bool IsDuplicateSolution(List<KempeChainSolution> solutions, KempeChainSolution newSolution)
        {
            var newSignature = string.Join("|",
                newSolution.Movements
                    .OrderBy(m => m.ScheduledLessonId)
                    .Select(m => $"{m.ScheduledLessonId}:{m.FromSlot}->{m.ToSlot}"));

            foreach (var existing in solutions)
            {
                var existingSignature = string.Join("|",
                    existing.Movements
                        .OrderBy(m => m.ScheduledLessonId)
                        .Select(m => $"{m.ScheduledLessonId}:{m.FromSlot}->{m.ToSlot}"));

                if (newSignature == existingSignature)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Get human-readable lesson description
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
        /// Load timetable data with all related entities
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
                .Where(sl => sl.TimetableId == timetableId)
                .AsSplitQuery()
                .ToListAsync();
        }
    }
}
