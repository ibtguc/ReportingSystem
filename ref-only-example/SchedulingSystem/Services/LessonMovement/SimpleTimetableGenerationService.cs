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
    /// Timetable generation service using Kempe Chain + Tabu Search algorithm
    /// Finds alternatives for moving selected lesson(s) to different timeslots
    /// Research-proven approach for efficient timetable editing
    /// </summary>
    public class SimpleTimetableGenerationService
    {
        private readonly ApplicationDbContext _context;
        private readonly AvailableSlotFinder _slotFinder;
        private readonly SwapChainSolver _swapSolver;
        private readonly KempeChainTabuSearch _kempeSearch;
        private readonly MultiStepKempeChainTabuSearch _multiStepKempeSearch;
        private readonly MusicalChairsAlgorithm _musicalChairsAlgorithm;
        private readonly RecursiveConflictResolutionAlgorithm _recursiveConflictResolution;

        public SimpleTimetableGenerationService(
            ApplicationDbContext context,
            AvailableSlotFinder slotFinder,
            SwapChainSolver swapSolver,
            KempeChainTabuSearch kempeSearch,
            MultiStepKempeChainTabuSearch multiStepKempeSearch,
            MusicalChairsAlgorithm musicalChairsAlgorithm,
            RecursiveConflictResolutionAlgorithm recursiveConflictResolution)
        {
            _context = context;
            _slotFinder = slotFinder;
            _swapSolver = swapSolver;
            _kempeSearch = kempeSearch;
            _multiStepKempeSearch = multiStepKempeSearch;
            _musicalChairsAlgorithm = musicalChairsAlgorithm;
            _recursiveConflictResolution = recursiveConflictResolution;
        }

        /// <summary>
        /// Generate timetable options using Kempe Chain + Tabu Search
        /// Algorithm:
        /// 1. Load current schedule from DB
        /// 2. Get selected lesson(s) to move
        /// 3. Use Kempe Chain algorithm to find connected lesson groups
        /// 4. Apply Tabu Search to explore alternative placements
        /// 5. Return sorted options with progress updates
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
                // === STEP 1: Load current schedule ===
                progress?.Report(new GenerationProgress
                {
                    OptionsGenerated = 0,
                    CombinationsExplored = 0,
                    ElapsedTime = stopwatch.Elapsed,
                    StatusMessage = "Loading current timetable..."
                });

                var allScheduledLessons = await LoadTimetableWithFullDataAsync(request.TimetableId);

                // === STEP 2: Get selected lesson(s) to move ===
                var selectedLessons = allScheduledLessons
                    .Where(sl => request.SelectedLessonIds.Contains(sl.Id))
                    .Where(sl => !sl.IsLocked) // Skip locked lessons
                    .ToList();

                if (!selectedLessons.Any())
                {
                    progress?.Report(new GenerationProgress
                    {
                        OptionsGenerated = 0,
                        CombinationsExplored = 0,
                        ElapsedTime = stopwatch.Elapsed,
                        StatusMessage = "No valid moveable lessons selected"
                    });
                    return options;
                }

                // For now, handle single lesson only (as specified)
                if (selectedLessons.Count > 1)
                {
                    throw new NotImplementedException("Multiple lesson selection not yet implemented in simplified algorithm");
                }

                var selectedLesson = selectedLessons[0];

                progress?.Report(new GenerationProgress
                {
                    OptionsGenerated = 0,
                    CombinationsExplored = 0,
                    ElapsedTime = stopwatch.Elapsed,
                    StatusMessage = $"Finding alternatives for {GetLessonDescription(selectedLesson)}..."
                });

                // === STEP 3: Find all possible destination slots ===
                List<AvailableSlot> destinationSlots;

                if (request.DestinationSlot.HasValue)
                {
                    // User specified a specific destination - check only that slot
                    var allSlots = await _slotFinder.FindAvailableSlotsAsync(
                        request.TimetableId,
                        selectedLesson.LessonId,
                        selectedLesson.Id,
                        request.AvoidSlotsForSelected,
                        includeCurrentSlot: false,
                        request.IgnoredConstraintCodes);

                    var dest = request.DestinationSlot.Value;
                    destinationSlots = allSlots
                        .Where(s => s.DayOfWeek == dest.Day && s.PeriodId == dest.PeriodId)
                        .ToList();
                }
                else
                {
                    // Find all available slots (excluding current position)
                    destinationSlots = await _slotFinder.FindAvailableSlotsAsync(
                        request.TimetableId,
                        selectedLesson.LessonId,
                        selectedLesson.Id,
                        request.AvoidSlotsForSelected,
                        includeCurrentSlot: false,
                        request.IgnoredConstraintCodes);

                    // Explicitly filter out current slot to ensure movement
                    destinationSlots = destinationSlots
                        .Where(s => s.DayOfWeek != selectedLesson.DayOfWeek || s.PeriodId != selectedLesson.PeriodId)
                        .ToList();

                    // Sort by quality - explore all available destinations to maximize solution diversity
                    destinationSlots = destinationSlots
                        .OrderByDescending(s => s.QualityScore)
                        .ThenBy(s => s.HasHardConstraintViolations ? 1 : 0)
                        .ToList(); // No limit - use all available destinations
                }

                if (!destinationSlots.Any())
                {
                    progress?.Report(new GenerationProgress
                    {
                        OptionsGenerated = 0,
                        CombinationsExplored = 0,
                        ElapsedTime = stopwatch.Elapsed,
                        StatusMessage = "No available destination slots found"
                    });
                    return options;
                }

                // Determine which algorithm to use
                var algorithmName = request.SelectedAlgorithm switch
                {
                    "multi-step-kempe-chain" => "Multi-Step Kempe Chain",
                    "musical-chairs" => "Musical Chairs",
                    "recursive-conflict-resolution" => "Recursive Conflict Resolution",
                    _ => "Kempe Chain"
                };

                progress?.Report(new GenerationProgress
                {
                    OptionsGenerated = 0,
                    CombinationsExplored = 0,
                    ElapsedTime = stopwatch.Elapsed,
                    StatusMessage = $"Using {algorithmName} algorithm to find alternatives..."
                });

                // === STEP 4: Use selected algorithm to find alternatives ===
                var maxIterations = (int)(request.MaxTimeMinutes * 200); // Scale iterations with time
                List<KempeChainSolution> kempeSolutions;

                if (request.SelectedAlgorithm == "multi-step-kempe-chain")
                {
                    kempeSolutions = await _multiStepKempeSearch.FindAlternativesAsync(
                        request.TimetableId,
                        selectedLesson.Id,
                        maxIterations,
                        maxSolutions: 50,
                        request.IgnoredConstraintCodes,
                        cancellationToken);
                }
                else if (request.SelectedAlgorithm == "musical-chairs")
                {
                    kempeSolutions = await _musicalChairsAlgorithm.FindAlternativesAsync(
                        request.TimetableId,
                        selectedLesson.Id,
                        maxIterations,
                        maxSolutions: 50,
                        request.IgnoredConstraintCodes,
                        cancellationToken);
                }
                else if (request.SelectedAlgorithm == "recursive-conflict-resolution")
                {
                    kempeSolutions = await _recursiveConflictResolution.FindAlternativesAsync(
                        request.TimetableId,
                        selectedLesson.Id,
                        maxIterations,
                        maxSolutions: 50,
                        request.IgnoredConstraintCodes,
                        cancellationToken);
                }
                else
                {
                    kempeSolutions = await _kempeSearch.FindAlternativesAsync(
                        request.TimetableId,
                        selectedLesson.Id,
                        maxIterations,
                        maxSolutions: 50,
                        request.IgnoredConstraintCodes,
                        cancellationToken);
                }

                combinationsExplored = kempeSolutions.Count;

                progress?.Report(new GenerationProgress
                {
                    OptionsGenerated = 0,
                    CombinationsExplored = combinationsExplored,
                    ElapsedTime = stopwatch.Elapsed,
                    StatusMessage = $"Found {kempeSolutions.Count} {algorithmName} solution(s), converting to options..."
                });

                // Convert Kempe chain solutions to timetable options
                foreach (var kempeSolution in kempeSolutions)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Check if solution respects max unfixed lessons limit
                    var unfixedMovesCount = kempeSolution.Movements.Count(m => !m.IsSelectedLesson);
                    if (unfixedMovesCount > request.MaxUnfixedLessonsToMove)
                        continue;

                    // Convert to timetable option
                    var option = await ConvertKempeChainToOptionAsync(
                        kempeSolution,
                        selectedLesson.Id,
                        request.TimetableId,
                        allScheduledLessons,
                        optionNumber++);

                    options.Add(option);

                    // Report progress
                    progress?.Report(new GenerationProgress
                    {
                        OptionsGenerated = options.Count,
                        CombinationsExplored = combinationsExplored,
                        ElapsedTime = stopwatch.Elapsed,
                        StatusMessage = $"Found {options.Count} option(s)..."
                    });
                }

                // === STEP 5: Eliminate identical options ===
                int originalCount = options.Count;
                options = EliminateDuplicateOptions(options);
                int eliminatedCount = originalCount - options.Count;

                if (eliminatedCount > 0)
                {
                    progress?.Report(new GenerationProgress
                    {
                        OptionsGenerated = options.Count,
                        CombinationsExplored = combinationsExplored,
                        ElapsedTime = stopwatch.Elapsed,
                        StatusMessage = $"Eliminated {eliminatedCount} duplicate option(s). {options.Count} unique option(s) remain."
                    });
                }

                // === STEP 6: Sort options by quality ===
                options = options
                    .OrderByDescending(o => o.QualityScore)
                    .ThenBy(o => o.UnfixedLessonsMoved)
                    .ThenBy(o => o.SoftViolationCount)
                    .ToList();

                // Renumber after sorting
                for (int i = 0; i < options.Count; i++)
                {
                    options[i].OptionNumber = i + 1;
                }

                // Final progress report
                progress?.Report(new GenerationProgress
                {
                    OptionsGenerated = options.Count,
                    CombinationsExplored = combinationsExplored,
                    ElapsedTime = stopwatch.Elapsed,
                    StatusMessage = $"Generation complete. Found {options.Count} option(s)."
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
                    StatusMessage = $"Generation stopped by user. Found {options.Count} option(s)."
                });

                return options;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// Generate options and immediately save them as draft timetables in the database
        /// Returns list of saved timetable IDs with metadata
        /// </summary>
        public async Task<List<SavedTimetableOption>> GenerateAndSaveOptionsAsync(
            GenerationRequest request,
            IProgress<GenerationProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            // First, generate all options
            var options = await GenerateOptionsAsync(request, progress, cancellationToken);

            if (!options.Any())
            {
                return new List<SavedTimetableOption>();
            }

            // Load base timetable info for naming
            var baseTimetable = await _context.Timetables
                .FirstOrDefaultAsync(t => t.Id == request.TimetableId, cancellationToken);

            if (baseTimetable == null)
            {
                throw new InvalidOperationException($"Base timetable {request.TimetableId} not found");
            }

            // Save each option as a timetable
            var savedOptions = new List<SavedTimetableOption>();
            int savedCount = 0;

            progress?.Report(new GenerationProgress
            {
                OptionsGenerated = options.Count,
                CombinationsExplored = 0,
                ElapsedTime = TimeSpan.Zero,
                StatusMessage = $"Saving {options.Count} option(s) to database..."
            });

            foreach (var option in options)
            {
                var timetableId = await SaveOptionAsTimetableAsync(
                    option,
                    request.TimetableId,
                    baseTimetable.Name,
                    baseTimetable.SchoolYearId,
                    baseTimetable.TermId,
                    cancellationToken);

                savedOptions.Add(new SavedTimetableOption
                {
                    TimetableId = timetableId,
                    OptionNumber = option.OptionNumber,
                    QualityScore = option.QualityScore,
                    TotalMovedLessons = option.TotalMovedLessons,
                    UnfixedLessonsMoved = option.UnfixedLessonsMoved,
                    SoftViolationCount = option.SoftViolationCount,
                    Movements = option.Movements,
                    SoftViolations = option.SoftViolations,
                    TimetableState = option.TimetableState
                });

                savedCount++;

                progress?.Report(new GenerationProgress
                {
                    OptionsGenerated = savedCount,
                    CombinationsExplored = 0,
                    ElapsedTime = TimeSpan.Zero,
                    StatusMessage = $"Saved {savedCount}/{options.Count} option(s)..."
                });
            }

            progress?.Report(new GenerationProgress
            {
                OptionsGenerated = savedCount,
                CombinationsExplored = 0,
                ElapsedTime = TimeSpan.Zero,
                StatusMessage = $"All {savedCount} option(s) saved successfully!"
            });

            return savedOptions;
        }

        /// <summary>
        /// Load all scheduled lessons for the timetable with complete related data
        /// </summary>
        private async Task<List<ScheduledLesson>> LoadTimetableWithFullDataAsync(int timetableId)
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
        /// Convert a swap chain to a timetable option
        /// </summary>
        private async Task<TimetableOption> ConvertSwapChainToOptionAsync(
            SwapChain chain,
            int selectedLessonId,
            int timetableId,
            List<ScheduledLesson> allScheduledLessons,
            int optionNumber)
        {
            // Convert swap chain steps to lesson movements
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

            // CRITICAL: Verify selected lesson actually moves to a different slot
            var selectedLessonMovement = movements.FirstOrDefault(m => m.IsSelectedLesson);
            if (selectedLessonMovement != null)
            {
                // Ensure the destination is different from source (not same day AND period)
                if (selectedLessonMovement.FromDay == selectedLessonMovement.ToDay &&
                    selectedLessonMovement.FromPeriodId == selectedLessonMovement.ToPeriodId)
                {
                    throw new InvalidOperationException(
                        $"Selected lesson {selectedLessonId} is not actually moving - source and destination are the same!");
                }
            }

            var option = new TimetableOption
            {
                OptionNumber = optionNumber,
                Movements = movements,
                QualityScore = chain.QualityScore,
                SoftViolations = new List<string>() // TODO: Calculate soft violations
            };

            // Build complete timetable state for display
            option.TimetableState = await BuildTimetableStateAsync(
                timetableId,
                movements,
                allScheduledLessons);

            return option;
        }

        /// <summary>
        /// Convert a Kempe chain solution to a timetable option
        /// </summary>
        private async Task<TimetableOption> ConvertKempeChainToOptionAsync(
            KempeChainSolution kempeSolution,
            int selectedLessonId,
            int timetableId,
            List<ScheduledLesson> allScheduledLessons,
            int optionNumber)
        {
            // Get period names for display
            var periods = await _context.Periods.ToDictionaryAsync(p => p.Id, p => p.Name);

            // Convert Kempe chain moves to lesson movements
            var movements = kempeSolution.Movements.Select(move => new LessonMovement
            {
                ScheduledLessonId = move.ScheduledLessonId,
                LessonId = move.LessonId,
                LessonDescription = move.LessonDescription,
                IsSelectedLesson = move.IsSelectedLesson,
                FromDay = move.FromSlot.Day,
                FromPeriodId = move.FromSlot.PeriodId,
                FromPeriodName = periods.GetValueOrDefault(move.FromSlot.PeriodId, $"Period {move.FromSlot.PeriodId}"),
                ToDay = move.ToSlot.Day,
                ToPeriodId = move.ToSlot.PeriodId,
                ToPeriodName = periods.GetValueOrDefault(move.ToSlot.PeriodId, $"Period {move.ToSlot.PeriodId}"),
                FromRoomId = null, // Kempe chains focus on timeslot swaps, rooms handled separately
                ToRoomId = null
            }).ToList();

            // CRITICAL: Verify selected lesson actually moves to a different slot
            var selectedLessonMovement = movements.FirstOrDefault(m => m.IsSelectedLesson);
            if (selectedLessonMovement != null)
            {
                // Ensure the destination is different from source (not same day AND period)
                if (selectedLessonMovement.FromDay == selectedLessonMovement.ToDay &&
                    selectedLessonMovement.FromPeriodId == selectedLessonMovement.ToPeriodId)
                {
                    throw new InvalidOperationException(
                        $"Selected lesson {selectedLessonId} is not actually moving - source and destination are the same!");
                }
            }

            // CRITICAL: Verify no lesson moves back to its original position
            // Get original positions from allScheduledLessons
            var originalPositions = allScheduledLessons.ToDictionary(
                sl => sl.Id,
                sl => (Day: sl.DayOfWeek, PeriodId: sl.PeriodId));

            foreach (var movement in movements)
            {
                if (originalPositions.TryGetValue(movement.ScheduledLessonId, out var original))
                {
                    // Check if this lesson ends up back at its original position
                    if (movement.ToDay == original.Day && movement.ToPeriodId == original.PeriodId)
                    {
                        throw new InvalidOperationException(
                            $"Invalid solution: Lesson {movement.ScheduledLessonId} ({movement.LessonDescription}) " +
                            $"moves back to its original position {original.Day}:{original.PeriodId}");
                    }
                }
            }

            var option = new TimetableOption
            {
                OptionNumber = optionNumber,
                Movements = movements,
                QualityScore = kempeSolution.QualityScore,
                SoftViolations = new List<string>() // TODO: Calculate soft violations
            };

            // Build complete timetable state for display
            option.TimetableState = await BuildTimetableStateAsync(
                timetableId,
                movements,
                allScheduledLessons);

            return option;
        }

        /// <summary>
        /// Build the complete timetable state after applying movements
        /// This is what the UI displays
        /// </summary>
        private async Task<Dictionary<string, SlotContent>> BuildTimetableStateAsync(
            int timetableId,
            List<LessonMovement> movements,
            List<ScheduledLesson> allScheduledLessons)
        {
            var state = new Dictionary<string, SlotContent>();
            var movedLessonIds = movements.Select(m => m.ScheduledLessonId).ToHashSet();

            // Create working copy of all lessons
            var workingLessons = allScheduledLessons.Select(sl => new
            {
                ScheduledLesson = sl,
                CurrentDay = sl.DayOfWeek,
                CurrentPeriodId = sl.PeriodId,
                WasMoved = false
            }).ToList();

            // Apply all movements
            var updatedLessons = workingLessons.Select(item =>
            {
                var movement = movements.FirstOrDefault(m => m.ScheduledLessonId == item.ScheduledLesson.Id);
                if (movement != null)
                {
                    return new
                    {
                        item.ScheduledLesson,
                        CurrentDay = movement.ToDay,
                        CurrentPeriodId = movement.ToPeriodId,
                        WasMoved = true
                    };
                }
                return item;
            }).ToList();

            // Build state dictionary (grouped by slot)
            foreach (var item in updatedLessons)
            {
                var slotKey = $"{item.CurrentDay}_{item.CurrentPeriodId}";

                if (!state.ContainsKey(slotKey))
                    state[slotKey] = new SlotContent();

                state[slotKey].Lessons.Add(new SlotLesson
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
        /// Get human-readable description of a lesson
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
        /// Save a timetable option as a draft timetable in the database
        /// </summary>
        private async Task<int> SaveOptionAsTimetableAsync(
            TimetableOption option,
            int baseTimetableId,
            string baseTimetableName,
            int schoolYearId,
            int? termId,
            CancellationToken cancellationToken = default)
        {
            // Generate timetable name with timestamp
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
            var name = $"{baseTimetableName}_Option{option.OptionNumber}_{timestamp}";

            // Create new timetable
            var newTimetable = new Timetable
            {
                Name = name,
                SchoolYearId = schoolYearId,
                TermId = termId,
                CreatedDate = DateTime.UtcNow,
                Status = TimetableStatus.Draft,
                Notes = $"Generated option {option.OptionNumber} from {baseTimetableName} - Quality Score: {option.QualityScore:F2}, Movements: {option.TotalMovedLessons}"
            };

            _context.Timetables.Add(newTimetable);
            await _context.SaveChangesAsync(cancellationToken);

            // Load base timetable scheduled lessons
            var baseScheduledLessons = await _context.ScheduledLessons
                .Include(sl => sl.ScheduledLessonRooms)
                .Where(sl => sl.TimetableId == baseTimetableId)
                .ToListAsync(cancellationToken);

            // Create map from old ID to new ScheduledLesson
            var scheduledLessonsMap = new Dictionary<int, ScheduledLesson>();

            // Clone all scheduled lessons
            foreach (var sl in baseScheduledLessons)
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

            await _context.SaveChangesAsync(cancellationToken);

            // Apply movements
            foreach (var movement in option.Movements)
            {
                if (scheduledLessonsMap.TryGetValue(movement.ScheduledLessonId, out var sl))
                {
                    sl.DayOfWeek = movement.ToDay;
                    sl.PeriodId = movement.ToPeriodId;
                    if (movement.ToRoomId.HasValue)
                    {
                        sl.RoomId = movement.ToRoomId;
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            return newTimetable.Id;
        }

        /// <summary>
        /// Eliminate duplicate options based on their movements
        /// Two options are considered identical if they move the same lessons to the same destinations
        /// </summary>
        private List<TimetableOption> EliminateDuplicateOptions(List<TimetableOption> options)
        {
            var uniqueOptions = new List<TimetableOption>();
            var seenSignatures = new HashSet<string>();

            foreach (var option in options)
            {
                // Create a signature for this option based on its movements
                // Signature format: "lessonId:fromDay:fromPeriod->toDay:toPeriod|..."
                var signature = string.Join("|",
                    option.Movements
                        .OrderBy(m => m.ScheduledLessonId) // Sort for consistent comparison
                        .Select(m => $"{m.ScheduledLessonId}:{m.FromDay}:{m.FromPeriodId}->{m.ToDay}:{m.ToPeriodId}"));

                // Only add if we haven't seen this signature before
                if (!seenSignatures.Contains(signature))
                {
                    seenSignatures.Add(signature);
                    uniqueOptions.Add(option);
                }
            }

            return uniqueOptions;
        }
    }
}
