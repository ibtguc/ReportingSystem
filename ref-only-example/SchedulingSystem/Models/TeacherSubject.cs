using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Models;

/// <summary>
/// Represents a teacher's qualification/ability to teach a specific subject
/// Many-to-Many relationship between Teachers and Subjects
/// </summary>
public class TeacherSubject
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Teacher")]
    public int TeacherId { get; set; }

    [Required]
    [Display(Name = "Subject")]
    public int SubjectId { get; set; }

    [Display(Name = "Qualification Level")]
    [StringLength(50)]
    public string? QualificationLevel { get; set; } // e.g., "Expert", "Qualified", "Basic"

    [Display(Name = "Class Level From")]
    public int? ClassLevelFrom { get; set; } // Minimum class level qualified to teach

    [Display(Name = "Class Level To")]
    public int? ClassLevelTo { get; set; } // Maximum class level qualified to teach

    [Display(Name = "Preferred")]
    public bool IsPreferred { get; set; } = false; // Teacher prefers teaching this subject

    [Display(Name = "Notes")]
    [StringLength(500)]
    public string? Notes { get; set; }

    // Navigation properties
    public Teacher? Teacher { get; set; }
    public Subject? Subject { get; set; }
}
