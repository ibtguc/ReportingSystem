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
    /// Request to generate timetable options
    /// </summary>
    public class GenerationRequest
    {
        public int TimetableId { get; set; }
        public List<int> SelectedLessonIds { get; set; } = new();
        public (DayOfWeek Day, int PeriodId)? DestinationSlot { get; set; }
        public List<(DayOfWeek Day, int PeriodId)> AvoidSlotsForSelected { get; set; } = new();
        public List<(DayOfWeek Day, int PeriodId)> AvoidSlotsForUnlocked { get; set; } = new();
        public int MaxUnfixedLessonsToMove { get; set; } = 6;
        public int MaxTimeMinutes { get; set; } = 5;
        public List<string> IgnoredConstraintCodes { get; set; } = new();
        public string SelectedAlgorithm { get; set; } = "kempe-chain";
    }

    /// <summary>
    /// Represents a single generated timetable option
    /// </summary>
    public class TimetableOption
    {
        public int OptionNumber { get; set; }
        public List<LessonMovement> Movements { get; set; } = new();
        public double QualityScore { get; set; }
        public int TotalMovedLessons => Movements.Count;
        public int UnfixedLessonsMoved => Movements.Count(m => !m.IsSelectedLesson);
        public List<string> SoftViolations { get; set; } = new();
        public int SoftViolationCount => SoftViolations.Count;

        // For display: complete timetable state after applying movements
        public Dictionary<string, SlotContent> TimetableState { get; set; } = new();
    }

    /// <summary>
    /// Represents a single lesson movement in an option
    /// </summary>
    public class LessonMovement
    {
        public int ScheduledLessonId { get; set; }
        public int LessonId { get; set; } // Lesson template ID
        public string LessonDescription { get; set; } = "";
        public bool IsSelectedLesson { get; set; }
        public DayOfWeek FromDay { get; set; }
        public int FromPeriodId { get; set; }
        public string FromPeriodName { get; set; } = "";
        public DayOfWeek ToDay { get; set; }
        public int ToPeriodId { get; set; }
        public string ToPeriodName { get; set; } = "";
        public int? FromRoomId { get; set; }
        public int? ToRoomId { get; set; }
    }

    /// <summary>
    /// Content of a timetable slot for display
    /// </summary>
    public class SlotContent
    {
        public List<SlotLesson> Lessons { get; set; } = new();
    }

    /// <summary>
    /// Lesson information for display in a slot
    /// </summary>
    public class SlotLesson
    {
        public int ScheduledLessonId { get; set; }
        public int LessonId { get; set; }
        public List<string> Subjects { get; set; } = new();
        public List<string> Teachers { get; set; } = new();
        public List<string> Classes { get; set; } = new();
        public List<string> Rooms { get; set; } = new();
        public bool IsLocked { get; set; }
        public bool WasMoved { get; set; }
    }

    /// <summary>
    /// Progress information for generation
    /// </summary>
    public class GenerationProgress
    {
        public int OptionsGenerated { get; set; }
        public int CombinationsExplored { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public string? StatusMessage { get; set; }
    }

    /// <summary>
    /// Represents a saved timetable option with metadata
    /// </summary>
    public class SavedTimetableOption
    {
        public int TimetableId { get; set; }
        public int OptionNumber { get; set; }
        public double QualityScore { get; set; }
        public int TotalMovedLessons { get; set; }
        public int UnfixedLessonsMoved { get; set; }
        public int SoftViolationCount { get; set; }

        // Include full data for accordion display
        public List<LessonMovement> Movements { get; set; } = new();
        public List<string> SoftViolations { get; set; } = new();
        public Dictionary<string, SlotContent> TimetableState { get; set; } = new();
    }

    /// <summary>
    /// Service to generate multiple timetable options for lesson movements
    /// </summary>
    public class TimetableGenerationService
    {
        private readonly ApplicationDbContext _context;
        private readonly AvailableSlotFinder _slotFinder;
        private readonly SwapChainSolver _swapSolver;

        public TimetableGenerationService(
            ApplicationDbContext context,
            AvailableSlotFinder slotFinder,
            SwapChainSolver swapSolver)
        {
            _context = context;
            _slotFinder = slotFinder;
            _swapSolver = swapSolver;
        }

        /// <summary>
        /// Generate timetable options based on user request
        /// </summary>
        public async Task<List<TimetableOption>> GenerateOptionsAsync(
            GenerationRequest request,
            IProgress<GenerationProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var options = new List<TimetableOption>();
            var combinationsExplored = 0;
            var optionNumber = 1;

            try
            {
                // Load all scheduled lessons for the timetable with complete data
                var allScheduledLessons = await LoadTimetableLessonsAsync(request.TimetableId);

                // Validate selected lessons
                var selectedLessons = allScheduledLessons
                    .Where(sl => request.SelectedLessonIds.Contains(sl.Id))
                    .ToList();

                if (selectedLessons.Count == 0)
                {
                    throw new ArgumentException("No valid selected lessons found");
                }

                // Check if all selected lessons are locked
                if (selectedLessons.All(sl => sl.IsLocked))
                {
                    throw new ArgumentException("All selected lessons are locked and cannot be moved");
                }

                progress?.Report(new GenerationProgress
                {
                    OptionsGenerated = 0,
                    CombinationsExplored = 0,
                    ElapsedTime = stopwatch.Elapsed,
                    StatusMessage = $"Starting generation for {selectedLessons.Count} lesson(s)..."
                });

                // Strategy selection: prioritize single lesson (uses swap chains)
                if (selectedLessons.Count == 1)
                {
                    // Single lesson - use swap chain solver
                    if (request.DestinationSlot.HasValue)
                    {
                        // Single destination: find swap chains to specific slot
                        var result = await GenerateSingleLessonWithDestinationAsync(
                            request,
                            selectedLessons[0],
                            allScheduledLessons,
                            combinationsExplored,
                            optionNumber,
                            progress,
                            stopwatch,
                            cancellationToken);
                        options = result.Options;
                        combinationsExplored = result.CombinationsExplored;
                        optionNumber = result.NextOptionNumber;
                    }
                    else
                    {
                        // No destination: explore swap chains to all viable slots
                        var result = await GenerateSingleLessonAllDestinationsAsync(
                            request,
                            selectedLessons[0],
                            allScheduledLessons,
                            combinationsExplored,
                            optionNumber,
                            progress,
                            stopwatch,
                            cancellationToken);
                        options = result.Options;
                        combinationsExplored = result.CombinationsExplored;
                        optionNumber = result.NextOptionNumber;
                    }
                }
                else
                {
                    // Multiple lessons
                    if (request.DestinationSlot.HasValue)
                    {
                        throw new ArgumentException("Destination slot can only be specified for single lesson selection");
                    }

                    // Find best slots for each lesson (without swap chains for now)
                    var result = await GenerateMultipleLessonOptionsAsync(
                        request,
                        selectedLessons,
                        allScheduledLessons,
                        combinationsExplored,
                        optionNumber,
                        progress,
                        stopwatch,
                        cancellationToken);
                    options = result.Options;
                    combinationsExplored = result.CombinationsExplored;
                    optionNumber = result.NextOptionNumber;
                }

                // Rank options by quality
                options = options
                    .OrderByDescending(o => o.QualityScore)
                    .ThenBy(o => o.UnfixedLessonsMoved)
                    .ThenBy(o => o.SoftViolationCount)
                    .ToList();

                // Renumber options after sorting
                for (int i = 0; i < options.Count; i++)
                {
                    options[i].OptionNumber = i + 1;
                }

                progress?.Report(new GenerationProgress
                {
                    OptionsGenerated = options.Count,
                    CombinationsExplored = combinationsExplored,
                    ElapsedTime = stopwatch.Elapsed,
                    StatusMessage = $"Generation complete. {options.Count} option(s) found."
                });

                return options;
            }
            catch (OperationCanceledException)
            {
                progress?.Report(new GenerationProgress
                {
                    OptionsGenerated = options.Count,
                    CombinationsExplored = combinationsExplored,
                    ElapsedTime = stopwatch.Elapsed,
                    StatusMessage = $"Generation stopped by user. {options.Count} option(s) found so far."
                });

                return options;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// Generate options for single lesson to specific destination using swap chains
        /// </summary>
        private async Task<(List<TimetableOption> Options, int CombinationsExplored, int NextOptionNumber)> GenerateSingleLessonWithDestinationAsync(
            GenerationRequest request,
            ScheduledLesson selectedLesson,
            List<ScheduledLesson> allScheduledLessons,
            int combinationsExplored,
            int optionNumber,
            IProgress<GenerationProgress>? progress,
            Stopwatch stopwatch,
            CancellationToken cancellationToken)
        {
            var options = new List<TimetableOption>();
            var destination = request.DestinationSlot!.Value;

            // Configure swap chain search
            var config = new SwapChainConfig
            {
                MaxDepth = request.MaxUnfixedLessonsToMove,
                MaxSolutions = 10, // Find up to 10 different ways
                Timeout = TimeSpan.FromMinutes(request.MaxTimeMinutes),
                ExcludeSlots = request.AvoidSlotsForSelected,
                IgnoredConstraintCodes = request.IgnoredConstraintCodes
            };

            // Find swap chains to move to destination
            var swapChains = await _swapSolver.FindSwapChainsAsync(
                request.TimetableId,
                selectedLesson.Id,
                destination.Day,
                destination.PeriodId,
                selectedLesson.RoomId,
                config);

            combinationsExplored += swapChains.Count;

            foreach (var chain in swapChains.Where(c => c.IsValid))
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Check if chain respects max unfixed lessons limit
                var unfixedMoves = chain.Steps.Count(s => s.ScheduledLessonId != selectedLesson.Id);
                if (unfixedMoves > request.MaxUnfixedLessonsToMove)
                    continue;

                // Convert swap chain to timetable option
                var option = await ConvertSwapChainToOptionAsync(
                    chain,
                    request,
                    selectedLesson.Id,
                    allScheduledLessons,
                    optionNumber++);

                options.Add(option);

                progress?.Report(new GenerationProgress
                {
                    OptionsGenerated = options.Count,
                    CombinationsExplored = combinationsExplored,
                    ElapsedTime = stopwatch.Elapsed,
                    StatusMessage = $"Found {options.Count} option(s)..."
                });

                // Stop if we have enough options or timeout
                if (options.Count >= 50 || stopwatch.Elapsed.TotalMinutes >= request.MaxTimeMinutes)
                    break;
            }

            return (options, combinationsExplored, optionNumber);
        }

        /// <summary>
        /// Generate options for single lesson exploring all possible destinations using swap chains
        /// </summary>
        private async Task<(List<TimetableOption> Options, int CombinationsExplored, int NextOptionNumber)> GenerateSingleLessonAllDestinationsAsync(
            GenerationRequest request,
            ScheduledLesson selectedLesson,
            List<ScheduledLesson> allScheduledLessons,
            int combinationsExplored,
            int optionNumber,
            IProgress<GenerationProgress>? progress,
            Stopwatch stopwatch,
            CancellationToken cancellationToken)
        {
            var options = new List<TimetableOption>();

            progress?.Report(new GenerationProgress
            {
                OptionsGenerated = 0,
                CombinationsExplored = combinationsExplored,
                ElapsedTime = stopwatch.Elapsed,
                StatusMessage = "Finding available destinations..."
            });

            // Get all available slots (including those that might need swap chains)
            var availableSlots = await _slotFinder.FindAvailableSlotsAsync(
                request.TimetableId,
                selectedLesson.LessonId,
                selectedLesson.Id,
                request.AvoidSlotsForSelected,
                includeCurrentSlot: false,
                request.IgnoredConstraintCodes);

            // Explicitly filter out current slot to ensure movement
            availableSlots = availableSlots
                .Where(s => !s.IsCurrentSlot)
                .Where(s => s.DayOfWeek != selectedLesson.DayOfWeek || s.PeriodId != selectedLesson.PeriodId)
                .ToList();

            // Filter to top quality slots to limit search space
            var topSlots = availableSlots
                .Where(s => !s.HasHardConstraintViolations)
                .OrderByDescending(s => s.QualityScore)
                .Take(15) // Explore top 15 destinations
                .ToList();

            // Also consider slots with hard violations if we use swap chains
            var blockedSlots = availableSlots
                .Where(s => s.HasHardConstraintViolations)
                .OrderByDescending(s => s.QualityScore)
                .Take(10) // Explore top 10 blocked slots
                .ToList();

            var allDestinations = topSlots.Concat(blockedSlots).ToList();

            // Check if we have any valid destinations
            if (!allDestinations.Any())
            {
                progress?.Report(new GenerationProgress
                {
                    OptionsGenerated = 0,
                    CombinationsExplored = combinationsExplored,
                    ElapsedTime = stopwatch.Elapsed,
                    StatusMessage = $"No available destinations found for {GetLessonDescription(selectedLesson)}"
                });
                return (options, combinationsExplored, optionNumber);
            }

            progress?.Report(new GenerationProgress
            {
                OptionsGenerated = 0,
                CombinationsExplored = combinationsExplored,
                ElapsedTime = stopwatch.Elapsed,
                StatusMessage = $"Exploring {allDestinations.Count} possible destinations..."
            });

            // Configure swap chain search
            var config = new SwapChainConfig
            {
                MaxDepth = request.MaxUnfixedLessonsToMove,
                MaxSolutions = 3, // Find up to 3 ways per destination to limit combinations
                Timeout = TimeSpan.FromMinutes(request.MaxTimeMinutes),
                ExcludeSlots = request.AvoidSlotsForSelected,
                IgnoredConstraintCodes = request.IgnoredConstraintCodes
            };

            // Try each destination
            foreach (var destination in allDestinations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Find swap chains to this destination
                var swapChains = await _swapSolver.FindSwapChainsAsync(
                    request.TimetableId,
                    selectedLesson.Id,
                    destination.DayOfWeek,
                    destination.PeriodId,
                    destination.RoomId,
                    config);

                combinationsExplored += swapChains.Count;

                foreach (var chain in swapChains.Where(c => c.IsValid))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Check if chain respects max unfixed lessons limit
                    var unfixedMoves = chain.Steps.Count(s => s.ScheduledLessonId != selectedLesson.Id);
                    if (unfixedMoves > request.MaxUnfixedLessonsToMove)
                        continue;

                    // Convert swap chain to timetable option
                    var option = await ConvertSwapChainToOptionAsync(
                        chain,
                        request,
                        selectedLesson.Id,
                        allScheduledLessons,
                        optionNumber++);

                    options.Add(option);

                    progress?.Report(new GenerationProgress
                    {
                        OptionsGenerated = options.Count,
                        CombinationsExplored = combinationsExplored,
                        ElapsedTime = stopwatch.Elapsed,
                        StatusMessage = $"Found {options.Count} option(s)..."
                    });

                    // Stop if we have enough options or timeout
                    if (options.Count >= 50 || stopwatch.Elapsed.TotalMinutes >= request.MaxTimeMinutes)
                        break;
                }

                // Stop if we have enough options or timeout
                if (options.Count >= 50 || stopwatch.Elapsed.TotalMinutes >= request.MaxTimeMinutes)
                    break;
            }

            return (options, combinationsExplored, optionNumber);
        }

        /// <summary>
        /// Generate options for multiple lessons (find best slots for each)
        /// </summary>
        private async Task<(List<TimetableOption> Options, int CombinationsExplored, int NextOptionNumber)> GenerateMultipleLessonOptionsAsync(
            GenerationRequest request,
            List<ScheduledLesson> selectedLessons,
            List<ScheduledLesson> allScheduledLessons,
            int combinationsExplored,
            int optionNumber,
            IProgress<GenerationProgress>? progress,
            Stopwatch stopwatch,
            CancellationToken cancellationToken)
        {
            var options = new List<TimetableOption>();

            // For each selected lesson, find available slots (excluding avoid slots)
            var lessonAvailableSlots = new Dictionary<int, List<AvailableSlot>>();

            foreach (var lesson in selectedLessons.Where(l => !l.IsLocked))
            {
                var slots = await _slotFinder.FindAvailableSlotsAsync(
                    request.TimetableId,
                    lesson.LessonId,
                    lesson.Id,
                    request.AvoidSlotsForSelected,
                    includeCurrentSlot: false,
                    request.IgnoredConstraintCodes);

                // Filter out current slot explicitly and ensure movement
                lessonAvailableSlots[lesson.Id] = slots
                    .Where(s => !s.HasHardConstraintViolations)
                    .Where(s => !s.IsCurrentSlot) // Explicitly exclude current slot
                    .Where(s => s.DayOfWeek != lesson.DayOfWeek || s.PeriodId != lesson.PeriodId) // Ensure actual movement
                    .OrderByDescending(s => s.QualityScore)
                    .Take(5) // Top 5 slots per lesson
                    .ToList();
            }

            // Validate that all selected lessons have at least one available slot
            var lessonsWithoutSlots = selectedLessons
                .Where(l => !l.IsLocked)
                .Where(l => !lessonAvailableSlots.ContainsKey(l.Id) || !lessonAvailableSlots[l.Id].Any())
                .ToList();

            if (lessonsWithoutSlots.Any())
            {
                var lessonDescriptions = string.Join(", ", lessonsWithoutSlots.Select(GetLessonDescription));
                progress?.Report(new GenerationProgress
                {
                    OptionsGenerated = 0,
                    CombinationsExplored = 0,
                    ElapsedTime = stopwatch.Elapsed,
                    StatusMessage = $"No available slots found for: {lessonDescriptions}"
                });
                return (options, combinationsExplored, optionNumber);
            }

            // Generate combinations of slot assignments
            var combinations = GenerateSlotCombinations(
                selectedLessons.Where(l => !l.IsLocked).ToList(),
                lessonAvailableSlots,
                request.MaxUnfixedLessonsToMove);

            foreach (var combination in combinations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                combinationsExplored++;

                // Try to realize this combination (may need swap chains)
                var option = await TryRealizeCombinationAsync(
                    combination,
                    request,
                    allScheduledLessons,
                    optionNumber,
                    cancellationToken);

                if (option != null)
                {
                    options.Add(option);
                    optionNumber++;

                    progress?.Report(new GenerationProgress
                    {
                        OptionsGenerated = options.Count,
                        CombinationsExplored = combinationsExplored,
                        ElapsedTime = stopwatch.Elapsed,
                        StatusMessage = $"Found {options.Count} option(s)... Exploring combinations..."
                    });
                }

                // Stop conditions
                if (options.Count >= 50 || stopwatch.Elapsed.TotalMinutes >= request.MaxTimeMinutes)
                    break;
            }

            return (options, combinationsExplored, optionNumber);
        }

        /// <summary>
        /// Generate combinations of slot assignments for multiple lessons
        /// </summary>
        private List<Dictionary<int, AvailableSlot>> GenerateSlotCombinations(
            List<ScheduledLesson> lessons,
            Dictionary<int, List<AvailableSlot>> availableSlots,
            int maxDepth)
        {
            var combinations = new List<Dictionary<int, AvailableSlot>>();

            if (lessons.Count == 0)
                return combinations;

            // Start with first lesson
            var firstLesson = lessons[0];
            var firstSlots = availableSlots.GetValueOrDefault(firstLesson.Id, new List<AvailableSlot>());

            foreach (var slot in firstSlots)
            {
                var combination = new Dictionary<int, AvailableSlot>
                {
                    [firstLesson.Id] = slot
                };

                if (lessons.Count == 1)
                {
                    combinations.Add(combination);
                }
                else
                {
                    // Recursively add other lessons
                    AddRemainingLessons(
                        combination,
                        lessons.Skip(1).ToList(),
                        availableSlots,
                        combinations,
                        0,
                        maxDepth * 2); // Limit total combinations
                }
            }

            return combinations.Take(100).ToList(); // Limit to prevent explosion
        }

        /// <summary>
        /// Recursively add remaining lessons to combination
        /// </summary>
        private void AddRemainingLessons(
            Dictionary<int, AvailableSlot> currentCombination,
            List<ScheduledLesson> remainingLessons,
            Dictionary<int, List<AvailableSlot>> availableSlots,
            List<Dictionary<int, AvailableSlot>> allCombinations,
            int depth,
            int maxCombinations)
        {
            if (allCombinations.Count >= maxCombinations)
                return;

            if (remainingLessons.Count == 0)
            {
                allCombinations.Add(new Dictionary<int, AvailableSlot>(currentCombination));
                return;
            }

            var nextLesson = remainingLessons[0];
            var nextSlots = availableSlots.GetValueOrDefault(nextLesson.Id, new List<AvailableSlot>());

            foreach (var slot in nextSlots)
            {
                currentCombination[nextLesson.Id] = slot;

                if (remainingLessons.Count == 1)
                {
                    allCombinations.Add(new Dictionary<int, AvailableSlot>(currentCombination));
                }
                else
                {
                    AddRemainingLessons(
                        currentCombination,
                        remainingLessons.Skip(1).ToList(),
                        availableSlots,
                        allCombinations,
                        depth + 1,
                        maxCombinations);
                }

                if (allCombinations.Count >= maxCombinations)
                    return;
            }
        }

        /// <summary>
        /// Try to realize a combination of slot assignments
        /// </summary>
        private async Task<TimetableOption?> TryRealizeCombinationAsync(
            Dictionary<int, AvailableSlot> combination,
            GenerationRequest request,
            List<ScheduledLesson> allScheduledLessons,
            int optionNumber,
            CancellationToken cancellationToken)
        {
            // This is a simplified version - in reality, you would need to:
            // 1. Check if any assignments conflict with each other
            // 2. Find swap chains if needed for conflicting assignments
            // 3. Validate the complete solution respects all constraints

            var movements = new List<LessonMovement>();
            var softViolations = new List<string>();
            double totalQuality = 0;

            foreach (var (lessonId, targetSlot) in combination)
            {
                var lesson = allScheduledLessons.First(l => l.Id == lessonId);

                // Verify this is an actual movement (not staying in the same slot)
                if (lesson.DayOfWeek == targetSlot.DayOfWeek && lesson.PeriodId == targetSlot.PeriodId)
                {
                    // This is not a real movement, skip this option
                    return null;
                }

                // Find period name from allScheduledLessons or database
                var toPeriodName = allScheduledLessons
                    .FirstOrDefault(sl => sl.PeriodId == targetSlot.PeriodId)?.Period?.Name ?? "";

                var movement = new LessonMovement
                {
                    ScheduledLessonId = lessonId,
                    LessonDescription = GetLessonDescription(lesson),
                    IsSelectedLesson = request.SelectedLessonIds.Contains(lessonId),
                    FromDay = lesson.DayOfWeek,
                    FromPeriodId = lesson.PeriodId,
                    FromPeriodName = lesson.Period?.Name ?? "",
                    ToDay = targetSlot.DayOfWeek,
                    ToPeriodId = targetSlot.PeriodId,
                    ToPeriodName = toPeriodName,
                    FromRoomId = lesson.RoomId,
                    ToRoomId = targetSlot.RoomId
                };

                movements.Add(movement);
                softViolations.AddRange(targetSlot.SoftViolations);
                totalQuality += targetSlot.QualityScore;
            }

            // Ensure we have at least one movement
            if (!movements.Any())
            {
                return null;
            }

            var option = new TimetableOption
            {
                OptionNumber = optionNumber,
                Movements = movements,
                QualityScore = totalQuality / Math.Max(1, combination.Count),
                SoftViolations = softViolations.Distinct().ToList()
            };

            // Build timetable state
            option.TimetableState = await BuildTimetableStateAsync(
                request.TimetableId,
                movements,
                allScheduledLessons);

            return option;
        }

        /// <summary>
        /// Convert a swap chain to a timetable option
        /// </summary>
        private async Task<TimetableOption> ConvertSwapChainToOptionAsync(
            SwapChain chain,
            GenerationRequest request,
            int selectedLessonId,
            List<ScheduledLesson> allScheduledLessons,
            int optionNumber)
        {
            var movements = chain.Steps.Select(step => new LessonMovement
            {
                ScheduledLessonId = step.ScheduledLessonId,
                LessonDescription = step.LessonDescription,
                IsSelectedLesson = step.ScheduledLessonId == selectedLessonId,
                FromDay = step.FromDay,
                FromPeriodId = step.FromPeriodId,
                FromPeriodName = step.FromPeriodName,
                ToDay = step.ToDay,
                ToPeriodId = step.ToPeriodId,
                ToPeriodName = step.ToPeriodName,
                FromRoomId = step.FromRoomId,
                ToRoomId = step.ToRoomId
            }).ToList();

            var option = new TimetableOption
            {
                OptionNumber = optionNumber,
                Movements = movements,
                QualityScore = chain.QualityScore,
                SoftViolations = new List<string>() // TODO: Calculate from chain
            };

            // Build timetable state
            option.TimetableState = await BuildTimetableStateAsync(
                request.TimetableId,
                movements,
                allScheduledLessons);

            return option;
        }

        /// <summary>
        /// Build the complete timetable state after applying movements
        /// </summary>
        private async Task<Dictionary<string, SlotContent>> BuildTimetableStateAsync(
            int timetableId,
            List<LessonMovement> movements,
            List<ScheduledLesson> allScheduledLessons)
        {
            var state = new Dictionary<string, SlotContent>();
            var movedLessonIds = movements.Select(m => m.ScheduledLessonId).ToHashSet();

            // Create a working copy of lessons
            var workingLessons = allScheduledLessons.Select(sl => new WorkingLesson
            {
                ScheduledLesson = sl,
                CurrentDay = sl.DayOfWeek,
                CurrentPeriodId = sl.PeriodId,
                WasMoved = false
            }).ToList();

            // Apply movements
            foreach (var movement in movements)
            {
                var lesson = workingLessons.First(l => l.ScheduledLesson.Id == movement.ScheduledLessonId);
                lesson.CurrentDay = movement.ToDay;
                lesson.CurrentPeriodId = movement.ToPeriodId;
                lesson.WasMoved = true;
            }

            // Build state dictionary
            foreach (var item in workingLessons)
            {
                var key = $"{item.CurrentDay}_{item.CurrentPeriodId}";

                if (!state.ContainsKey(key))
                    state[key] = new SlotContent();

                state[key].Lessons.Add(new SlotLesson
                {
                    ScheduledLessonId = item.ScheduledLesson.Id,
                    LessonId = item.ScheduledLesson.LessonId,
                    Subjects = item.ScheduledLesson.Lesson.LessonSubjects
                        .OrderByDescending(ls => ls.IsPrimary)
                        .Select(ls => ls.Subject.Name)
                        .ToList(),
                    Teachers = item.ScheduledLesson.Lesson.LessonTeachers
                        .OrderByDescending(lt => lt.IsLead)
                        .Select(lt => lt.Teacher.FullName)
                        .ToList(),
                    Classes = item.ScheduledLesson.Lesson.LessonClasses
                        .OrderByDescending(lc => lc.IsPrimary)
                        .Select(lc => lc.Class.Name)
                        .ToList(),
                    Rooms = item.ScheduledLesson.RoomId.HasValue
                        ? new List<string> { item.ScheduledLesson.Room?.RoomNumber ?? "" }
                        : new List<string>(),
                    IsLocked = item.ScheduledLesson.IsLocked,
                    WasMoved = item.WasMoved
                });
            }

            return state;
        }

        /// <summary>
        /// Load all scheduled lessons for a timetable with complete data
        /// </summary>
        private async Task<List<ScheduledLesson>> LoadTimetableLessonsAsync(int timetableId)
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
                .ToListAsync();
        }

        /// <summary>
        /// Get a human-readable description of a lesson
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
        /// Helper class to track lesson state during timetable building
        /// </summary>
        private class WorkingLesson
        {
            public ScheduledLesson ScheduledLesson { get; set; } = null!;
            public DayOfWeek CurrentDay { get; set; }
            public int CurrentPeriodId { get; set; }
            public bool WasMoved { get; set; }
        }
    }
}
