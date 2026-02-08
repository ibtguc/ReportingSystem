using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Models;

/// <summary>
/// Represents a department/division in the school (e.g., PYP, MYP, STEM)
/// Maps to GPU007.TXT from UNTIS export
/// </summary>
public class Department
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Department Code")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Full Name")]
    public string? FullName { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();
    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}
