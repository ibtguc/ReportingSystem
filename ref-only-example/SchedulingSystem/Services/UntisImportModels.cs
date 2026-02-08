namespace SchedulingSystem.Services;

/// <summary>
/// In-memory data models for UNTIS import processing.
/// These are used to analyze and detect co-teaching before inserting to database.
/// </summary>
public class UntisImportModels
{
    /// <summary>
    /// Represents a lesson definition parsed from GPU002.TXT (in-memory)
    /// </summary>
    public class ParsedLessonDefinition
    {
        public int LessonNumber { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public int PeriodsPerWeek { get; set; }
        public int ClassPeriodsPerWeek { get; set; }
        public int TeacherPeriodsPerWeek { get; set; }
        public int? NumberOfStudents { get; set; }
        public int? MaleStudents { get; set; }
        public int? FemaleStudents { get; set; }
        public decimal? WeekValue { get; set; }
        public decimal? YearValue { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? PartitionNumber { get; set; }
        public string? StudentGroup { get; set; }
        public string? HomeRoom { get; set; }
        public string? RequiredRoomType { get; set; }
        public int? MinDoublePeriods { get; set; }
        public int? MaxDoublePeriods { get; set; }
        public int? BlockSize { get; set; }
        public int? Priority { get; set; }
        public int? ConsecutiveSubjectsClass { get; set; }
        public int? ConsecutiveSubjectsTeacher { get; set; }
        public string? Codes { get; set; }
        public string? Description { get; set; }
        public string? ForegroundColor { get; set; }
        public string? BackgroundColor { get; set; }
        public string? WeeklyPeriodsInTerms { get; set; }
    }

    /// <summary>
    /// Represents a scheduled lesson entry parsed from GPU001.TXT (in-memory)
    /// </summary>
    public class ParsedScheduleEntry
    {
        public int LessonNumber { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public int DayNumber { get; set; } // 1-5 for Sunday-Thursday
        public int PeriodNumber { get; set; }

        /// <summary>
        /// Key for grouping: same class + same day + same period = potential co-teaching
        /// </summary>
        public string GroupingKey => $"{ClassName}|{DayNumber}|{PeriodNumber}";
    }

    /// <summary>
    /// Represents a detected lesson (possibly with co-teaching) ready for database insertion
    /// </summary>
    public class DetectedLesson
    {
        /// <summary>
        /// Unique identifier for this detected lesson (class + day + period)
        /// </summary>
        public string LessonKey { get; set; } = string.Empty;

        /// <summary>
        /// Class name
        /// </summary>
        public string ClassName { get; set; } = string.Empty;

        /// <summary>
        /// All subject codes for this lesson
        /// </summary>
        public List<string> SubjectCodes { get; set; } = new();

        /// <summary>
        /// All teacher names for this lesson
        /// </summary>
        public List<string> TeacherNames { get; set; } = new();

        /// <summary>
        /// All room numbers for this lesson
        /// </summary>
        public List<string> RoomNumbers { get; set; } = new();

        /// <summary>
        /// Day of week (1-5)
        /// </summary>
        public int DayNumber { get; set; }

        /// <summary>
        /// Period number
        /// </summary>
        public int PeriodNumber { get; set; }

        /// <summary>
        /// Original schedule entries that were merged into this detected lesson
        /// </summary>
        public List<ParsedScheduleEntry> SourceEntries { get; set; } = new();

        /// <summary>
        /// Original lesson definitions that were merged
        /// </summary>
        public List<ParsedLessonDefinition> SourceDefinitions { get; set; } = new();

        /// <summary>
        /// Is this a co-teaching scenario?
        /// </summary>
        public bool IsCoTeaching => TeacherNames.Count > 1;

        /// <summary>
        /// Is this a multi-subject scenario?
        /// </summary>
        public bool IsMultiSubject => SubjectCodes.Distinct().Count() > 1;

        /// <summary>
        /// Is this a multi-room scenario?
        /// </summary>
        public bool IsMultiRoom => RoomNumbers.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().Count() > 1;

        /// <summary>
        /// Get the primary lesson definition (first one, or one with most periods per week)
        /// </summary>
        public ParsedLessonDefinition GetPrimaryDefinition()
        {
            if (SourceDefinitions.Count == 0)
                throw new InvalidOperationException("No source definitions available");

            // Choose the one with highest periods per week as primary
            return SourceDefinitions
                .OrderByDescending(d => d.PeriodsPerWeek)
                .ThenBy(d => d.LessonNumber)
                .First();
        }
    }

    /// <summary>
    /// Represents combined data from schedule entry and lesson definition for processing
    /// </summary>
    public class CombinedLessonData
    {
        public ParsedScheduleEntry ScheduleEntry { get; set; } = null!;
        public ParsedLessonDefinition Definition { get; set; } = null!;
    }
}
