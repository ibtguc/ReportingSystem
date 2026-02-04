using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Models;

/// <summary>
/// IMPORTANT: Represents time CONSTRAINTS/PREFERENCES for classes (from UNTIS GPU016.TXT)
/// Negative values = AVOID scheduling, Positive values = TRY TO schedule
///
/// UNTIS Constraint/Preference Scale:
/// -3: AVOID scheduling (strongest - highest penalty)
/// -2: AVOID scheduling (medium strength)
/// -1: AVOID scheduling (weakest - lowest penalty)
///  0: Neutral (no preference either way - fully available)
/// +1: TRY TO schedule (weakest - lowest bonus)
/// +2: TRY TO schedule (medium strength)
/// +3: TRY TO schedule (strongest - highest bonus)
///
/// SCHEDULING ALGORITHM NOTE:
/// - Negative values = AVOID scheduling at this time
///   -3 is strongest avoidance, -2 is weaker, -1 is weakest
/// - Zero (0) = Neutral, no penalty or bonus
/// - Positive values = TRY TO schedule at this time
///   +1 is weakest preference, +2 is stronger, +3 is strongest
/// </summary>
public class ClassAvailability
{
    public int Id { get; set; }

    // Foreign key
    [Required]
    [Display(Name = "Class")]
    public int ClassId { get; set; }

    [Required]
    [Display(Name = "Day of Week")]
    public DayOfWeek DayOfWeek { get; set; }

    [Required]
    [Display(Name = "Period")]
    public int PeriodId { get; set; }

    /// <summary>
    /// UNTIS importance/constraint scale: -3 (prefer NOT to schedule) to +3 (MUST schedule)
    /// NEGATIVE values = unavailability/constraints (avoid scheduling)
    /// POSITIVE values = preferences/requirements (prefer scheduling)
    /// </summary>
    [Required]
    [Range(-3, 3)]
    [Display(Name = "Constraint Level")]
    public int Importance { get; set; } = 0;

    [StringLength(500)]
    [Display(Name = "Reason")]
    public string? Reason { get; set; }

    // Navigation properties
    public Class? Class { get; set; }
    public Period? Period { get; set; }
}
