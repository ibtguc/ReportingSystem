using SchedulingSystem.Models;

namespace SchedulingSystem.Services.Constraints;

/// <summary>
/// Central repository for all constraint definitions in the scheduling system.
/// This class serves as the single source of truth for constraint rules, metadata, and special cases.
/// </summary>
public static class ConstraintDefinitions
{
    /// <summary>
    /// All constraint definitions organized by category
    /// </summary>
    public static class Constraints
    {
        // ========== HARD CONSTRAINTS - CONFLICT DETECTION ==========

        public static readonly ConstraintDefinition TeacherDoubleBooking = new()
        {
            Code = "HC-1",
            Name = "Teacher Double-Booking",
            Type = ConstraintType.Hard,
            Category = ConstraintCategory.Conflict,
            Description = "A teacher cannot teach two different lessons at the same time",
            ErrorMessageTemplate = "Teacher {0} is already teaching {1} ({2}) at this time",
            ExemptEntities = new List<string> { SpecialCases.InternTeacherName }
        };

        public static readonly ConstraintDefinition ClassDoubleBooking = new()
        {
            Code = "HC-2",
            Name = "Class Double-Booking",
            Type = ConstraintType.Hard,
            Category = ConstraintCategory.Conflict,
            Description = "A class cannot attend two different lessons at the same time",
            ErrorMessageTemplate = "Class {0} is already scheduled for {1} ({2}) at this time",
            ExemptEntities = new List<string> { SpecialCases.ReserveClassName, SpecialCases.TeamClassName }
        };

        public static readonly ConstraintDefinition RoomDoubleBooking = new()
        {
            Code = "HC-3",
            Name = "Room Double-Booking",
            Type = ConstraintType.Hard,
            Category = ConstraintCategory.Conflict,
            Description = "A room cannot host two different lessons at the same time",
            ErrorMessageTemplate = "Room {0} is already occupied by {1} ({2})",
            ExemptEntities = new List<string> { SpecialCases.TeamRoomName }
        };

        // ========== HARD CONSTRAINTS - AVAILABILITY ==========

        public static readonly ConstraintDefinition TeacherAbsoluteUnavailability = new()
        {
            Code = "HC-4",
            Name = "Teacher Absolute Unavailability",
            Type = ConstraintType.Hard,
            Category = ConstraintCategory.Availability,
            Description = "A teacher cannot be scheduled during times marked as absolutely unavailable (Importance = -3)",
            ErrorMessageTemplate = "Teacher {0} is unavailable at this time (Reason: {1})",
            ExemptEntities = new List<string> { SpecialCases.InternTeacherName }
        };

        public static readonly ConstraintDefinition ClassAbsoluteUnavailability = new()
        {
            Code = "HC-5",
            Name = "Class Absolute Unavailability",
            Type = ConstraintType.Hard,
            Category = ConstraintCategory.Availability,
            Description = "A class cannot be scheduled during times marked as absolutely unavailable (Importance = -3)",
            ErrorMessageTemplate = "Class {0} is unavailable at this time (e.g., assembly, standardized testing)",
            ExemptEntities = new List<string> { SpecialCases.ReserveClassName }
        };

        public static readonly ConstraintDefinition RoomAbsoluteUnavailability = new()
        {
            Code = "HC-6",
            Name = "Room Absolute Unavailability",
            Type = ConstraintType.Hard,
            Category = ConstraintCategory.Availability,
            Description = "A room cannot be used during times marked as absolutely unavailable (Importance = -3)",
            ErrorMessageTemplate = "Room {0} is unavailable at this time (e.g., maintenance, lockdown)",
            ExemptEntities = new List<string>()
        };

        public static readonly ConstraintDefinition SubjectAbsoluteUnavailability = new()
        {
            Code = "HC-7",
            Name = "Subject Absolute Unavailability",
            Type = ConstraintType.Hard,
            Category = ConstraintCategory.Availability,
            Description = "A subject cannot be taught during times marked as absolutely unavailable (Importance = -3)",
            ErrorMessageTemplate = "Subject {0} should not be scheduled at this time",
            ExemptEntities = new List<string>()
        };

        // ========== HARD CONSTRAINTS - TIME ==========

        public static readonly ConstraintDefinition TeacherMaxConsecutivePeriods = new()
        {
            Code = "HC-8",
            Name = "Teacher Max Consecutive Periods",
            Type = ConstraintType.Hard,
            Category = ConstraintCategory.Time,
            Description = "A teacher cannot teach more than N consecutive periods without a break",
            ErrorMessageTemplate = "Teacher {0} exceeds max consecutive periods ({1})",
            ExemptEntities = new List<string> { SpecialCases.InternTeacherName }
        };

        public static readonly ConstraintDefinition ClassMaxConsecutiveSameSubject = new()
        {
            Code = "HC-9",
            Name = "Class Max Consecutive Same Subject",
            Type = ConstraintType.Hard,
            Category = ConstraintCategory.Time,
            Description = "A class cannot have the same subject for more than N consecutive periods",
            ErrorMessageTemplate = "Class {0} exceeds max consecutive periods of {1} ({2})",
            ExemptEntities = new List<string> { SpecialCases.ReserveClassName }
        };

        public static readonly ConstraintDefinition LockedLesson = new()
        {
            Code = "HC-10",
            Name = "Locked Lesson",
            Type = ConstraintType.Hard,
            Category = ConstraintCategory.Time,
            Description = "A lesson marked as locked cannot be moved or deleted",
            ErrorMessageTemplate = "Lesson for {0} ({1}) is locked and cannot be modified",
            ExemptEntities = new List<string>()
        };

        public static readonly ConstraintDefinition TeacherMaxPeriodsPerDay = new()
        {
            Code = "HC-11",
            Name = "Teacher Max Periods Per Day",
            Type = ConstraintType.Hard,
            Category = ConstraintCategory.Workload,
            Description = "Teachers cannot exceed a maximum number of periods per day",
            ErrorMessageTemplate = "Teacher {0} exceeds maximum periods per day ({1})",
            ExemptEntities = new List<string> { SpecialCases.InternTeacherName }
        };

        public static readonly ConstraintDefinition ClassMaxPeriodsPerDay = new()
        {
            Code = "HC-12",
            Name = "Class Max Periods Per Day",
            Type = ConstraintType.Hard,
            Category = ConstraintCategory.Workload,
            Description = "Classes cannot exceed a maximum number of periods per day",
            ErrorMessageTemplate = "Class {0} exceeds maximum periods per day ({1})",
            ExemptEntities = new List<string> { SpecialCases.ReserveClassName, SpecialCases.TeamClassName }
        };

        // ========== SOFT CONSTRAINTS - PREFERENCES ==========

        public static readonly ConstraintDefinition TeacherPreferenceUnavailability = new()
        {
            Code = "SC-1",
            Name = "Teacher Preference Unavailability",
            Type = ConstraintType.Soft,
            Category = ConstraintCategory.Availability,
            Description = "A teacher has expressed preference to avoid certain times (Importance = -2 or -1)",
            ErrorMessageTemplate = "Teacher {0} {1} to teach at this time",
            ExemptEntities = new List<string> { SpecialCases.InternTeacherName }
        };

        public static readonly ConstraintDefinition ClassPreferenceUnavailability = new()
        {
            Code = "SC-2",
            Name = "Class Preference Unavailability",
            Type = ConstraintType.Soft,
            Category = ConstraintCategory.Availability,
            Description = "A class has a preference to avoid certain times (Importance = -2 or -1)",
            ErrorMessageTemplate = "Class {0} {1} to be scheduled at this time",
            ExemptEntities = new List<string> { SpecialCases.ReserveClassName }
        };

        public static readonly ConstraintDefinition SubjectTimePreferences = new()
        {
            Code = "SC-3",
            Name = "Subject Time Preferences",
            Type = ConstraintType.Soft,
            Category = ConstraintCategory.Availability,
            Description = "A subject has preferred or discouraged time slots (Importance = -2 or -1)",
            ErrorMessageTemplate = "Subject {0} {1} to be scheduled at this time",
            ExemptEntities = new List<string>()
        };

        public static readonly ConstraintDefinition RoomPreferences = new()
        {
            Code = "SC-4",
            Name = "Room Preferences",
            Type = ConstraintType.Soft,
            Category = ConstraintCategory.Availability,
            Description = "A room has preference levels for certain times (Importance = -2 or -1)",
            ErrorMessageTemplate = "Room {0} {1} to be used at this time",
            ExemptEntities = new List<string>()
        };

        // ========== SOFT CONSTRAINTS - WORKLOAD ==========

        public static readonly ConstraintDefinition TeacherLunchBreak = new()
        {
            Code = "SC-5",
            Name = "Teacher Lunch Break",
            Type = ConstraintType.Soft,
            Category = ConstraintCategory.Workload,
            Description = "Teachers should have adequate lunch break time",
            ErrorMessageTemplate = "Teacher {0} may not have adequate lunch break",
            ExemptEntities = new List<string> { SpecialCases.InternTeacherName }
        };

        public static readonly ConstraintDefinition ClassLunchBreak = new()
        {
            Code = "SC-6",
            Name = "Class Lunch Break",
            Type = ConstraintType.Soft,
            Category = ConstraintCategory.Workload,
            Description = "Classes should have adequate lunch break time",
            ErrorMessageTemplate = "Class {0} may not have adequate lunch break",
            ExemptEntities = new List<string> { SpecialCases.ReserveClassName }
        };

        // ========== SOFT CONSTRAINTS - PEDAGOGICAL ==========

        public static readonly ConstraintDefinition NoGapsInClassSchedule = new()
        {
            Code = "SC-7",
            Name = "No Gaps in Class Schedule",
            Type = ConstraintType.Soft,
            Category = ConstraintCategory.Pedagogical,
            Description = "Classes should have compact schedules without gaps (free periods between lessons)",
            ErrorMessageTemplate = "Class {0} creates a gap in the class schedule",
            ExemptEntities = new List<string>(),
            Priority = ConstraintPriority.High
        };

        // ========== SOFT CONSTRAINTS - RESOURCE ==========

        public static readonly ConstraintDefinition RoomTypePreferenceMismatch = new()
        {
            Code = "SC-8",
            Name = "Room Type Preference Mismatch",
            Type = ConstraintType.Soft,
            Category = ConstraintCategory.Resource,
            Description = "Lessons should be scheduled in rooms of the appropriate type when possible",
            ErrorMessageTemplate = "Room is type '{0}' but lesson requires '{1}'",
            ExemptEntities = new List<string>()
        };

        public static readonly ConstraintDefinition SubjectPreferredRoom = new()
        {
            Code = "SC-9",
            Name = "Subject Preferred Room",
            Type = ConstraintType.Soft,
            Category = ConstraintCategory.Resource,
            Description = "Subjects should use their designated preferred room when available",
            ErrorMessageTemplate = "Subject {0} prefers room {1}",
            ExemptEntities = new List<string>()
        };

        // ========== SOFT CONSTRAINTS - WORKLOAD (continued) ==========

        public static readonly ConstraintDefinition TeacherMinPeriodsPerDay = new()
        {
            Code = "SC-10",
            Name = "Teacher Min Periods Per Day",
            Type = ConstraintType.Soft,
            Category = ConstraintCategory.Workload,
            Description = "Teachers should teach at least a minimum number of periods per day",
            ErrorMessageTemplate = "Teacher {0} has fewer than minimum periods per day ({1})",
            ExemptEntities = new List<string> { SpecialCases.InternTeacherName }
        };

        public static readonly ConstraintDefinition ClassMinPeriodsPerDay = new()
        {
            Code = "SC-11",
            Name = "Class Min Periods Per Day",
            Type = ConstraintType.Soft,
            Category = ConstraintCategory.Workload,
            Description = "Classes should have at least a minimum number of periods per day",
            ErrorMessageTemplate = "Class {0} has fewer than minimum periods per day ({1})",
            ExemptEntities = new List<string> { SpecialCases.ReserveClassName, SpecialCases.TeamClassName }
        };
    }

    /// <summary>
    /// Special case definitions and helper methods
    /// </summary>
    public static class SpecialCases
    {
        /// <summary>
        /// Placeholder name for intern/temporary teachers
        /// </summary>
        public const string InternTeacherName = "xy";

        /// <summary>
        /// Reserve class name used for substitution planning
        /// </summary>
        public const string ReserveClassName = "v-res";

        /// <summary>
        /// Team class name - exempt from double-booking
        /// </summary>
        public const string TeamClassName = "Team";

        /// <summary>
        /// Team room name - exempt from double-booking
        /// </summary>
        public const string TeamRoomName = "Teamraum";

        /// <summary>
        /// Checks if a teacher is an intern placeholder
        /// </summary>
        public static bool IsInternTeacher(Teacher? teacher)
        {
            if (teacher == null) return false;

            var firstName = teacher.FirstName?.Trim() ?? "";
            var fullName = teacher.FullName?.Trim() ?? "";

            return firstName.Equals(InternTeacherName, StringComparison.OrdinalIgnoreCase) ||
                   fullName.Equals(InternTeacherName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if a class is a special class exempt from double-booking (v-res, Team)
        /// </summary>
        public static bool IsSpecialClass(Class? @class)
        {
            if (@class == null) return false;

            var className = @class.Name?.Trim() ?? "";
            return className.Equals(ReserveClassName, StringComparison.OrdinalIgnoreCase) ||
                   className.Equals(TeamClassName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if a room is a special room exempt from double-booking (Teamraum)
        /// </summary>
        public static bool IsSpecialRoom(Room? room)
        {
            if (room == null) return false;

            var roomNumber = room.RoomNumber?.Trim() ?? "";
            return roomNumber.Equals(TeamRoomName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Legacy method - checks if class is reserve class only
        /// </summary>
        public static bool IsReserveClass(Class? @class)
        {
            if (@class == null) return false;

            var className = @class.Name?.Trim() ?? "";
            return className.Equals(ReserveClassName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if a constraint should be exempted for a given entity name
        /// </summary>
        public static bool IsExempt(ConstraintDefinition constraint, string entityName)
        {
            if (string.IsNullOrWhiteSpace(entityName)) return false;

            return constraint.ExemptEntities.Any(exempt =>
                entityName.Equals(exempt, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// UNTIS importance scale definitions and helper methods
    /// </summary>
    public static class ImportanceScale
    {
        public const int MustNotSchedule = -3;
        public const int StronglyPreferNot = -2;
        public const int MildlyPreferNot = -1;
        public const int Neutral = 0;
        public const int MildlyPrefer = 1;
        public const int StronglyPrefer = 2;
        public const int MustSchedule = 3;

        /// <summary>
        /// Checks if an importance level represents a hard constraint
        /// </summary>
        public static bool IsHardConstraint(int importance)
        {
            return importance == MustNotSchedule || importance == MustSchedule;
        }

        /// <summary>
        /// Checks if an importance level represents a soft constraint (preference)
        /// </summary>
        public static bool IsSoftConstraint(int importance)
        {
            return importance != Neutral && !IsHardConstraint(importance);
        }

        /// <summary>
        /// Gets a human-readable description of the preference strength
        /// </summary>
        public static string GetPreferenceStrengthDescription(int importance)
        {
            return importance switch
            {
                MustNotSchedule => "must NOT",
                StronglyPreferNot => "strongly prefers NOT",
                MildlyPreferNot => "mildly prefers NOT",
                Neutral => "has no preference",
                MildlyPrefer => "mildly prefers",
                StronglyPrefer => "strongly prefers",
                MustSchedule => "must",
                _ => "has no preference"
            };
        }
    }

    /// <summary>
    /// Gets all hard constraint definitions
    /// </summary>
    public static List<ConstraintDefinition> GetHardConstraints()
    {
        return new List<ConstraintDefinition>
        {
            Constraints.TeacherDoubleBooking,
            Constraints.ClassDoubleBooking,
            Constraints.RoomDoubleBooking,
            Constraints.TeacherAbsoluteUnavailability,
            Constraints.ClassAbsoluteUnavailability,
            Constraints.RoomAbsoluteUnavailability,
            Constraints.SubjectAbsoluteUnavailability,
            Constraints.TeacherMaxConsecutivePeriods,
            Constraints.ClassMaxConsecutiveSameSubject,
            Constraints.LockedLesson,
            Constraints.TeacherMaxPeriodsPerDay,
            Constraints.ClassMaxPeriodsPerDay
        };
    }

    /// <summary>
    /// Gets all soft constraint definitions
    /// </summary>
    public static List<ConstraintDefinition> GetSoftConstraints()
    {
        return new List<ConstraintDefinition>
        {
            Constraints.TeacherPreferenceUnavailability,
            Constraints.ClassPreferenceUnavailability,
            Constraints.SubjectTimePreferences,
            Constraints.RoomPreferences,
            Constraints.TeacherLunchBreak,
            Constraints.ClassLunchBreak,
            Constraints.NoGapsInClassSchedule,
            Constraints.RoomTypePreferenceMismatch,
            Constraints.SubjectPreferredRoom,
            Constraints.TeacherMinPeriodsPerDay,
            Constraints.ClassMinPeriodsPerDay
        };
    }

    /// <summary>
    /// Gets all constraint definitions
    /// </summary>
    public static List<ConstraintDefinition> GetAllConstraints()
    {
        return GetHardConstraints().Concat(GetSoftConstraints()).ToList();
    }

    /// <summary>
    /// Gets a constraint definition by its code
    /// </summary>
    public static ConstraintDefinition? GetConstraintByCode(string code)
    {
        return GetAllConstraints().FirstOrDefault(c =>
            c.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all constraints in a specific category
    /// </summary>
    public static List<ConstraintDefinition> GetConstraintsByCategory(ConstraintCategory category)
    {
        return GetAllConstraints().Where(c => c.Category == category).ToList();
    }
}

/// <summary>
/// Defines a constraint in the scheduling system
/// </summary>
public class ConstraintDefinition
{
    /// <summary>
    /// Unique constraint code (e.g., HC-1, SC-1)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable constraint name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Constraint type (Hard or Soft)
    /// </summary>
    public ConstraintType Type { get; set; }

    /// <summary>
    /// Constraint category
    /// </summary>
    public ConstraintCategory Category { get; set; }

    /// <summary>
    /// Detailed description of the constraint
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Error message template with placeholders for dynamic content
    /// </summary>
    public string ErrorMessageTemplate { get; set; } = string.Empty;

    /// <summary>
    /// List of entity names that are exempt from this constraint (e.g., "xy", "v-res")
    /// </summary>
    public List<string> ExemptEntities { get; set; } = new();

    /// <summary>
    /// Priority level for soft constraints (only applicable to soft constraints)
    /// </summary>
    public ConstraintPriority Priority { get; set; } = ConstraintPriority.Normal;
}

/// <summary>
/// Type of constraint
/// </summary>
public enum ConstraintType
{
    /// <summary>
    /// Must be satisfied; violation prevents scheduling
    /// </summary>
    Hard,

    /// <summary>
    /// Should be satisfied; violation generates warning but allows scheduling
    /// </summary>
    Soft
}

/// <summary>
/// Category of constraint
/// </summary>
public enum ConstraintCategory
{
    /// <summary>
    /// Prevents double-booking of resources (teachers, classes, rooms)
    /// </summary>
    Conflict,

    /// <summary>
    /// Respects time-based availability and unavailability
    /// </summary>
    Availability,

    /// <summary>
    /// Limits duration, frequency, and distribution of lessons
    /// </summary>
    Time,

    /// <summary>
    /// Ensures proper allocation of rooms and equipment
    /// </summary>
    Resource,

    /// <summary>
    /// Balances teaching load across days and weeks
    /// </summary>
    Workload,

    /// <summary>
    /// Supports educational best practices (no gaps, consecutive lessons, etc.)
    /// </summary>
    Pedagogical
}

/// <summary>
/// Priority level for soft constraints
/// </summary>
public enum ConstraintPriority
{
    /// <summary>
    /// Low priority - nice to have
    /// </summary>
    Low,

    /// <summary>
    /// Normal priority - standard preference
    /// </summary>
    Normal,

    /// <summary>
    /// High priority - strongly recommended
    /// </summary>
    High,

    /// <summary>
    /// Critical priority - almost as important as hard constraints
    /// </summary>
    Critical
}
