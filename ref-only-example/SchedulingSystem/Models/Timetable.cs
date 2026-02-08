using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Models;

/// <summary>
/// Represents a complete timetable/schedule
/// </summary>
public class Timetable
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Timetable Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "School Year")]
    public int SchoolYearId { get; set; }

    [Display(Name = "Term")]
    public int? TermId { get; set; }

    [Required]
    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Display(Name = "Status")]
    public TimetableStatus Status { get; set; } = TimetableStatus.Draft;

    [Display(Name = "Published Date")]
    public DateTime? PublishedDate { get; set; }

    [StringLength(1000)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    [Display(Name = "Generation Duration (ms)")]
    public long? GenerationDurationMs { get; set; }

    // Navigation properties
    public SchoolYear? SchoolYear { get; set; }
    public Term? Term { get; set; }
    public ICollection<ScheduledLesson> ScheduledLessons { get; set; } = new List<ScheduledLesson>();
    public ICollection<BreakSupervisionDuty> BreakSupervisionDuties { get; set; } = new List<BreakSupervisionDuty>();
}

public enum TimetableStatus
{
    Draft,
    Published,
    Archived
}
