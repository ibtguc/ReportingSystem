using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;
using SchedulingSystem.Services.Constraints;

namespace SchedulingSystem.Services;

/// <summary>
/// Scheduling service using Simulated Annealing algorithm with centralized constraint validation
/// This algorithm uses probabilistic optimization to find better schedules
/// by exploring the solution space and accepting worse solutions with decreasing probability
/// </summary>
public class SchedulingServiceSimulatedAnnealing
{
    private readonly ApplicationDbContext _context;
    private readonly IConstraintValidator _constraintValidator;
    private Random _random;

    public SchedulingServiceSimulatedAnnealing(
        ApplicationDbContext context,
        IConstraintValidator constraintValidator)
    {
        _context = context;
        _constraintValidator = constraintValidator;
        _random = new Random();
    }

    /// <summary>
    /// Generate a timetable using Simulated Annealing algorithm
    /// </summary>
    public async Task<SchedulingResult> GenerateTimetableAsync(
        int schoolYearId,
        string timetableName,
        SimulatedAnnealingConfig? config = null)
    {
        config ??= SimulatedAnnealingConfig.Balanced;

        // Set random seed if provided
        if (config.RandomSeed.HasValue)
        {
            _random = new Random(config.RandomSeed.Value);
        }

        var result = new SchedulingResult();

        try
        {
            // Step 1: Create timetable
            var timetable = new Timetable
            {
                Name = timetableName,
                SchoolYearId = schoolYearId,
                Status = TimetableStatus.Draft,
                CreatedDate = DateTime.UtcNow
            };

            _context.Timetables.Add(timetable);
            await _context.SaveChangesAsync();

            result.TimetableId = timetable.Id;

            // Step 2: Load all required data
            var schoolYear = await _context.SchoolYears
                .FirstOrDefaultAsync(sy => sy.Id == schoolYearId);

            if (schoolYear == null)
            {
                result.Errors.Add("School year not found");
                return result;
            }

            var lessons = await _context.Lessons
                .Include(l => l.LessonSubjects)
                    .ThenInclude(ls => ls.Subject)
                .Include(l => l.LessonTeachers)
                    .ThenInclude(lt => lt.Teacher)
                .Include(l => l.LessonClasses)
                    .ThenInclude(lc => lc.Class)
                .Where(l => l.IsActive)
                .ToListAsync();

            var rooms = await _context.Rooms.Where(r => r.IsActive).ToListAsync();
            var periods = await _context.Periods
                .Where(p => !p.IsBreak)
                .OrderBy(p => p.PeriodNumber)
                .ToListAsync();

            // Load availability constraints for all entities
            var teacherAvailabilities = await _context.TeacherAvailabilities.ToListAsync();
            var classAvailabilities = await _context.ClassAvailabilities.ToListAsync();
            var roomAvailabilities = await _context.RoomAvailabilities.ToListAsync();
            var subjectAvailabilities = await _context.SubjectAvailabilities.ToListAsync();

            result.TotalCount = lessons.Sum(l => l.FrequencyPerWeek);

            if (lessons.Count() == 0)
            {
                result.Errors.Add("No active lessons to schedule");
                return result;
            }

            if (periods.Count() == 0)
            {
                result.Errors.Add("No periods defined for this school year");
                return result;
            }

            // Step 3: Generate initial solution using greedy approach
            var initialSolution = await GenerateInitialSolutionAsync(
                lessons, periods, rooms, timetable.Id);

            result.ScheduledCount = initialSolution.Count;

            if (initialSolution.Count == 0)
            {
                result.Errors.Add("Could not generate initial solution");
                return result;
            }

            // Step 4: Apply Simulated Annealing optimization
            var optimizedSolution = await OptimizeWithSimulatedAnnealingAsync(
                initialSolution, lessons, periods, rooms, timetable.Id, config,
                teacherAvailabilities, classAvailabilities, roomAvailabilities, subjectAvailabilities);

            // Step 5: Save optimized solution to database
            foreach (var scheduledLesson in optimizedSolution)
            {
                _context.ScheduledLessons.Add(scheduledLesson);
            }

            await _context.SaveChangesAsync();

            // Step 6: Calculate quality metrics
            result.QualityMetrics = CalculateQualityMetrics(optimizedSolution, lessons, config.Weights);
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Error during scheduling: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Generate initial feasible solution using greedy algorithm
    /// </summary>
#pragma warning disable CS1998 // Async method lacks 'await' operators - designed for future async operations
    private async Task<List<ScheduledLesson>> GenerateInitialSolutionAsync(
        List<Lesson> lessons,
        List<Period> periods,
        List<Room> rooms,
        int timetableId)
    {
        var scheduledLessons = new List<ScheduledLesson>();
        var daysOfWeek = new[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday };
        var lessonDict = lessons.ToDictionary(l => l.Id);

        // Sort lessons by priority (fewer instances first, then by special requirements)
        var lessonsToSchedule = lessons
            .OrderBy(l => l.FrequencyPerWeek)
            .ThenByDescending(l => !string.IsNullOrEmpty(l.RequiredRoomType))
            .ToList();

        foreach (var lesson in lessonsToSchedule)
        {
            for (int instance = 0; instance < lesson.FrequencyPerWeek; instance++)
            {
                bool scheduled = false;

                // Try each day
                foreach (var day in daysOfWeek)
                {
                    // Try each period
                    foreach (var period in periods)
                    {
                        // Find available room with sufficient capacity
                        Room? assignedRoom = rooms
                            .Where(r => r.Capacity >= (lesson.LessonClasses.FirstOrDefault()?.Class?.StudentCount ?? 0))
                            .FirstOrDefault(r => IsRoomAvailable(r.Id, day, period.Id, scheduledLessons));

                        if (assignedRoom == null)
                            continue;

                        // Check hard constraints
                        var primaryTeacherId = lesson.LessonTeachers.FirstOrDefault()?.TeacherId;
                        var classId = lesson.LessonClasses.FirstOrDefault()?.ClassId;
                        if (primaryTeacherId.HasValue && classId.HasValue &&
                            IsTeacherAvailable(primaryTeacherId.Value, day, period.Id, scheduledLessons, lessonDict) &&
                            IsClassAvailable(classId.Value, day, period.Id, scheduledLessons, lessonDict) &&
                            !WouldCreateGapForClass(classId.Value, day, period.Id, scheduledLessons, lessonDict))
                        {
                            // Schedule this lesson
                            var scheduledLesson = new ScheduledLesson
                            {
                                TimetableId = timetableId,
                                LessonId = lesson.Id,
                                Lesson = lesson, // Set navigation property for constraint checking
                                DayOfWeek = day,
                                PeriodId = period.Id,
                                RoomId = assignedRoom.Id
                            };

                            scheduledLessons.Add(scheduledLesson);
                            scheduled = true;
                            break;
                        }
                    }

                    if (scheduled) break;
                }
            }
        }

        return scheduledLessons;
    }

    /// <summary>
    /// Optimize schedule using Simulated Annealing
    /// </summary>
#pragma warning disable CS1998 // Async method lacks 'await' operators - designed for future async operations
    private async Task<List<ScheduledLesson>> OptimizeWithSimulatedAnnealingAsync(
        List<ScheduledLesson> initialSolution,
        List<Lesson> lessons,
        List<Period> periods,
        List<Room> rooms,
        int timetableId,
        SimulatedAnnealingConfig config,
        List<TeacherAvailability> teacherAvailabilities,
        List<ClassAvailability> classAvailabilities,
        List<RoomAvailability> roomAvailabilities,
        List<SubjectAvailability> subjectAvailabilities)
    {
        var currentSolution = initialSolution.Select(sl => new ScheduledLesson
        {
            TimetableId = sl.TimetableId,
            LessonId = sl.LessonId,
            DayOfWeek = sl.DayOfWeek,
            PeriodId = sl.PeriodId,
            RoomId = sl.RoomId
        }).ToList();

        var bestSolution = currentSolution.Select(sl => new ScheduledLesson
        {
            TimetableId = sl.TimetableId,
            LessonId = sl.LessonId,
            DayOfWeek = sl.DayOfWeek,
            PeriodId = sl.PeriodId,
            RoomId = sl.RoomId
        }).ToList();

        double currentEnergy = CalculateEnergy(currentSolution, lessons, config.Weights,
            teacherAvailabilities, classAvailabilities, roomAvailabilities, subjectAvailabilities);
        double bestEnergy = currentEnergy;

        double temperature = config.InitialTemperature;
        int iterationsWithoutImprovement = 0;
        int totalIterations = 0;

        var daysOfWeek = new[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday };

        // Main annealing loop
        while (temperature > config.FinalTemperature && iterationsWithoutImprovement < config.MaxIterationsWithoutImprovement)
        {
            for (int iter = 0; iter < config.IterationsPerTemperature; iter++)
            {
                totalIterations++;

                // Generate neighbor solution
                var neighborSolution = GenerateNeighborSolution(currentSolution, lessons, periods, rooms, daysOfWeek);

                // Calculate energy of neighbor
                double neighborEnergy = CalculateEnergy(neighborSolution, lessons, config.Weights,
                    teacherAvailabilities, classAvailabilities, roomAvailabilities, subjectAvailabilities);

                // Decide whether to accept neighbor
                double energyDelta = neighborEnergy - currentEnergy;

                if (energyDelta < 0 || _random.NextDouble() < Math.Exp(-energyDelta / temperature))
                {
                    // Accept neighbor
                    currentSolution = neighborSolution;
                    currentEnergy = neighborEnergy;

                    // Update best solution if improved
                    if (currentEnergy < bestEnergy)
                    {
                        bestSolution = currentSolution.Select(sl => new ScheduledLesson
                        {
                            TimetableId = sl.TimetableId,
                            LessonId = sl.LessonId,
                            DayOfWeek = sl.DayOfWeek,
                            PeriodId = sl.PeriodId,
                            RoomId = sl.RoomId
                        }).ToList();
                        bestEnergy = currentEnergy;
                        iterationsWithoutImprovement = 0;
                    }
                    else
                    {
                        iterationsWithoutImprovement++;
                    }
                }
                else
                {
                    iterationsWithoutImprovement++;
                }

                // Early stopping if optimal solution found
                if (bestEnergy == 0)
                {
                    break;
                }
            }

            // Cool down
            temperature *= config.CoolingRate;

            if (bestEnergy == 0)
            {
                break;
            }
        }

        return bestSolution;
    }

    /// <summary>
    /// Generate a neighbor solution by making a small random change
    /// </summary>
    private List<ScheduledLesson> GenerateNeighborSolution(
        List<ScheduledLesson> currentSolution,
        List<Lesson> lessons,
        List<Period> periods,
        List<Room> rooms,
        DayOfWeek[] daysOfWeek)
    {
        var neighbor = currentSolution.Select(sl => new ScheduledLesson
        {
            TimetableId = sl.TimetableId,
            LessonId = sl.LessonId,
            DayOfWeek = sl.DayOfWeek,
            PeriodId = sl.PeriodId,
            RoomId = sl.RoomId
        }).ToList();

        if (neighbor.Count == 0)
            return neighbor;

        // Choose random operation
        double operation = _random.NextDouble();

        if (operation < 0.4)
        {
            // Operation 1: Swap two lessons (40% probability)
            SwapTwoLessons(neighbor);
        }
        else if (operation < 0.7)
        {
            // Operation 2: Move one lesson to a different time slot (30% probability)
            MoveLessonToNewTimeSlot(neighbor, lessons, periods, rooms, daysOfWeek);
        }
        else
        {
            // Operation 3: Change room assignment (30% probability)
            ChangeRoomAssignment(neighbor, lessons, rooms);
        }

        return neighbor;
    }

    /// <summary>
    /// Swap time slots and rooms of two randomly selected lessons
    /// </summary>
    private void SwapTwoLessons(List<ScheduledLesson> solution)
    {
        if (solution.Count < 2)
            return;

        int idx1 = _random.Next(solution.Count);
        int idx2 = _random.Next(solution.Count);

        while (idx1 == idx2 && solution.Count > 1)
        {
            idx2 = _random.Next(solution.Count);
        }

        // Swap day, period, and room
        (solution[idx1].DayOfWeek, solution[idx2].DayOfWeek) = (solution[idx2].DayOfWeek, solution[idx1].DayOfWeek);
        (solution[idx1].PeriodId, solution[idx2].PeriodId) = (solution[idx2].PeriodId, solution[idx1].PeriodId);
        (solution[idx1].RoomId, solution[idx2].RoomId) = (solution[idx2].RoomId, solution[idx1].RoomId);
    }

    /// <summary>
    /// Move a randomly selected lesson to a new time slot
    /// </summary>
    private void MoveLessonToNewTimeSlot(
        List<ScheduledLesson> solution,
        List<Lesson> lessons,
        List<Period> periods,
        List<Room> rooms,
        DayOfWeek[] daysOfWeek)
    {
        if (solution.Count == 0)
            return;

        int idx = _random.Next(solution.Count);
        var lesson = solution[idx];

        // Choose random new day and period
        var newDay = daysOfWeek[_random.Next(daysOfWeek.Length)];
        var newPeriod = periods[_random.Next(periods.Count)];

        lesson.DayOfWeek = newDay;
        lesson.PeriodId = newPeriod.Id;
    }

    /// <summary>
    /// Change room assignment for a randomly selected lesson
    /// </summary>
    private void ChangeRoomAssignment(
        List<ScheduledLesson> solution,
        List<Lesson> lessons,
        List<Room> rooms)
    {
        if (solution.Count == 0 || rooms.Count == 0)
            return;

        int idx = _random.Next(solution.Count);
        var scheduledLesson = solution[idx];

        var lesson = lessons.FirstOrDefault(l => l.Id == scheduledLesson.LessonId);
        if (lesson == null)
            return;

        // Find suitable rooms
        var suitableRooms = rooms
            .Where(r => r.Capacity >= (lesson.LessonClasses.FirstOrDefault()?.Class?.StudentCount ?? 0))
            .ToList();

        if (suitableRooms.Any())
        {
            var newRoom = suitableRooms[_random.Next(suitableRooms.Count)];
            scheduledLesson.RoomId = newRoom.Id;
        }
    }

    /// <summary>
    /// Calculate energy (cost) of a solution - lower is better
    /// Energy is based on violations of soft constraints (including availability)
    /// </summary>
    private double CalculateEnergy(
        List<ScheduledLesson> solution,
        List<Lesson> lessons,
        SoftConstraintWeights weights,
        List<TeacherAvailability> teacherAvailabilities,
        List<ClassAvailability> classAvailabilities,
        List<RoomAvailability> roomAvailabilities,
        List<SubjectAvailability> subjectAvailabilities)
    {
        if (!weights.Enabled)
            return 0;

        double energy = 0;

        // Load lesson details
        var lessonDict = lessons.ToDictionary(l => l.Id);

        // Penalty 1: Hard constraint violations (these should be minimized with high weight)
        energy += CalculateHardConstraintViolations(solution, lessonDict) * 10000;

        // Penalty 2: Teacher NTPs (gaps in schedule)
        energy += CalculateTeacherNTPs(solution, lessonDict) * weights.MinimizeTeacherNTPs;

        // Penalty 3: Student NTPs (gaps in schedule)
        energy += CalculateStudentNTPs(solution, lessonDict) * weights.MinimizeStudentNTPs;

        // Penalty 4: Uneven distribution (too many lessons on one day)
        energy += CalculateUnevenDistribution(solution, lessonDict) * weights.EvenDistribution;

        // Penalty 5: Non-preferred time slots
        energy += CalculateNonPreferredTimeSlots(solution, lessonDict) * weights.PreferredTimeSlot;

        // Penalty 6: Room changes for teachers
        energy += CalculateRoomChanges(solution, lessonDict) * weights.MinimizeRoomChanges;

        // Penalty 7: Unbalanced workload
        energy += CalculateUnbalancedWorkload(solution, lessonDict) * weights.BalancedWorkload;

        // Penalty 8: Consecutive same subject for a class
        energy += CalculateConsecutiveSameSubject(solution, lessonDict) * 100; // Heavy penalty

        // Penalty 9: Availability constraint violations (UNTIS GPU016.TXT)
        // Negative availability score = penalty, so negate it to add as energy
        energy += -CalculateAvailabilityPenalty(solution, lessonDict, teacherAvailabilities,
            classAvailabilities, roomAvailabilities, subjectAvailabilities) * weights.AvailabilityWeight;

        return energy;
    }

    /// <summary>
    /// Calculate hard constraint violations (double bookings and UNTIS constraints)
    /// NOTE: SA uses its own optimized constraint counting for performance in the annealing process.
    /// This differs from other services which use IConstraintValidator.
    /// Future enhancement: Consider integrating with IConstraintValidator for consistency.
    /// </summary>
    private int CalculateHardConstraintViolations(List<ScheduledLesson> solution, Dictionary<int, Lesson> lessonDict)
    {
        int violations = 0;

        // Check double booking violations
        for (int i = 0; i < solution.Count; i++)
        {
            for (int j = i + 1; j < solution.Count; j++)
            {
                var lesson1 = solution[i];
                var lesson2 = solution[j];

                // Same time slot
                if (lesson1.DayOfWeek == lesson2.DayOfWeek && lesson1.PeriodId == lesson2.PeriodId)
                {
                    var l1 = lessonDict.GetValueOrDefault(lesson1.LessonId);
                    var l2 = lessonDict.GetValueOrDefault(lesson2.LessonId);

                    if (l1 != null && l2 != null)
                    {
                        // Same teacher
                        var l1TeacherId = l1.LessonTeachers.FirstOrDefault()?.TeacherId;
                        var l2TeacherId = l2.LessonTeachers.FirstOrDefault()?.TeacherId;
                        if (l1TeacherId.HasValue && l2TeacherId.HasValue && l1TeacherId == l2TeacherId)
                            violations++;

                        // Same class
                        var l1ClassId = l1.LessonClasses.FirstOrDefault()?.ClassId;
                        var l2ClassId = l2.LessonClasses.FirstOrDefault()?.ClassId;
                        if (l1ClassId.HasValue && l2ClassId.HasValue && l1ClassId == l2ClassId)
                            violations++;

                        // Same room
                        if (lesson1.RoomId == lesson2.RoomId)
                            violations++;
                    }
                }
            }
        }

        // Check UNTIS constraint violations
        violations += CalculateUntisConstraintViolations(solution, lessonDict);

        return violations;
    }

    /// <summary>
    /// Calculate UNTIS-specific constraint violations
    /// </summary>
    private int CalculateUntisConstraintViolations(List<ScheduledLesson> solution, Dictionary<int, Lesson> lessonDict)
    {
        int violations = 0;

        // Group by teacher and day to check max periods per day
        var teacherDayGroups = solution
            .Where(sl => lessonDict.ContainsKey(sl.LessonId))
            .Select(sl => new {
                ScheduledLesson = sl,
                TeacherId = lessonDict[sl.LessonId].LessonTeachers.FirstOrDefault()?.TeacherId,
                Teacher = lessonDict[sl.LessonId].LessonTeachers.FirstOrDefault()?.Teacher
            })
            .Where(x => x.TeacherId.HasValue)
            .GroupBy(x => new {
                x.TeacherId,
                Day = x.ScheduledLesson.DayOfWeek,
                x.Teacher
            });

        foreach (var group in teacherDayGroups)
        {
            if (group.Key.Teacher?.MaxPeriodsPerDay.HasValue == true)
            {
                int periodsToday = group.Select(x => x.ScheduledLesson.PeriodId).Distinct().Count();
                if (periodsToday > group.Key.Teacher.MaxPeriodsPerDay.Value)
                    violations += (periodsToday - group.Key.Teacher.MaxPeriodsPerDay.Value);
            }

            // Check max consecutive periods for teacher
            if (group.Key.Teacher?.MaxConsecutivePeriods.HasValue == true)
            {
                var periods = group.Select(x => x.ScheduledLesson.PeriodId).Distinct().OrderBy(p => p).ToList();
                violations += CountConsecutiveViolations(periods, group.Key.Teacher.MaxConsecutivePeriods.Value);
            }
        }

        // Group by class and day to check max periods per day
        var classDayGroups = solution
            .Where(sl => lessonDict.ContainsKey(sl.LessonId))
            .Select(sl => new {
                ScheduledLesson = sl,
                ClassId = lessonDict[sl.LessonId].LessonClasses.FirstOrDefault()?.ClassId,
                Class = lessonDict[sl.LessonId].LessonClasses.FirstOrDefault()?.Class
            })
            .Where(x => x.ClassId.HasValue)
            .GroupBy(x => new {
                x.ClassId,
                Day = x.ScheduledLesson.DayOfWeek,
                x.Class
            });

        foreach (var group in classDayGroups)
        {
            if (group.Key.Class?.MaxPeriodsPerDay.HasValue == true)
            {
                int periodsToday = group.Select(x => x.ScheduledLesson.PeriodId).Distinct().Count();
                if (periodsToday > group.Key.Class.MaxPeriodsPerDay.Value)
                    violations += (periodsToday - group.Key.Class.MaxPeriodsPerDay.Value);
            }

            // Check max consecutive subjects for class
            if (group.Key.Class?.MaxConsecutiveSubjects.HasValue == true)
            {
                var subjectPeriods = group
                    .GroupBy(x => lessonDict[x.ScheduledLesson.LessonId].LessonSubjects.FirstOrDefault()?.SubjectId)
                    .Select(g => g.Select(x => x.ScheduledLesson.PeriodId).Distinct().OrderBy(p => p).ToList());

                foreach (var periods in subjectPeriods)
                {
                    violations += CountConsecutiveViolations(periods, group.Key.Class.MaxConsecutiveSubjects.Value);
                }
            }
        }

        return violations;
    }

    /// <summary>
    /// Count how many times consecutive period limit is violated
    /// </summary>
    private int CountConsecutiveViolations(List<int> periods, int maxConsecutive)
    {
        int violations = 0;
        int currentStreak = 1;

        for (int i = 1; i < periods.Count; i++)
        {
            if (periods[i] == periods[i - 1] + 1)
            {
                currentStreak++;
                if (currentStreak > maxConsecutive)
                    violations++;
            }
            else
            {
                currentStreak = 1;
            }
        }

        return violations;
    }

    /// <summary>
    /// Calculate total teacher NTPs (gaps)
    /// </summary>
    private int CalculateTeacherNTPs(List<ScheduledLesson> solution, Dictionary<int, Lesson> lessonDict)
    {
        var teacherSchedules = solution
            .Where(sl => lessonDict.ContainsKey(sl.LessonId))
            .Select(sl => new {
                ScheduledLesson = sl,
                TeacherId = lessonDict[sl.LessonId].LessonTeachers.FirstOrDefault()?.TeacherId
            })
            .Where(x => x.TeacherId.HasValue)
            .GroupBy(x => x.TeacherId.Value);

        int totalNTPs = 0;

        foreach (var teacherSchedule in teacherSchedules)
        {
            var byDay = teacherSchedule.GroupBy(x => x.ScheduledLesson.DayOfWeek);

            foreach (var daySchedule in byDay)
            {
                var periods = daySchedule.Select(x => x.ScheduledLesson.PeriodId).OrderBy(p => p).ToList();

                if (periods.Count > 1)
                {
                    // Count gaps between first and last period
                    int gaps = (periods.Max() - periods.Min() + 1) - periods.Distinct().Count();
                    totalNTPs += gaps;
                }
            }
        }

        return totalNTPs;
    }

    /// <summary>
    /// Calculate total student NTPs (gaps)
    /// </summary>
    private int CalculateStudentNTPs(List<ScheduledLesson> solution, Dictionary<int, Lesson> lessonDict)
    {
        var classSchedules = solution
            .Where(sl => lessonDict.ContainsKey(sl.LessonId))
            .Select(sl => new {
                ScheduledLesson = sl,
                ClassId = lessonDict[sl.LessonId].LessonClasses.FirstOrDefault()?.ClassId
            })
            .Where(x => x.ClassId.HasValue)
            .GroupBy(x => x.ClassId.Value);

        int totalNTPs = 0;

        foreach (var classSchedule in classSchedules)
        {
            var byDay = classSchedule.GroupBy(x => x.ScheduledLesson.DayOfWeek);

            foreach (var daySchedule in byDay)
            {
                var periods = daySchedule.Select(x => x.ScheduledLesson.PeriodId).OrderBy(p => p).ToList();

                if (periods.Count > 1)
                {
                    int gaps = (periods.Max() - periods.Min() + 1) - periods.Distinct().Count();
                    totalNTPs += gaps;
                }
            }
        }

        return totalNTPs;
    }

    /// <summary>
    /// Calculate uneven distribution penalty
    /// </summary>
    private int CalculateUnevenDistribution(List<ScheduledLesson> solution, Dictionary<int, Lesson> lessonDict)
    {
        var lessonsByDay = solution.GroupBy(sl => sl.DayOfWeek);
        var countsPerDay = lessonsByDay.Select(g => g.Count()).ToList();

        if (!countsPerDay.Any())
            return 0;

        double avg = countsPerDay.Average();
        double variance = countsPerDay.Sum(c => Math.Pow(c - avg, 2)) / countsPerDay.Count;

        return (int)(variance * 10); // Scale variance
    }

    /// <summary>
    /// Calculate non-preferred time slot penalty
    /// </summary>
    private int CalculateNonPreferredTimeSlots(List<ScheduledLesson> solution, Dictionary<int, Lesson> lessonDict)
    {
        int penalty = 0;

        // Simple heuristic: Math should be in morning (period 1-3), Arts in afternoon
        foreach (var sl in solution)
        {
            var lesson = lessonDict.GetValueOrDefault(sl.LessonId);
            var subject = lesson?.LessonSubjects.FirstOrDefault()?.Subject;
            if (subject != null)
            {
                // This is a simplified check - in real implementation, you'd check actual period times
                if (subject.Name.Contains("Math", StringComparison.OrdinalIgnoreCase) && sl.PeriodId > 3)
                    penalty++;
                else if (subject.Name.Contains("Art", StringComparison.OrdinalIgnoreCase) && sl.PeriodId <= 3)
                    penalty++;
            }
        }

        return penalty;
    }

    /// <summary>
    /// Calculate room change penalty for teachers
    /// </summary>
    private int CalculateRoomChanges(List<ScheduledLesson> solution, Dictionary<int, Lesson> lessonDict)
    {
        var teacherSchedules = solution
            .Where(sl => lessonDict.ContainsKey(sl.LessonId))
            .Select(sl => new {
                ScheduledLesson = sl,
                TeacherId = lessonDict[sl.LessonId].LessonTeachers.FirstOrDefault()?.TeacherId
            })
            .Where(x => x.TeacherId.HasValue)
            .GroupBy(x => x.TeacherId.Value);

        int totalChanges = 0;

        foreach (var teacherSchedule in teacherSchedules)
        {
            var roomsUsed = teacherSchedule.Select(x => x.ScheduledLesson.RoomId).Distinct().Count();
            totalChanges += Math.Max(0, roomsUsed - 1); // Penalty for using more than 1 room
        }

        return totalChanges;
    }

    /// <summary>
    /// Calculate unbalanced workload penalty
    /// </summary>
    private int CalculateUnbalancedWorkload(List<ScheduledLesson> solution, Dictionary<int, Lesson> lessonDict)
    {
        var teacherSchedules = solution
            .Where(sl => lessonDict.ContainsKey(sl.LessonId))
            .Select(sl => new {
                ScheduledLesson = sl,
                TeacherId = lessonDict[sl.LessonId].LessonTeachers.FirstOrDefault()?.TeacherId
            })
            .Where(x => x.TeacherId.HasValue)
            .GroupBy(x => x.TeacherId.Value);

        int totalImbalance = 0;

        foreach (var teacherSchedule in teacherSchedules)
        {
            var byDay = teacherSchedule.GroupBy(x => x.ScheduledLesson.DayOfWeek);
            var countsPerDay = byDay.Select(g => g.Count()).ToList();

            if (countsPerDay.Any())
            {
                double avg = countsPerDay.Average();
                double imbalance = countsPerDay.Sum(c => Math.Abs(c - avg));
                totalImbalance += (int)imbalance;
            }
        }

        return totalImbalance;
    }

    /// <summary>
    /// Calculate penalty for consecutive same subjects for classes
    /// </summary>
    private int CalculateConsecutiveSameSubject(List<ScheduledLesson> solution, Dictionary<int, Lesson> lessonDict)
    {
        int violations = 0;

        // Group by class and day
        var classDayGroups = solution
            .Where(sl => lessonDict.ContainsKey(sl.LessonId))
            .Select(sl => new {
                ScheduledLesson = sl,
                ClassId = lessonDict[sl.LessonId].LessonClasses.FirstOrDefault()?.ClassId
            })
            .Where(x => x.ClassId.HasValue)
            .GroupBy(x => new { x.ClassId, x.ScheduledLesson.DayOfWeek });

        foreach (var group in classDayGroups)
        {
            // Get lessons sorted by period
            var dayLessons = group
                .OrderBy(x => x.ScheduledLesson.PeriodId)
                .Select(x => new {
                    x.ScheduledLesson.PeriodId,
                    SubjectId = lessonDict[x.ScheduledLesson.LessonId].LessonSubjects.FirstOrDefault()?.SubjectId
                })
                .ToList();

            // Check for consecutive same subjects
            for (int i = 0; i < dayLessons.Count - 1; i++)
            {
                if (dayLessons[i].PeriodId + 1 == dayLessons[i + 1].PeriodId &&
                    dayLessons[i].SubjectId.HasValue &&
                    dayLessons[i].SubjectId == dayLessons[i + 1].SubjectId)
                {
                    violations++;
                }
            }
        }

        return violations;
    }

    /// <summary>
    /// Calculate quality metrics for reporting
    /// </summary>
    private QualityMetrics CalculateQualityMetrics(
        List<ScheduledLesson> solution,
        List<Lesson> lessons,
        SoftConstraintWeights weights)
    {
        var lessonDict = lessons.ToDictionary(l => l.Id);

        var teacherNTPs = new Dictionary<int, int>();
        var studentNTPs = new Dictionary<int, int>();

        // Calculate teacher NTPs
        var teacherSchedules = solution
            .Where(sl => lessonDict.ContainsKey(sl.LessonId))
            .Select(sl => new {
                ScheduledLesson = sl,
                TeacherId = lessonDict[sl.LessonId].LessonTeachers.FirstOrDefault()?.TeacherId
            })
            .Where(x => x.TeacherId.HasValue)
            .GroupBy(x => x.TeacherId.Value);

        foreach (var teacherSchedule in teacherSchedules)
        {
            int ntps = 0;
            var byDay = teacherSchedule.GroupBy(x => x.ScheduledLesson.DayOfWeek);

            foreach (var daySchedule in byDay)
            {
                var periods = daySchedule.Select(x => x.ScheduledLesson.PeriodId).OrderBy(p => p).ToList();
                if (periods.Count > 1)
                {
                    int gaps = (periods.Max() - periods.Min() + 1) - periods.Distinct().Count();
                    ntps += gaps;
                }
            }

            if (ntps > 0)
                teacherNTPs[teacherSchedule.Key] = ntps;
        }

        // Calculate student NTPs
        var classSchedules = solution
            .Where(sl => lessonDict.ContainsKey(sl.LessonId))
            .Select(sl => new {
                ScheduledLesson = sl,
                ClassId = lessonDict[sl.LessonId].LessonClasses.FirstOrDefault()?.ClassId
            })
            .Where(x => x.ClassId.HasValue)
            .GroupBy(x => x.ClassId.Value);

        foreach (var classSchedule in classSchedules)
        {
            int ntps = 0;
            var byDay = classSchedule.GroupBy(x => x.ScheduledLesson.DayOfWeek);

            foreach (var daySchedule in byDay)
            {
                var periods = daySchedule.Select(x => x.ScheduledLesson.PeriodId).OrderBy(p => p).ToList();
                if (periods.Count > 1)
                {
                    int gaps = (periods.Max() - periods.Min() + 1) - periods.Distinct().Count();
                    ntps += gaps;
                }
            }

            if (ntps > 0)
                studentNTPs[classSchedule.Key] = ntps;
        }

        // Calculate overall score (0-100)
        int totalNTPs = teacherNTPs.Values.Sum() + studentNTPs.Values.Sum();
        int maxPossibleNTPs = solution.Count * 2; // Rough estimate
        double ntpScore = maxPossibleNTPs > 0 ? Math.Max(0, 100 - (totalNTPs * 100.0 / maxPossibleNTPs)) : 100;

        // Factor in hard constraint violations
        int hardViolations = CalculateHardConstraintViolations(solution, lessonDict);
        double violationPenalty = Math.Min(50, hardViolations * 10);

        int overallScore = (int)Math.Max(0, Math.Min(100, ntpScore - violationPenalty));

        return new QualityMetrics
        {
            OverallScore = overallScore,
            TotalTeacherNTPs = teacherNTPs.Values.Sum(),
            TotalStudentNTPs = studentNTPs.Values.Sum(),
            TeacherNTPs = teacherNTPs,
            StudentNTPs = studentNTPs
        };
    }

    // Helper methods for checking availability

    private bool IsTeacherAvailable(int teacherId, DayOfWeek day, int periodId, List<ScheduledLesson> scheduled, Dictionary<int, Lesson> lessonDict)
    {
        return !scheduled.Any(sl =>
            sl.DayOfWeek == day &&
            sl.PeriodId == periodId &&
            lessonDict.TryGetValue(sl.LessonId, out var lesson) &&
            lesson.LessonTeachers.Any(lt => lt.TeacherId == teacherId));
    }

    private bool IsClassAvailable(int classId, DayOfWeek day, int periodId, List<ScheduledLesson> scheduled, Dictionary<int, Lesson> lessonDict)
    {
        return !scheduled.Any(sl =>
            sl.DayOfWeek == day &&
            sl.PeriodId == periodId &&
            lessonDict.TryGetValue(sl.LessonId, out var lesson) &&
            lesson.LessonClasses.Any(lc => lc.ClassId == classId));
    }

    /// <summary>
    /// Checks if scheduling a lesson at the specified time would create a gap in the class schedule
    /// A gap is defined as an empty period between two scheduled periods on the same day
    /// </summary>
    private bool WouldCreateGapForClass(int classId, DayOfWeek day, int periodId, List<ScheduledLesson> scheduled, Dictionary<int, Lesson> lessonDict)
    {
        // Get all lessons for this class on this day, sorted by period
        var classLessonsThisDay = scheduled
            .Where(sl => sl.DayOfWeek == day &&
                         lessonDict.TryGetValue(sl.LessonId, out var lesson) &&
                         lesson.LessonClasses.Any(lc => lc.ClassId == classId))
            .OrderBy(sl => sl.PeriodId)
            .ToList();

        // If no lessons scheduled yet for this class on this day, no gap possible
        if (!classLessonsThisDay.Any())
        {
            return false;
        }

        // Get period numbers for existing lessons
        var existingPeriodIds = classLessonsThisDay.Select(sl => sl.PeriodId).ToList();

        // Check if adding this period would create a gap
        // A gap exists if there's a period between the min and max that has no lesson
        var allPeriodIds = new List<int>(existingPeriodIds) { periodId };
        var minPeriod = allPeriodIds.Min();
        var maxPeriod = allPeriodIds.Max();

        // If we only have min and max (2 periods total), check if there's a gap between them
        if (maxPeriod - minPeriod > allPeriodIds.Count - 1)
        {
            // There's a gap - count the periods between min and max
            // If count of periods < range, there's a gap
            return true;
        }

        return false;
    }

    private bool IsRoomAvailable(int roomId, DayOfWeek day, int periodId, List<ScheduledLesson> scheduled)
    {
        return !scheduled.Any(sl =>
            sl.RoomId == roomId &&
            sl.DayOfWeek == day &&
            sl.PeriodId == periodId);
    }

    /// <summary>
    /// Calculate availability penalty for the solution
    /// Returns the sum of all availability scores (negative = bad, positive = good)
    /// So a negative return value means we're scheduling at times we should avoid
    /// </summary>
    private int CalculateAvailabilityPenalty(
        List<ScheduledLesson> solution,
        Dictionary<int, Lesson> lessonDict,
        List<TeacherAvailability> teacherAvailabilities,
        List<ClassAvailability> classAvailabilities,
        List<RoomAvailability> roomAvailabilities,
        List<SubjectAvailability> subjectAvailabilities)
    {
        int totalScore = 0;

        foreach (var scheduledLesson in solution)
        {
            if (!lessonDict.TryGetValue(scheduledLesson.LessonId, out var lesson))
                continue;

            // Teacher availability constraint
            var primaryTeacherId = lesson.LessonTeachers.FirstOrDefault()?.TeacherId;
            if (primaryTeacherId.HasValue)
            {
                var teacherConstraint = teacherAvailabilities.FirstOrDefault(ta =>
                    ta.TeacherId == primaryTeacherId.Value &&
                    ta.DayOfWeek == scheduledLesson.DayOfWeek &&
                    ta.PeriodId == scheduledLesson.PeriodId);
                if (teacherConstraint != null)
                {
                    totalScore += teacherConstraint.Importance;
                }
            }

            // Second teacher (if exists)
            var secondaryTeacherId = lesson.LessonTeachers.Skip(1).FirstOrDefault()?.TeacherId;
            if (secondaryTeacherId.HasValue)
            {
                var secondTeacherConstraint = teacherAvailabilities.FirstOrDefault(ta =>
                    ta.TeacherId == secondaryTeacherId.Value &&
                    ta.DayOfWeek == scheduledLesson.DayOfWeek &&
                    ta.PeriodId == scheduledLesson.PeriodId);
                if (secondTeacherConstraint != null)
                {
                    totalScore += secondTeacherConstraint.Importance;
                }
            }

            // Class availability constraint
            var classId = lesson.LessonClasses.FirstOrDefault()?.ClassId;
            if (classId.HasValue)
            {
                var classConstraint = classAvailabilities.FirstOrDefault(ca =>
                    ca.ClassId == classId.Value &&
                    ca.DayOfWeek == scheduledLesson.DayOfWeek &&
                    ca.PeriodId == scheduledLesson.PeriodId);
                if (classConstraint != null)
                {
                    totalScore += classConstraint.Importance;
                }
            }

            // Subject availability constraint
            var subjectId = lesson.LessonSubjects.FirstOrDefault()?.SubjectId;
            if (subjectId.HasValue)
            {
                var subjectConstraint = subjectAvailabilities.FirstOrDefault(sa =>
                    sa.SubjectId == subjectId.Value &&
                    sa.DayOfWeek == scheduledLesson.DayOfWeek &&
                    sa.PeriodId == scheduledLesson.PeriodId);
                if (subjectConstraint != null)
                {
                    totalScore += subjectConstraint.Importance;
                }
            }

            // Room availability constraint (if room is assigned)
            if (scheduledLesson.RoomId.HasValue)
            {
                var roomConstraint = roomAvailabilities.FirstOrDefault(ra =>
                    ra.RoomId == scheduledLesson.RoomId.Value &&
                    ra.DayOfWeek == scheduledLesson.DayOfWeek &&
                    ra.PeriodId == scheduledLesson.PeriodId);
                if (roomConstraint != null)
                {
                    totalScore += roomConstraint.Importance;
                }
            }
        }

        return totalScore;
    }
}
