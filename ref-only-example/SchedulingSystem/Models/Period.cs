using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Models;

/// <summary>
/// Represents a time period in the daily schedule (e.g., Period 1: 8:00-8:45)
/// </summary>
public class Period
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Period Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Period Number")]
    public int PeriodNumber { get; set; }

    [Required]
    [Display(Name = "Start Time")]
    public TimeSpan StartTime { get; set; }

    [Required]
    [Display(Name = "End Time")]
    public TimeSpan EndTime { get; set; }

    [Display(Name = "Is Break")]
    public bool IsBreak { get; set; } = false;

    [Display(Name = "Duration (Minutes)")]
    public int DurationMinutes => (int)(EndTime - StartTime).TotalMinutes;

    // Navigation properties
    public ICollection<ScheduledLesson> ScheduledLessons { get; set; } = new List<ScheduledLesson>();
}
