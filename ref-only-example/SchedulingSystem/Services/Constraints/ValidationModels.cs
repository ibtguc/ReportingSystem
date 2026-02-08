namespace SchedulingSystem.Services.Constraints;

/// <summary>
/// Result of constraint validation for a scheduled lesson
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// True if no hard constraint violations found
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of hard constraint violations
    /// </summary>
    public List<ConstraintViolation> HardViolations { get; set; } = new();

    /// <summary>
    /// List of soft constraint violations (warnings)
    /// </summary>
    public List<ConstraintViolation> SoftViolations { get; set; } = new();

    /// <summary>
    /// True if any hard constraint violations exist
    /// </summary>
    public bool HasErrors => HardViolations.Any();

    /// <summary>
    /// True if any soft constraint violations exist
    /// </summary>
    public bool HasWarnings => SoftViolations.Any();

    /// <summary>
    /// Gets all error messages from hard violations
    /// </summary>
    public List<string> GetErrorMessages() => HardViolations.Select(v => v.Message).ToList();

    /// <summary>
    /// Gets all warning messages from soft violations
    /// </summary>
    public List<string> GetWarningMessages() => SoftViolations.Select(v => v.Message).ToList();

    /// <summary>
    /// Merges another validation result into this one
    /// </summary>
    public void Merge(ValidationResult other)
    {
        HardViolations.AddRange(other.HardViolations);
        SoftViolations.AddRange(other.SoftViolations);
        IsValid = !HasErrors;
    }
}

/// <summary>
/// Represents a single constraint violation
/// </summary>
public class ConstraintViolation
{
    /// <summary>
    /// Constraint code (e.g., HC-1, SC-1)
    /// </summary>
    public string ConstraintCode { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable constraint name
    /// </summary>
    public string ConstraintName { get; set; } = string.Empty;

    /// <summary>
    /// Type of constraint violated
    /// </summary>
    public ConstraintType Type { get; set; }

    /// <summary>
    /// Formatted error/warning message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional details about the violation (entity IDs, etc.)
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();

    /// <summary>
    /// When the violation was detected
    /// </summary>
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result of validating a single constraint
/// </summary>
public class ConstraintResult
{
    /// <summary>
    /// Constraint code that was checked
    /// </summary>
    public string ConstraintCode { get; set; } = string.Empty;

    /// <summary>
    /// True if constraint is satisfied
    /// </summary>
    public bool Satisfied { get; set; }

    /// <summary>
    /// Error/warning message if constraint not satisfied
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional details about the constraint check
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Result of validating an entire timetable
/// </summary>
public class TimetableValidationResult
{
    /// <summary>
    /// True if no error-level conflicts found
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of conflicts found
    /// </summary>
    public List<TimetableConflict> Conflicts { get; set; } = new();

    /// <summary>
    /// Total number of lessons validated
    /// </summary>
    public int TotalLessons { get; set; }

    /// <summary>
    /// Number of lessons with errors
    /// </summary>
    public int LessonsWithErrors => Conflicts.Count(c => c.Type == ConflictSeverity.Error);

    /// <summary>
    /// Number of lessons with warnings
    /// </summary>
    public int LessonsWithWarnings => Conflicts.Count(c => c.Type == ConflictSeverity.Warning);

    /// <summary>
    /// Gets all error messages
    /// </summary>
    public List<string> GetAllErrorMessages()
    {
        return Conflicts
            .Where(c => c.Type == ConflictSeverity.Error)
            .SelectMany(c => c.Messages)
            .ToList();
    }

    /// <summary>
    /// Gets all warning messages
    /// </summary>
    public List<string> GetAllWarningMessages()
    {
        return Conflicts
            .Where(c => c.Type == ConflictSeverity.Warning)
            .SelectMany(c => c.Messages)
            .ToList();
    }
}

/// <summary>
/// Represents a conflict in the timetable
/// </summary>
public class TimetableConflict
{
    /// <summary>
    /// ID of the scheduled lesson with conflicts
    /// </summary>
    public int ScheduledLessonId { get; set; }

    /// <summary>
    /// Severity of the conflict (Error or Warning)
    /// </summary>
    public ConflictSeverity Type { get; set; }

    /// <summary>
    /// List of conflict messages
    /// </summary>
    public List<string> Messages { get; set; } = new();

    /// <summary>
    /// List of constraint codes violated
    /// </summary>
    public List<string> ConstraintCodes { get; set; } = new();
}

/// <summary>
/// Severity level of a conflict
/// </summary>
public enum ConflictSeverity
{
    /// <summary>
    /// Hard constraint violation - schedule is invalid
    /// </summary>
    Error,

    /// <summary>
    /// Soft constraint violation - warning only
    /// </summary>
    Warning
}

/// <summary>
/// Context for constraint validation
/// </summary>
public class ValidationContext
{
    /// <summary>
    /// Optional list of specific constraint codes to check (null = check all)
    /// </summary>
    public List<string>? ConstraintCodesToCheck { get; set; }

    /// <summary>
    /// Optional list of constraint codes to skip
    /// </summary>
    public List<string>? ConstraintCodesToSkip { get; set; }

    /// <summary>
    /// Whether to include soft constraints in validation (default: true)
    /// </summary>
    public bool IncludeSoftConstraints { get; set; } = true;

    /// <summary>
    /// Whether to stop at the first error (default: false)
    /// </summary>
    public bool EarlyExit { get; set; } = false;

    /// <summary>
    /// Cache for storing loaded data during batch validation
    /// Key format: "entity_type:id:property"
    /// </summary>
    public Dictionary<string, object> Cache { get; set; } = new();

    /// <summary>
    /// Checks if a constraint should be validated based on context filters
    /// </summary>
    public bool ShouldCheckConstraint(string constraintCode)
    {
        // If specific constraints specified, only check those
        if (ConstraintCodesToCheck != null && ConstraintCodesToCheck.Any())
        {
            return ConstraintCodesToCheck.Contains(constraintCode,
                StringComparer.OrdinalIgnoreCase);
        }

        // If constraints to skip specified, skip those
        if (ConstraintCodesToSkip != null && ConstraintCodesToSkip.Any())
        {
            return !ConstraintCodesToSkip.Contains(constraintCode,
                StringComparer.OrdinalIgnoreCase);
        }

        return true;
    }
}
