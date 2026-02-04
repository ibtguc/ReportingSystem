using SchedulingSystem.Models;

namespace SchedulingSystem.Services.Constraints;

/// <summary>
/// Interface for centralized constraint validation service
/// </summary>
public interface IConstraintValidator
{
    /// <summary>
    /// Validates all hard constraints for a scheduled lesson
    /// </summary>
    /// <param name="lesson">The scheduled lesson to validate</param>
    /// <param name="existingSchedule">List of already scheduled lessons</param>
    /// <param name="context">Optional validation context for filtering and caching</param>
    /// <returns>Validation result with any hard constraint violations</returns>
    Task<ValidationResult> ValidateHardConstraintsAsync(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext? context = null);

    /// <summary>
    /// Validates all soft constraints for a scheduled lesson
    /// </summary>
    /// <param name="lesson">The scheduled lesson to validate</param>
    /// <param name="existingSchedule">List of already scheduled lessons</param>
    /// <param name="context">Optional validation context for filtering and caching</param>
    /// <returns>Validation result with any soft constraint violations</returns>
    Task<ValidationResult> ValidateSoftConstraintsAsync(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext? context = null);

    /// <summary>
    /// Validates both hard and soft constraints for a scheduled lesson
    /// </summary>
    /// <param name="lesson">The scheduled lesson to validate</param>
    /// <param name="existingSchedule">List of already scheduled lessons</param>
    /// <param name="context">Optional validation context for filtering and caching</param>
    /// <returns>Validation result with all constraint violations</returns>
    Task<ValidationResult> ValidateAllConstraintsAsync(
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext? context = null);

    /// <summary>
    /// Validates a specific constraint by code
    /// </summary>
    /// <param name="constraintCode">The constraint code to validate (e.g., "HC-1")</param>
    /// <param name="lesson">The scheduled lesson to validate</param>
    /// <param name="existingSchedule">List of already scheduled lessons</param>
    /// <param name="context">Optional validation context for caching</param>
    /// <returns>Constraint-specific validation result</returns>
    Task<ConstraintResult> ValidateConstraintAsync(
        string constraintCode,
        ScheduledLesson lesson,
        List<ScheduledLesson> existingSchedule,
        ValidationContext? context = null);

    /// <summary>
    /// Validates entire timetable for all constraints
    /// </summary>
    /// <param name="timetableId">ID of the timetable to validate</param>
    /// <param name="context">Optional validation context for filtering</param>
    /// <returns>Timetable validation result with all conflicts</returns>
    Task<TimetableValidationResult> ValidateTimetableAsync(
        int timetableId,
        ValidationContext? context = null);

    /// <summary>
    /// Quick check if a lesson can be scheduled at a specific time without violating hard constraints
    /// </summary>
    /// <param name="lessonId">ID of the lesson to schedule</param>
    /// <param name="dayOfWeek">Day of week</param>
    /// <param name="periodId">Period ID</param>
    /// <param name="roomId">Optional room ID</param>
    /// <param name="existingSchedule">List of already scheduled lessons</param>
    /// <returns>True if lesson can be scheduled without hard constraint violations</returns>
    Task<bool> CanScheduleAtAsync(
        int lessonId,
        DayOfWeek dayOfWeek,
        int periodId,
        int? roomId,
        List<ScheduledLesson> existingSchedule);
}
