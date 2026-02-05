using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

/// <summary>
/// Represents a formal decision on an upward flow request.
/// Links to SuggestedAction, ResourceRequest, or SupportRequest.
/// </summary>
public class Decision
{
    public int Id { get; set; }

    /// <summary>
    /// The report containing the original request.
    /// </summary>
    [Required]
    public int ReportId { get; set; }

    /// <summary>
    /// User making the decision.
    /// </summary>
    [Required]
    public int DecidedById { get; set; }

    /// <summary>
    /// Type of request this decision relates to.
    /// </summary>
    [Required]
    [StringLength(30)]
    [Display(Name = "Request Type")]
    public string RequestType { get; set; } = DecisionRequestType.SuggestedAction;

    /// <summary>
    /// ID of the related SuggestedAction (if applicable).
    /// </summary>
    public int? SuggestedActionId { get; set; }

    /// <summary>
    /// ID of the related ResourceRequest (if applicable).
    /// </summary>
    public int? ResourceRequestId { get; set; }

    /// <summary>
    /// ID of the related SupportRequest (if applicable).
    /// </summary>
    public int? SupportRequestId { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    [Display(Name = "Outcome")]
    public string Outcome { get; set; } = DecisionOutcome.Pending;

    [Required]
    [StringLength(2000)]
    [Display(Name = "Justification")]
    public string Justification { get; set; } = string.Empty;

    /// <summary>
    /// Any conditions attached to the decision.
    /// </summary>
    [StringLength(2000)]
    [Display(Name = "Conditions")]
    public string? Conditions { get; set; }

    /// <summary>
    /// Date when the decision becomes effective.
    /// </summary>
    [Display(Name = "Effective Date")]
    public DateTime? EffectiveDate { get; set; }

    /// <summary>
    /// Approved budget amount (for ResourceRequest decisions).
    /// </summary>
    [Display(Name = "Approved Amount")]
    public decimal? ApprovedAmount { get; set; }

    [StringLength(10)]
    [Display(Name = "Currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Modified request details (for "Approved with Modifications").
    /// </summary>
    [StringLength(2000)]
    [Display(Name = "Modifications")]
    public string? Modifications { get; set; }

    /// <summary>
    /// Referral target (for "Referred" decisions).
    /// </summary>
    [StringLength(200)]
    [Display(Name = "Referred To")]
    public string? ReferredTo { get; set; }

    /// <summary>
    /// Whether the originator has acknowledged this decision.
    /// </summary>
    public bool IsAcknowledged { get; set; } = false;

    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// Optional response from the originator.
    /// </summary>
    [StringLength(1000)]
    [Display(Name = "Acknowledgment Response")]
    public string? AcknowledgmentResponse { get; set; }

    /// <summary>
    /// Whether to cascade this decision notification through hierarchy.
    /// </summary>
    public bool CascadeNotification { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Report Report { get; set; } = null!;
    public User DecidedBy { get; set; } = null!;
    public SuggestedAction? SuggestedAction { get; set; }
    public ResourceRequest? ResourceRequest { get; set; }
    public SupportRequest? SupportRequest { get; set; }

    /// <summary>
    /// Display name combining title and outcome.
    /// </summary>
    public string DisplayName => $"{Title} ({DecisionOutcome.DisplayName(Outcome)})";

    /// <summary>
    /// Check if decision is positive (approved in any form).
    /// </summary>
    public bool IsPositive => Outcome == DecisionOutcome.Approved ||
                              Outcome == DecisionOutcome.ApprovedWithModifications ||
                              Outcome == DecisionOutcome.PartiallyApproved;

    /// <summary>
    /// Check if decision requires acknowledgment.
    /// </summary>
    public bool IsPendingAcknowledgment => !IsAcknowledged && Outcome != DecisionOutcome.Pending;

    /// <summary>
    /// Get the display name of the related request.
    /// </summary>
    public string RelatedRequestTitle => RequestType switch
    {
        DecisionRequestType.SuggestedAction => SuggestedAction?.Title ?? "Unknown Action",
        DecisionRequestType.ResourceRequest => ResourceRequest?.Title ?? "Unknown Resource",
        DecisionRequestType.SupportRequest => SupportRequest?.Title ?? "Unknown Support",
        _ => "Unknown"
    };
}

/// <summary>
/// Types of requests that can have decisions.
/// </summary>
public static class DecisionRequestType
{
    public const string SuggestedAction = "SuggestedAction";
    public const string ResourceRequest = "ResourceRequest";
    public const string SupportRequest = "SupportRequest";

    public static readonly string[] All = [SuggestedAction, ResourceRequest, SupportRequest];

    public static string DisplayName(string type) => type switch
    {
        SuggestedAction => "Suggested Action",
        ResourceRequest => "Resource Request",
        SupportRequest => "Support Request",
        _ => type
    };

    public static string BadgeClass(string type) => type switch
    {
        SuggestedAction => "bg-info",
        ResourceRequest => "bg-warning text-dark",
        SupportRequest => "bg-danger",
        _ => "bg-secondary"
    };

    public static string Icon(string type) => type switch
    {
        SuggestedAction => "bi-lightbulb",
        ResourceRequest => "bi-box-seam",
        SupportRequest => "bi-life-preserver",
        _ => "bi-question"
    };
}

/// <summary>
/// Outcome values for decisions.
/// </summary>
public static class DecisionOutcome
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string ApprovedWithModifications = "ApprovedWithMods";
    public const string PartiallyApproved = "PartiallyApproved";
    public const string Deferred = "Deferred";
    public const string Rejected = "Rejected";
    public const string Referred = "Referred";

    public static readonly string[] All = [Pending, Approved, ApprovedWithModifications,
                                           PartiallyApproved, Deferred, Rejected, Referred];

    public static string DisplayName(string outcome) => outcome switch
    {
        Pending => "Pending",
        Approved => "Approved",
        ApprovedWithModifications => "Approved with Modifications",
        PartiallyApproved => "Partially Approved",
        Deferred => "Deferred",
        Rejected => "Rejected",
        Referred => "Referred",
        _ => outcome
    };

    public static string BadgeClass(string outcome) => outcome switch
    {
        Pending => "bg-secondary",
        Approved => "bg-success",
        ApprovedWithModifications => "bg-info",
        PartiallyApproved => "bg-warning text-dark",
        Deferred => "bg-secondary",
        Rejected => "bg-danger",
        Referred => "bg-primary",
        _ => "bg-secondary"
    };

    public static string Icon(string outcome) => outcome switch
    {
        Pending => "bi-hourglass-split",
        Approved => "bi-check-circle-fill",
        ApprovedWithModifications => "bi-check-circle",
        PartiallyApproved => "bi-check",
        Deferred => "bi-clock-history",
        Rejected => "bi-x-circle-fill",
        Referred => "bi-arrow-right-circle",
        _ => "bi-question"
    };
}
