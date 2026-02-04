using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

/// <summary>
/// Represents a temporary transfer of reporting authority from one user to another.
/// Used when a manager is absent and needs someone to handle their reporting duties.
/// </summary>
public class Delegation
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Delegator")]
    public int DelegatorId { get; set; }

    [Required]
    [Display(Name = "Delegate")]
    public int DelegateId { get; set; }

    [Required]
    [Display(Name = "Start Date")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [Required]
    [Display(Name = "End Date")]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Scope")]
    public string Scope { get; set; } = DelegationScope.Full;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RevokedAt { get; set; }

    // Navigation properties
    [Display(Name = "Delegator")]
    public User Delegator { get; set; } = null!;

    [Display(Name = "Delegate")]
    public User Delegate { get; set; } = null!;

    /// <summary>
    /// Whether this delegation is currently in effect (active, within date range).
    /// </summary>
    public bool IsCurrentlyEffective =>
        IsActive &&
        RevokedAt == null &&
        StartDate <= DateTime.UtcNow &&
        EndDate >= DateTime.UtcNow;
}

/// <summary>
/// Defines the scope of a delegation.
/// </summary>
public static class DelegationScope
{
    public const string Full = "Full";
    public const string ReportingOnly = "ReportingOnly";
    public const string ApprovalOnly = "ApprovalOnly";

    public static readonly string[] All = [Full, ReportingOnly, ApprovalOnly];

    public static string DisplayName(string scope) => scope switch
    {
        Full => "Full Authority",
        ReportingOnly => "Reporting Only",
        ApprovalOnly => "Approval Only",
        _ => scope
    };
}
