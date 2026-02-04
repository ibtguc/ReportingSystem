using SchedulingSystem.Data;
using SchedulingSystem.Models;
using SchedulingSystem.Services.Constraints;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchedulingSystem.Services.LessonMovement
{
    /// <summary>
    /// Result of a lesson movement operation
    /// </summary>
    public class MovementResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public ScheduledLesson? UpdatedLesson { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Result of executing a swap chain
    /// </summary>
    public class SwapChainExecutionResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<ScheduledLesson> UpdatedLessons { get; set; } = new();
        public int TotalMoves { get; set; }
    }

    /// <summary>
    /// Coordinator service for all lesson movement operations
    /// Provides high-level API for single moves and multi-step swaps
    /// </summary>
    public class LessonMovementService
    {
        private readonly ApplicationDbContext _context;
        private readonly AvailableSlotFinder _slotFinder;
        private readonly SwapChainSolver _swapSolver;

        public LessonMovementService(
            ApplicationDbContext context,
            AvailableSlotFinder slotFinder,
            SwapChainSolver swapSolver)
        {
            _context = context;
            _slotFinder = slotFinder;
            _swapSolver = swapSolver;
        }

        /// <summary>
        /// Move a scheduled lesson to a new timeslot (single-step move)
        /// </summary>
        public async Task<MovementResult> MoveLessonAsync(
            int scheduledLessonId,
            DayOfWeek newDay,
            int newPeriodId,
            int? newRoomId,
            bool force = false)
        {
            var scheduledLesson = await _context.ScheduledLessons
                .Include(sl => sl.Lesson)
                .FirstOrDefaultAsync(sl => sl.Id == scheduledLessonId);

            if (scheduledLesson == null)
            {
                return new MovementResult
                {
                    Success = false,
                    ErrorMessage = "Scheduled lesson not found"
                };
            }

            // Check if lesson is locked
            if (scheduledLesson.IsLocked && !force)
            {
                return new MovementResult
                {
                    Success = false,
                    ErrorMessage = "Cannot move locked lesson. Unlock it first or use force option."
                };
            }

            // Validate the move using slot finder
            if (!scheduledLesson.TimetableId.HasValue)
            {
                return new MovementResult
                {
                    Success = false,
                    ErrorMessage = "Scheduled lesson has no timetable assignment"
                };
            }

            var availableSlots = await _slotFinder.FindAvailableSlotsAsync(
                scheduledLesson.TimetableId.Value,
                scheduledLesson.LessonId,
                scheduledLesson.Id,
                includeCurrentSlot: false);

            var targetSlot = availableSlots.FirstOrDefault(s =>
                s.DayOfWeek == newDay &&
                s.PeriodId == newPeriodId &&
                s.RoomId == newRoomId);

            if (targetSlot == null)
            {
                return new MovementResult
                {
                    Success = false,
                    ErrorMessage = "Target slot not found in available slots"
                };
            }

            if (targetSlot.HasHardConstraintViolations && !force)
            {
                var violations = string.Join(", ", targetSlot.HardViolations);
                return new MovementResult
                {
                    Success = false,
                    ErrorMessage = $"Cannot move lesson due to hard constraint violations: {violations}"
                };
            }

            // Perform the move
            scheduledLesson.DayOfWeek = newDay;
            scheduledLesson.PeriodId = newPeriodId;
            scheduledLesson.RoomId = newRoomId;

            await _context.SaveChangesAsync();

            var warnings = targetSlot.SoftViolations;

            return new MovementResult
            {
                Success = true,
                UpdatedLesson = scheduledLesson,
                Warnings = warnings
            };
        }

        /// <summary>
        /// Get all available slots for a scheduled lesson
        /// </summary>
        public async Task<List<AvailableSlot>> GetAvailableSlotsAsync(
            int scheduledLessonId,
            List<(DayOfWeek Day, int PeriodId)>? excludeSlots = null)
        {
            var scheduledLesson = await _context.ScheduledLessons
                .FirstOrDefaultAsync(sl => sl.Id == scheduledLessonId);

            if (scheduledLesson == null)
                throw new ArgumentException($"Scheduled lesson {scheduledLessonId} not found");

            if (!scheduledLesson.TimetableId.HasValue)
                throw new ArgumentException($"Scheduled lesson {scheduledLessonId} has no timetable assignment");

            return await _slotFinder.FindAvailableSlotsAsync(
                scheduledLesson.TimetableId.Value,
                scheduledLesson.LessonId,
                scheduledLesson.Id,
                excludeSlots,
                includeCurrentSlot: false);
        }

        /// <summary>
        /// Get available slots grouped by quality
        /// </summary>
        public async Task<AvailableSlotsByQuality> GetAvailableSlotsGroupedAsync(
            int scheduledLessonId,
            List<(DayOfWeek Day, int PeriodId)>? excludeSlots = null)
        {
            var scheduledLesson = await _context.ScheduledLessons
                .FirstOrDefaultAsync(sl => sl.Id == scheduledLessonId);

            if (scheduledLesson == null)
                throw new ArgumentException($"Scheduled lesson {scheduledLessonId} not found");

            return await _slotFinder.FindAvailableSlotsGroupedAsync(
                scheduledLesson.TimetableId!.Value,
                scheduledLesson.LessonId,
                scheduledLesson.Id,
                excludeSlots);
        }

        /// <summary>
        /// Find swap chains to move a lesson to a specific target slot
        /// </summary>
        public async Task<List<SwapChain>> FindSwapChainsAsync(
            int scheduledLessonId,
            DayOfWeek targetDay,
            int targetPeriodId,
            int? targetRoomId,
            SwapChainConfig? config = null)
        {
            var scheduledLesson = await _context.ScheduledLessons
                .FirstOrDefaultAsync(sl => sl.Id == scheduledLessonId);

            if (scheduledLesson == null)
                throw new ArgumentException($"Scheduled lesson {scheduledLessonId} not found");

            return await _swapSolver.FindSwapChainsAsync(
                scheduledLesson.TimetableId!.Value,
                scheduledLessonId,
                targetDay,
                targetPeriodId,
                targetRoomId,
                config);
        }

        /// <summary>
        /// Execute a swap chain (all moves as a transaction)
        /// </summary>
        public async Task<SwapChainExecutionResult> ExecuteSwapChainAsync(SwapChain swapChain, bool force = false)
        {
            if (!swapChain.IsValid)
            {
                return new SwapChainExecutionResult
                {
                    Success = false,
                    ErrorMessage = swapChain.ErrorMessage ?? "Invalid swap chain"
                };
            }

            // Begin transaction
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var updatedLessons = new List<ScheduledLesson>();

                // Execute each step in order
                foreach (var step in swapChain.Steps.OrderBy(s => s.StepNumber))
                {
                    var scheduledLesson = await _context.ScheduledLessons
                        .FirstOrDefaultAsync(sl => sl.Id == step.ScheduledLessonId);

                    if (scheduledLesson == null)
                    {
                        await transaction.RollbackAsync();
                        return new SwapChainExecutionResult
                        {
                            Success = false,
                            ErrorMessage = $"Scheduled lesson {step.ScheduledLessonId} not found"
                        };
                    }

                    // Check if locked
                    if (scheduledLesson.IsLocked && !force)
                    {
                        await transaction.RollbackAsync();
                        return new SwapChainExecutionResult
                        {
                            Success = false,
                            ErrorMessage = $"Cannot move locked lesson: {step.LessonDescription}"
                        };
                    }

                    // Perform the move
                    scheduledLesson.DayOfWeek = step.ToDay;
                    scheduledLesson.PeriodId = step.ToPeriodId;
                    scheduledLesson.RoomId = step.ToRoomId;

                    updatedLessons.Add(scheduledLesson);
                }

                // Save all changes
                await _context.SaveChangesAsync();

                // Commit transaction
                await transaction.CommitAsync();

                return new SwapChainExecutionResult
                {
                    Success = true,
                    UpdatedLessons = updatedLessons,
                    TotalMoves = swapChain.TotalMoves
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new SwapChainExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"Error executing swap chain: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Validate if a move is possible without executing it
        /// </summary>
        public async Task<MovementValidation> ValidateMoveAsync(
            int scheduledLessonId,
            DayOfWeek newDay,
            int newPeriodId,
            int? newRoomId)
        {
            var scheduledLesson = await _context.ScheduledLessons
                .Include(sl => sl.Lesson)
                .FirstOrDefaultAsync(sl => sl.Id == scheduledLessonId);

            if (scheduledLesson == null)
            {
                return new MovementValidation
                {
                    IsValid = false,
                    ErrorMessage = "Scheduled lesson not found"
                };
            }

            if (scheduledLesson.IsLocked)
            {
                return new MovementValidation
                {
                    IsValid = false,
                    ErrorMessage = "Lesson is locked",
                    IsLocked = true
                };
            }

            // Find the target slot
            var availableSlots = await _slotFinder.FindAvailableSlotsAsync(
                scheduledLesson.TimetableId!.Value,
                scheduledLesson.LessonId,
                scheduledLesson.Id,
                includeCurrentSlot: false);

            var targetSlot = availableSlots.FirstOrDefault(s =>
                s.DayOfWeek == newDay &&
                s.PeriodId == newPeriodId &&
                s.RoomId == newRoomId);

            if (targetSlot == null)
            {
                return new MovementValidation
                {
                    IsValid = false,
                    ErrorMessage = "Target slot not available"
                };
            }

            return new MovementValidation
            {
                IsValid = !targetSlot.HasHardConstraintViolations,
                HasHardConstraintViolations = targetSlot.HasHardConstraintViolations,
                HardViolations = targetSlot.HardViolations,
                SoftViolations = targetSlot.SoftViolations,
                QualityScore = targetSlot.QualityScore,
                ErrorMessage = targetSlot.HasHardConstraintViolations
                    ? "Target slot has hard constraint violations"
                    : null
            };
        }

        /// <summary>
        /// Check if a direct move is possible or if swaps are needed
        /// </summary>
        public async Task<MovementStrategy> DetermineMovementStrategyAsync(
            int scheduledLessonId,
            DayOfWeek targetDay,
            int targetPeriodId,
            int? targetRoomId)
        {
            var validation = await ValidateMoveAsync(scheduledLessonId, targetDay, targetPeriodId, targetRoomId);

            if (validation.IsValid)
            {
                return new MovementStrategy
                {
                    StrategyType = MoveStrategyType.DirectMove,
                    CanMoveDirectly = true,
                    RequiresSwaps = false,
                    Validation = validation
                };
            }

            // Check if it's blocked by another lesson or just has constraint violations
            var scheduledLesson = await _context.ScheduledLessons
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l.LessonTeachers)
                .Include(sl => sl.Lesson)
                    .ThenInclude(l => l.LessonClasses)
                .FirstOrDefaultAsync(sl => sl.Id == scheduledLessonId);

            if (scheduledLesson == null)
            {
                return new MovementStrategy
                {
                    StrategyType = MoveStrategyType.Impossible,
                    CanMoveDirectly = false,
                    RequiresSwaps = false,
                    ErrorMessage = "Scheduled lesson not found"
                };
            }

            // Check if target slot has a blocking lesson
            var hasBlockingLesson = await HasBlockingLessonAtSlotAsync(
                scheduledLesson.TimetableId!.Value,
                scheduledLesson.Lesson,
                targetDay,
                targetPeriodId,
                targetRoomId);

            if (hasBlockingLesson)
            {
                return new MovementStrategy
                {
                    StrategyType = MoveStrategyType.RequiresSwaps,
                    CanMoveDirectly = false,
                    RequiresSwaps = true,
                    Validation = validation
                };
            }

            return new MovementStrategy
            {
                StrategyType = MoveStrategyType.Impossible,
                CanMoveDirectly = false,
                RequiresSwaps = false,
                ErrorMessage = "Move not possible due to hard constraint violations",
                Validation = validation
            };
        }

        /// <summary>
        /// Check if there's a blocking lesson at the target slot
        /// </summary>
        private async Task<bool> HasBlockingLessonAtSlotAsync(
            int timetableId,
            Lesson lesson,
            DayOfWeek day,
            int periodId,
            int? roomId)
        {
            var teacherIds = lesson.LessonTeachers.Select(lt => lt.TeacherId).ToList();
            var classIds = lesson.LessonClasses.Select(lc => lc.ClassId).ToList();

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

            foreach (var scheduled in scheduledLessons)
            {
                // Check teacher conflicts
                var scheduledTeacherIds = scheduled.Lesson.LessonTeachers.Select(lt => lt.TeacherId);
                if (teacherIds.Any(tid => scheduledTeacherIds.Contains(tid)))
                    return true;

                // Check class conflicts
                var scheduledClassIds = scheduled.Lesson.LessonClasses.Select(lc => lc.ClassId);
                if (classIds.Any(cid => scheduledClassIds.Contains(cid)))
                    return true;

                // Check room conflicts
                if (roomId.HasValue)
                {
                    if (scheduled.RoomId == roomId.Value)
                        return true;
                    if (scheduled.ScheduledLessonRooms.Any(slr => slr.RoomId == roomId.Value))
                        return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Result of movement validation
    /// </summary>
    public class MovementValidation
    {
        public bool IsValid { get; set; }
        public bool IsLocked { get; set; }
        public bool HasHardConstraintViolations { get; set; }
        public List<string> HardViolations { get; set; } = new();
        public List<string> SoftViolations { get; set; } = new();
        public double QualityScore { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Strategy for moving a lesson
    /// </summary>
    public class MovementStrategy
    {
        public MoveStrategyType StrategyType { get; set; }
        public bool CanMoveDirectly { get; set; }
        public bool RequiresSwaps { get; set; }
        public string? ErrorMessage { get; set; }
        public MovementValidation? Validation { get; set; }
    }

    /// <summary>
    /// Types of movement strategies
    /// </summary>
    public enum MoveStrategyType
    {
        DirectMove,        // Can move directly to target slot
        RequiresSwaps,     // Target is blocked, needs swap chain
        Impossible         // Cannot move due to constraints
    }
}
