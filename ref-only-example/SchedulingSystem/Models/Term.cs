using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Models;

/// <summary>
/// Represents a term/semester within a school year
/// </summary>
public class Term
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Term Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; }

    [Required]
    [Display(Name = "End Date")]
    public DateTime EndDate { get; set; }

    // Foreign key
    [Required]
    [Display(Name = "School Year")]
    public int SchoolYearId { get; set; }

    // Navigation property
    public SchoolYear? SchoolYear { get; set; }
}
