namespace SchedulingSystem.Services;

/// <summary>
/// Configuration for soft constraint weights used in scheduling optimization
/// </summary>
public class SoftConstraintWeights
{
    /// <summary>
    /// Weight for minimizing teacher non-teaching periods (gaps in schedule)
    /// Default: 100 (highest priority)
    /// </summary>
    public int MinimizeTeacherNTPs { get; set; } = 100;

    /// <summary>
    /// Weight for minimizing student non-teaching periods (gaps in schedule)
    /// Default: 80
    /// </summary>
    public int MinimizeStudentNTPs { get; set; } = 80;

    /// <summary>
    /// Weight for distributing lessons evenly across the week
    /// Default: 60
    /// </summary>
    public int EvenDistribution { get; set; } = 60;

    /// <summary>
    /// Weight for preferring specific time slots (e.g., Math in morning)
    /// Default: 40
    /// </summary>
    public int PreferredTimeSlot { get; set; } = 40;

    /// <summary>
    /// Weight for minimizing room changes for teachers
    /// Default: 30
    /// </summary>
    public int MinimizeRoomChanges { get; set; } = 30;

    /// <summary>
    /// Weight for balancing daily workload
    /// Default: 50
    /// </summary>
    public int BalancedWorkload { get; set; } = 50;

    /// <summary>
    /// Weight for grouping lessons into blocks (consecutive periods)
    /// Default: 20
    /// </summary>
    public int BlockScheduling { get; set; } = 20;

    /// <summary>
    /// Weight for availability constraints (from UNTIS GPU016.TXT)
    /// Each importance point (-3 to +3) is multiplied by this weight
    /// Default: 10 (high priority - imported constraints should be respected)
    /// </summary>
    public int AvailabilityWeight { get; set; } = 10;

    /// <summary>
    /// Indicates whether soft constraints are enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets default weights
    /// </summary>
    public static SoftConstraintWeights Default => new SoftConstraintWeights();

    /// <summary>
    /// Gets aggressive weights (prioritize quality over speed)
    /// </summary>
    public static SoftConstraintWeights Aggressive => new SoftConstraintWeights
    {
        MinimizeTeacherNTPs = 150,
        MinimizeStudentNTPs = 120,
        EvenDistribution = 90,
        PreferredTimeSlot = 60,
        MinimizeRoomChanges = 50,
        BalancedWorkload = 80,
        BlockScheduling = 40,
        AvailabilityWeight = 15 // Higher weight for availability in aggressive mode
    };

    /// <summary>
    /// Gets relaxed weights (prioritize speed over quality)
    /// </summary>
    public static SoftConstraintWeights Relaxed => new SoftConstraintWeights
    {
        MinimizeTeacherNTPs = 50,
        MinimizeStudentNTPs = 40,
        EvenDistribution = 30,
        PreferredTimeSlot = 20,
        MinimizeRoomChanges = 15,
        BalancedWorkload = 25,
        BlockScheduling = 10,
        AvailabilityWeight = 5 // Lower weight in relaxed mode
    };
}

/// <summary>
/// Represents a candidate time slot with its quality score
/// </summary>
public class TimeSlotCandidate
{
    public DayOfWeek Day { get; set; }
    public int PeriodId { get; set; }
    public int? RoomId { get; set; }
    public int Score { get; set; }
    public List<string> ScoreReasons { get; set; } = new();

    public override string ToString()
    {
        return $"{Day} Period {PeriodId} - Score: {Score} ({string.Join(", ", ScoreReasons)})";
    }
}

/// <summary>
/// Subject time preferences
/// </summary>
public enum PreferredTimeOfDay
{
    Morning,    // Periods 1-3
    Midday,     // Periods 4-5
    Afternoon,  // Periods 6+
    Any
}

/// <summary>
/// Subject preferences for scheduling
/// </summary>
public static class SubjectPreferences
{
    /// <summary>
    /// Gets preferred time of day for a subject category
    /// </summary>
    public static PreferredTimeOfDay GetPreferredTime(string subjectCategory)
    {
        return subjectCategory?.ToLower() switch
        {
            "mathematics" => PreferredTimeOfDay.Morning,
            "science" => PreferredTimeOfDay.Morning,
            "language" => PreferredTimeOfDay.Morning,
            "arts" => PreferredTimeOfDay.Afternoon,
            "physical education" => PreferredTimeOfDay.Afternoon,
            "elective" => PreferredTimeOfDay.Any,
            _ => PreferredTimeOfDay.Any
        };
    }

    /// <summary>
    /// Checks if period matches preferred time of day
    /// </summary>
    public static bool IsPreferredPeriod(int periodNumber, PreferredTimeOfDay preferred)
    {
        return preferred switch
        {
            PreferredTimeOfDay.Morning => periodNumber <= 3,
            PreferredTimeOfDay.Midday => periodNumber >= 4 && periodNumber <= 5,
            PreferredTimeOfDay.Afternoon => periodNumber >= 6,
            PreferredTimeOfDay.Any => true,
            _ => true
        };
    }
}
