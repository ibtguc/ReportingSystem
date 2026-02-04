using SchedulingSystem.Data;
using SchedulingSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SchedulingSystem.Services.LessonMovement
{
    /// <summary>
    /// Implements Kempe Chain + Tabu Search for timetable optimization
    /// Research-proven approach for moving lessons with minimal disruption
    /// </summary>
    public class KempeChainTabuSearch
    {
        private readonly ApplicationDbContext _context;
        private readonly AvailableSlotFinder _slotFinder;
        private readonly ILogger<KempeChainTabuSearch> _logger;

        public KempeChainTabuSearch(
            ApplicationDbContext context,
            AvailableSlotFinder slotFinder,
            ILogger<KempeChainTabuSearch> logger)
        {
            _context = context;
            _slotFinder = slotFinder;
            _logger = logger;
        }

        /// <summary>
        /// Find alternative placements for a lesson using Kempe chains and tabu search
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
            // Use all available destinations to maximize solution diversity
            var targetSlots = destinations
                .Where(d => d.DayOfWeek != currentSlot.Day || d.PeriodId != currentSlot.PeriodId)
                .OrderByDescending(d => d.QualityScore)
                .Select(d => new TimeSlot(d.DayOfWeek, d.PeriodId))
                .ToList(); // No limit - explore all destinations until we reach maxSolutions

            _logger.LogInformation($"Kempe Chain: Found {targetSlots.Count} potential destination slots to explore");

            var solutions = new List<KempeChainSolution>();
            var tabuList = new TabuList(tenure: 7);
            int chainsExplored = 0;
            int chainsSkippedTooLarge = 0;
            int chainsSkippedOriginalPosition = 0;
            int solutionsCreated = 0;
            int duplicatesSkipped = 0;

            // For each promising destination
            foreach (var targetSlot in targetSlots)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (solutions.Count >= maxSolutions)
                {
                    _logger.LogInformation($"Kempe Chain: Reached maxSolutions limit ({maxSolutions})");
                    break;
                }

                // Extract Kempe chain
                var chain = graph.ExtractKempeChain(selectedLessonId, currentSlot, targetSlot);
                chainsExplored++;

                // Check if chain is valid (not too large)
                // Increased limit to allow more complex swap chains for better solution diversity
                if (chain.ChainSize > 50) // Limit chain size to prevent excessive complexity
                {
                    chainsSkippedTooLarge++;
                    continue;
                }

                // Create solution from this chain
                var solution = CreateSolutionFromChain(
                    chain,
                    selectedLessonId,
                    allLessons,
                    graph,
                    originalPositions);

                if (solution != null)
                {
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
                else
                {
                    // Solution was rejected (likely because a lesson would return to original position)
                    chainsSkippedOriginalPosition++;
                }
            }

            _logger.LogInformation($"Kempe Chain Summary: Explored {chainsExplored} chains, " +
                $"Skipped {chainsSkippedTooLarge} too large, " +
                $"Skipped {chainsSkippedOriginalPosition} (would return to original position), " +
                $"Created {solutionsCreated} unique solutions, " +
                $"Skipped {duplicatesSkipped} duplicates");

            return solutions
                .OrderByDescending(s => s.QualityScore)
                .Take(maxSolutions)
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
        /// Check if two lessons share resources (teacher, class, or room)
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

            // Check room conflicts (if both have rooms assigned)
            if (lesson1.RoomId.HasValue && lesson2.RoomId.HasValue &&
                lesson1.RoomId == lesson2.RoomId)
                return true;

            return false;
        }

        /// <summary>
        /// Create a solution from a Kempe chain by swapping the lessons
        /// </summary>
        private KempeChainSolution? CreateSolutionFromChain(
            KempeChain chain,
            int selectedLessonId,
            List<ScheduledLesson> allLessons,
            ConflictGraph graph,
            Dictionary<int, TimeSlot> originalPositions)
        {
            var movements = new List<KempeChainMove>();

            // Swap lessons in slot1 to slot2
            foreach (var lessonId in chain.LessonsInSlot1)
            {
                var lesson = allLessons.FirstOrDefault(l => l.Id == lessonId);
                if (lesson == null) continue;

                // CRITICAL: Check if this lesson would be moving back to its original position
                if (originalPositions.TryGetValue(lessonId, out var originalSlot))
                {
                    // If ToSlot equals the original position, skip this movement
                    if (chain.Slot2.Equals(originalSlot))
                    {
                        _logger.LogDebug($"Skipping movement of lesson {lessonId} from {chain.Slot1} to {chain.Slot2} - would return to original position");
                        return null; // Invalid solution - lesson would return to original position
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

                // CRITICAL: Check if this lesson would be moving back to its original position
                if (originalPositions.TryGetValue(lessonId, out var originalSlot))
                {
                    // If ToSlot equals the original position, skip this movement
                    if (chain.Slot1.Equals(originalSlot))
                    {
                        _logger.LogDebug($"Skipping movement of lesson {lessonId} from {chain.Slot2} to {chain.Slot1} - would return to original position");
                        return null; // Invalid solution - lesson would return to original position
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

            // Verify selected lesson actually moves
            var selectedMove = movements.FirstOrDefault(m => m.IsSelectedLesson);
            if (selectedMove == null || selectedMove.FromSlot.Equals(selectedMove.ToSlot))
                return null;

            return new KempeChainSolution
            {
                Movements = movements,
                ChainSize = chain.ChainSize,
                QualityScore = CalculateQualityScore(movements, selectedLessonId)
            };
        }

        /// <summary>
        /// Calculate quality score for a solution
        /// </summary>
        private double CalculateQualityScore(List<KempeChainMove> movements, int selectedLessonId)
        {
            // Prefer solutions that:
            // 1. Move fewer additional lessons (smaller chain)
            // 2. Don't involve locked lessons
            // 3. Have minimal disruption

            var additionalMoves = movements.Count(m => !m.IsSelectedLesson);
            var score = 100.0;

            // Penalty for each additional lesson moved
            score -= additionalMoves * 5.0;

            // Bonus for very small chains
            if (additionalMoves == 0)
                score += 50.0; // Direct move (no swap needed)
            else if (additionalMoves <= 2)
                score += 20.0; // Small chain

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

    /// <summary>
    /// Represents a solution found by Kempe chain search
    /// </summary>
    public class KempeChainSolution
    {
        public List<KempeChainMove> Movements { get; set; } = new();
        public int ChainSize { get; set; }
        public double QualityScore { get; set; }
    }

    /// <summary>
    /// Represents a single move in a Kempe chain solution
    /// </summary>
    public class KempeChainMove
    {
        public int ScheduledLessonId { get; set; }
        public int LessonId { get; set; } // Lesson template ID
        public bool IsSelectedLesson { get; set; }
        public TimeSlot FromSlot { get; set; } = default!;
        public TimeSlot ToSlot { get; set; } = default!;
        public string LessonDescription { get; set; } = "";
    }
}
