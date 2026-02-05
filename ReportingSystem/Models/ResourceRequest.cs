using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

/// <summary>
/// Represents a resource request submitted as part of a report.
/// Used for requesting budget, equipment, personnel, training, etc.
/// </summary>
public class ResourceRequest
{
    public int Id { get; set; }

    [Required]
    public int ReportId { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Quantity/Amount")]
    public string? Quantity { get; set; }

    [StringLength(1000)]
    [Display(Name = "Justification")]
    public string? Justification { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Category")]
    public string Category { get; set; } = ResourceCategory.Equipment;

    [Required]
    [StringLength(20)]
    [Display(Name = "Urgency")]
    public string Urgency { get; set; } = ResourceUrgency.Medium;

    [Display(Name = "Estimated Cost")]
    [DataType(DataType.Currency)]
    public decimal? EstimatedCost { get; set; }

    [StringLength(20)]
    [Display(Name = "Currency")]
    public string? Currency { get; set; } = "EGP";

    [Required]
    [StringLength(30)]
    [Display(Name = "Status")]
    public string Status { get; set; } = ResourceStatus.Submitted;

    [StringLength(1000)]
    [Display(Name = "Review Comments")]
    public string? ReviewComments { get; set; }

    [Display(Name = "Approved Amount")]
    [DataType(DataType.Currency)]
    public decimal? ApprovedAmount { get; set; }

    [Display(Name = "Reviewed By")]
    public int? ReviewedById { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [Display(Name = "Fulfilled At")]
    public DateTime? FulfilledAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Report Report { get; set; } = null!;
    public User? ReviewedBy { get; set; }

    /// <summary>
    /// Display name combining title and status.
    /// </summary>
    public string DisplayName => $"{Title} ({ResourceStatus.DisplayName(Status)})";

    /// <summary>
    /// Formatted estimated cost with currency.
    /// </summary>
    public string EstimatedCostDisplay =>
        EstimatedCost.HasValue ? $"{EstimatedCost:N2} {Currency}" : "Not specified";
}

/// <summary>
/// Categories for resource requests.
/// </summary>
public static class ResourceCategory
{
    public const string Budget = "Budget";
    public const string Equipment = "Equipment";
    public const string Software = "Software";
    public const string Personnel = "Personnel";
    public const string Materials = "Materials";
    public const string Facilities = "Facilities";
    public const string Training = "Training";

    public static readonly string[] All = [Budget, Equipment, Software, Personnel, Materials, Facilities, Training];

    public static string DisplayName(string category) => category;
}

/// <summary>
/// Urgency levels for resource requests.
/// </summary>
public static class ResourceUrgency
{
    public const string Critical = "Critical";
    public const string High = "High";
    public const string Medium = "Medium";
    public const string Low = "Low";

    public static readonly string[] All = [Critical, High, Medium, Low];

    public static string DisplayName(string urgency) => urgency;

    public static string BadgeClass(string urgency) => urgency switch
    {
        Critical => "bg-danger",
        High => "bg-warning text-dark",
        Medium => "bg-info",
        Low => "bg-secondary",
        _ => "bg-secondary"
    };
}

/// <summary>
/// Status values for resource requests.
/// </summary>
public static class ResourceStatus
{
    public const string Submitted = "Submitted";
    public const string UnderReview = "UnderReview";
    public const string Approved = "Approved";
    public const string PartiallyApproved = "PartiallyApproved";
    public const string Rejected = "Rejected";
    public const string Fulfilled = "Fulfilled";

    public static readonly string[] All = [Submitted, UnderReview, Approved, PartiallyApproved, Rejected, Fulfilled];

    public static string DisplayName(string status) => status switch
    {
        Submitted => "Submitted",
        UnderReview => "Under Review",
        Approved => "Approved",
        PartiallyApproved => "Partially Approved",
        Rejected => "Rejected",
        Fulfilled => "Fulfilled",
        _ => status
    };

    public static string BadgeClass(string status) => status switch
    {
        Submitted => "bg-primary",
        UnderReview => "bg-warning text-dark",
        Approved => "bg-success",
        PartiallyApproved => "bg-info",
        Rejected => "bg-danger",
        Fulfilled => "bg-dark",
        _ => "bg-secondary"
    };
}
