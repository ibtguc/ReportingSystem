using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Models;

/// <summary>
/// Represents a student in the school
/// Imported from UNTIS GPU010.TXT
/// </summary>
public class Student
{
    public int Id { get; set; }

    /// <summary>
    /// GPU010 Field 1: Name (Abbreviated short name)
    /// </summary>
    [Required]
    [StringLength(20)]
    [Display(Name = "Short Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// GPU010 Field 2: Full Name (Last Name)
    /// </summary>
    [Required]
    [StringLength(100)]
    [Display(Name = "Last Name")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// GPU010 Field 8: First Name
    /// </summary>
    [StringLength(100)]
    [Display(Name = "First Name")]
    public string? FirstName { get; set; }

    /// <summary>
    /// GPU010 Field 9: Student Number
    /// </summary>
    [Required]
    [StringLength(50)]
    [Display(Name = "Student Number")]
    public string StudentNumber { get; set; } = string.Empty;

    /// <summary>
    /// GPU010 Field 10: Class
    /// </summary>
    [Display(Name = "Class")]
    public int? ClassId { get; set; }

    /// <summary>
    /// GPU010 Field 11: Gender (1 = female, 2 = male)
    /// </summary>
    [Display(Name = "Gender")]
    public int? Gender { get; set; }

    /// <summary>
    /// GPU010 Field 13: Date of birth (YYYYMMDD)
    /// </summary>
    [Display(Name = "Date of Birth")]
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// GPU010 Field 14: Email address
    /// </summary>
    [EmailAddress]
    [StringLength(200)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    /// <summary>
    /// GPU010 Field 3: Text
    /// </summary>
    [StringLength(100)]
    [Display(Name = "Text")]
    public string? Text { get; set; }

    /// <summary>
    /// GPU010 Field 4: Description
    /// </summary>
    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    /// <summary>
    /// GPU010 Field 5: Statistics 1
    /// </summary>
    [StringLength(20)]
    [Display(Name = "Statistic 1")]
    public string? Statistic1 { get; set; }

    /// <summary>
    /// GPU010 Field 6: Statistics 2
    /// </summary>
    [StringLength(20)]
    [Display(Name = "Statistic 2")]
    public string? Statistic2 { get; set; }

    /// <summary>
    /// GPU010 Field 7: Code
    /// </summary>
    [StringLength(20)]
    [Display(Name = "Code")]
    public string? Code { get; set; }

    /// <summary>
    /// GPU010 Field 12: (Course-) Optimisation Code
    /// </summary>
    [StringLength(20)]
    [Display(Name = "Optimisation Code")]
    public string? OptimisationCode { get; set; }

    /// <summary>
    /// UNTIS background color in hex format (e.g., "#FF5733")
    /// </summary>
    [StringLength(7)]
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// UNTIS foreground color in hex format (e.g., "#FFFFFF")
    /// </summary>
    [StringLength(7)]
    public string? ForegroundColor { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display name combining first and last name
    /// </summary>
    [Display(Name = "Display Name")]
    public string DisplayName => string.IsNullOrWhiteSpace(FirstName)
        ? FullName
        : $"{FirstName} {FullName}";

    /// <summary>
    /// Gender display text
    /// </summary>
    public string GenderDisplay => Gender switch
    {
        1 => "Female",
        2 => "Male",
        _ => "Not specified"
    };

    // Navigation properties
    public Class? Class { get; set; }
}
