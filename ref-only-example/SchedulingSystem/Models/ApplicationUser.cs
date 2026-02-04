using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Models;

/// <summary>
/// Custom user class extending IdentityUser for authentication
/// </summary>
public class ApplicationUser : IdentityUser
{
    [StringLength(100)]
    [Display(Name = "First Name")]
    public string? FirstName { get; set; }

    [StringLength(100)]
    [Display(Name = "Last Name")]
    public string? LastName { get; set; }

    [Display(Name = "Full Name")]
    public string FullName => $"{FirstName} {LastName}".Trim();

    // Optional links to Teacher or Student
    [Display(Name = "Teacher")]
    public int? TeacherId { get; set; }

    [Display(Name = "Student")]
    public int? StudentId { get; set; }
}
