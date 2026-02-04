using SchedulingSystem.Data;
using SchedulingSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SchedulingSystem.Services.LessonMovement
{
    /// <summary>
    /// Implements "Musical Chairs" recursive displacement algorithm for timetable optimization.
    /// Inspired by TimeTabler's FIT command, this algorithm finds chains of movements where
    /// each lesson displaces another until finding an empty slot or valid endpoint.
    /// Supports 2-step to 16-step "musical chairs" movement chains.
    /// </summary>
    public class MusicalChairsAlgorithm
    {
        private readonly ApplicationDbContext _context;
        private readonly AvailableSlotFinder _slotFinder;
        private readonly ILogger<MusicalChairsAlgorithm> _logger;

        public MusicalChairsAlgorithm(
            ApplicationDbContext context,
            AvailableSlotFinder slotFinder,
            ILogger<MusicalChairsAlgorithm> logger)
        {
            _context = context;
            _slotFinder = slotFinder;
            _logger = logger;
        }

        /// <summary>
        /// Find alternative placements using Musical Chairs recursive displacement
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

            // Store original positions
            var originalPositions = allLessons.ToDictionary(
                l => l.Id,
                l => new TimeSlot(l.DayOfWeek, l.PeriodId));

            // Get current slot
            var currentSlot = new TimeSlot(selectedLesson.DayOfWeek, selectedLesson.PeriodId);

            // Find all potential destination slots
            var destinations = await _slotFinder.FindAvailableSlotsAsync(
                timetableId,
                selectedLesson.LessonId,
                selectedLessonId,
                new List<(DayOfWeek, int)>(),
                includeCurrentSlot: false,
                ignoredConstraints);

            _logger.LogInformation($"Musical Chairs: Found {destinations.Count} potential destination slots to explore");

            var solutions = new List<KempeChainSolution>();
            int chainsExplored = 0;
            int directMoves = 0;
            int multiStepChains = 0;

            // Try different chain depths (2 to 16 steps as per TimeTabler FIT)
            var maxDepths = new[] { 2, 3, 4, 6, 8, 12, 16 };

            foreach (var maxDepth in maxDepths)
            {
                if (solutions.Count >= maxSolutions)
                    break;

                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation($"Musical Chairs: Exploring chains up to {maxDepth} steps...");

                // For each potential destination
                foreach (var destination in destinations.OrderByDescending(d => d.QualityScore))
                {
                    if (solutions.Count >= maxSolutions)
                        break;

                    cancellationToken.ThrowIfCancellationRequested();

                    var targetSlot = new TimeSlot(destination.DayOfWeek, destination.PeriodId);

                    // Skip current position
                    if (targetSlot.Equals(currentSlot))
                        continue;

                    chainsExplored++;

                    // Try to build a movement chain to this destination
                    var chain = await FindDisplacementChainAsync(
                        selectedLesson,
                        targetSlot,
                        allLessons,
                        originalPositions,
                        maxDepth,
                        ignoredConstraints,
                        new HashSet<int>());

                    if (chain != null && chain.Movements.Count > 0)
                    {
                        // Check for duplicates
                        if (!IsDuplicateSolution(solutions, chain))
                        {
                            solutions.Add(chain);

                            if (chain.Movements.Count == 1)
                                directMoves++;
                            else
                                multiStepChains++;
                        }
                    }
                }

                // If we found enough solutions at this depth, no need to go deeper
                if (solutions.Count >= maxSolutions / 2)
                    break;
            }

            _logger.LogInformation($"Musical Chairs Summary: Explored {chainsExplored} potential moves, " +
                $"Found {directMoves} direct moves, {multiStepChains} multi-step chains, " +
                $"Total: {solutions.Count} unique solutions");

            return solutions
                .OrderByDescending(s => s.QualityScore)
                .Take(maxSolutions)
                .ToList();
        }

        /// <summary>
        /// Recursively find a chain of displacements to move a lesson to a target slot
        /// </summary>
        private async Task<KempeChainSolution?> FindDisplacementChainAsync(
            ScheduledLesson lessonToMove,
            TimeSlot targetSlot,
            List<ScheduledLesson> allLessons,
            Dictionary<int, TimeSlot> originalPositions,
            int maxDepth,
            List<string>? ignoredConstraints,
            HashSet<int> visitedLessons,
            int currentDepth = 0)
        {
            // Prevent infinite recursion
            if (currentDepth >= maxDepth)
                return null;

            // Prevent cycles
            if (visitedLessons.Contains(lessonToMove.Id))
                return null;

            // Check if moving to original position (not allowed)
            if (originalPositions.TryGetValue(lessonToMove.Id, out var originalSlot) &&
                targetSlot.Equals(originalSlot))
                return null;

            // Add to visited set
            var newVisited = new HashSet<int>(visitedLessons) { lessonToMove.Id };

            // Find what's currently occupying the target slot
            var occupyingLesson = allLessons.FirstOrDefault(l =>
                l.Id != lessonToMove.Id &&
                l.DayOfWeek == targetSlot.Day &&
                l.PeriodId == targetSlot.PeriodId);

            // CASE 1: Target slot is empty or we can move here directly
            if (occupyingLesson == null)
            {
                // Direct move - create solution with single movement
                return new KempeChainSolution
                {
                    Movements = new List<KempeChainMove>
                    {
                        new KempeChainMove
                        {
                            ScheduledLessonId = lessonToMove.Id,
                            IsSelectedLesson = currentDepth == 0,
                            FromSlot = new TimeSlot(lessonToMove.DayOfWeek, lessonToMove.PeriodId),
                            ToSlot = targetSlot,
                            LessonDescription = GetLessonDescription(lessonToMove)
                        }
                    },
                    ChainSize = 1,
                    QualityScore = CalculateQualityScore(1, currentDepth)
                };
            }

            // CASE 2: Target slot is occupied - need to recursively displace the occupying lesson
            if (occupyingLesson.IsLocked)
            {
                // Can't move locked lessons
                return null;
            }

            // Find where the occupying lesson could move
            var occupyingLessonDestinations = await _slotFinder.FindAvailableSlotsAsync(
                occupyingLesson.TimetableId ?? 0,
                occupyingLesson.LessonId,
                occupyingLesson.Id,
                new List<(DayOfWeek, int)>(),
                includeCurrentSlot: false,
                ignoredConstraints);

            // Try to find a displacement chain for the occupying lesson
            foreach (var destination in occupyingLessonDestinations
                .OrderByDescending(d => d.QualityScore)
                .Take(10)) // Limit to top 10 destinations to prevent explosion
            {
                var occupyingTargetSlot = new TimeSlot(destination.DayOfWeek, destination.PeriodId);

                // Skip if it's the current slot or the slot we're trying to move into
                if (occupyingTargetSlot.Equals(new TimeSlot(occupyingLesson.DayOfWeek, occupyingLesson.PeriodId)))
                    continue;

                // Recursively find chain for occupying lesson
                var subChain = await FindDisplacementChainAsync(
                    occupyingLesson,
                    occupyingTargetSlot,
                    allLessons,
                    originalPositions,
                    maxDepth,
                    ignoredConstraints,
                    newVisited,
                    currentDepth + 1);

                if (subChain != null)
                {
                    // Build complete chain: our move + sub-chain moves
                    var completeChain = new List<KempeChainMove>
                    {
                        new KempeChainMove
                        {
                            ScheduledLessonId = lessonToMove.Id,
                            IsSelectedLesson = currentDepth == 0,
                            FromSlot = new TimeSlot(lessonToMove.DayOfWeek, lessonToMove.PeriodId),
                            ToSlot = targetSlot,
                            LessonDescription = GetLessonDescription(lessonToMove)
                        }
                    };

                    completeChain.AddRange(subChain.Movements);

                    // Validate chain doesn't have conflicts
                    if (ValidateChain(completeChain))
                    {
                        return new KempeChainSolution
                        {
                            Movements = completeChain,
                            ChainSize = completeChain.Count,
                            QualityScore = CalculateQualityScore(completeChain.Count, currentDepth)
                        };
                    }
                }
            }

            // CASE 3: Try swapping with the occupying lesson
            // Check if occupying lesson can move to our current slot
            var ourCurrentSlot = new TimeSlot(lessonToMove.DayOfWeek, lessonToMove.PeriodId);
            var canSwap = await CanLessonMoveToSlotAsync(
                occupyingLesson,
                ourCurrentSlot,
                allLessons,
                ignoredConstraints);

            if (canSwap)
            {
                // Check if swap would cause occupying lesson to return to original position
                if (originalPositions.TryGetValue(occupyingLesson.Id, out var occupyingOriginal) &&
                    ourCurrentSlot.Equals(occupyingOriginal))
                {
                    // Skip - would return to original
                }
                else
                {
                    // Direct swap
                    return new KempeChainSolution
                    {
                        Movements = new List<KempeChainMove>
                        {
                            new KempeChainMove
                            {
                                ScheduledLessonId = lessonToMove.Id,
                                IsSelectedLesson = currentDepth == 0,
                                FromSlot = ourCurrentSlot,
                                ToSlot = targetSlot,
                                LessonDescription = GetLessonDescription(lessonToMove)
                            },
                            new KempeChainMove
                            {
                                ScheduledLessonId = occupyingLesson.Id,
                                IsSelectedLesson = false,
                                FromSlot = targetSlot,
                                ToSlot = ourCurrentSlot,
                                LessonDescription = GetLessonDescription(occupyingLesson)
                            }
                        },
                        ChainSize = 2,
                        QualityScore = CalculateQualityScore(2, currentDepth)
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// Validate that a chain doesn't have conflicting movements
        /// </summary>
        private bool ValidateChain(List<KempeChainMove> chain)
        {
            // Check for duplicate lesson IDs (shouldn't happen, but safety check)
            var lessonIds = chain.Select(m => m.ScheduledLessonId).ToList();
            if (lessonIds.Count != lessonIds.Distinct().Count())
                return false;

            // Check that no two lessons end up in the same slot
            var finalPositions = chain.GroupBy(m => m.ToSlot.ToString());
            if (finalPositions.Any(g => g.Count() > 1))
                return false;

            return true;
        }

        /// <summary>
        /// Check if a lesson can move to a specific slot
        /// </summary>
        private async Task<bool> CanLessonMoveToSlotAsync(
            ScheduledLesson lesson,
            TimeSlot targetSlot,
            List<ScheduledLesson> allLessons,
            List<string>? ignoredConstraints)
        {
            // Simple check: find available slots and see if target is among them
            var availableSlots = await _slotFinder.FindAvailableSlotsAsync(
                lesson.TimetableId ?? 0,
                lesson.LessonId,
                lesson.Id,
                new List<(DayOfWeek, int)>(),
                includeCurrentSlot: false,
                ignoredConstraints);

            return availableSlots.Any(s =>
                s.DayOfWeek == targetSlot.Day &&
                s.PeriodId == targetSlot.PeriodId);
        }

        /// <summary>
        /// Calculate quality score for a solution
        /// </summary>
        private double CalculateQualityScore(int chainLength, int recursionDepth)
        {
            var score = 100.0;

            // Prefer shorter chains
            score -= chainLength * 10.0;

            // Bonus for direct moves
            if (chainLength == 1)
                score += 50.0;
            else if (chainLength == 2)
                score += 30.0;
            else if (chainLength <= 4)
                score += 15.0;

            // Small penalty for deeper recursion (more complex search)
            score -= recursionDepth * 2.0;

            // Bonus for finding any solution at all
            score += 25.0;

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
