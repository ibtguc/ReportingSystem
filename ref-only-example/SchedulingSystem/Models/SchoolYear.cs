using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Models;

/// <summary>
/// Represents an academic year (e.g., 2024-2025)
/// </summary>
public class SchoolYear
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "School Year")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; }

    [Required]
    [Display(Name = "End Date")]
    public DateTime EndDate { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Term> Terms { get; set; } = new List<Term>();
}
