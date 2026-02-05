using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

/// <summary>
/// Stores aggregated/rolled-up values for a report field at a specific
/// organizational unit level. Supports drill-down to source reports.
/// </summary>
public class AggregatedValue
{
    public int Id { get; set; }

    [Required]
    public int AggregationRuleId { get; set; }

    [Required]
    public int ReportPeriodId { get; set; }

    [Required]
    public int OrganizationalUnitId { get; set; }

    // The computed aggregated value (stored as string for flexibility)
    [StringLength(4000)]
    [Display(Name = "Aggregated Value")]
    public string? Value { get; set; }

    // Numeric value for easier sorting/comparison
    [Display(Name = "Numeric Value")]
    public double? NumericValue { get; set; }

    // Number of source reports that contributed to this aggregation
    [Display(Name = "Source Count")]
    public int SourceCount { get; set; }

    // IDs of source reports (JSON array: [1,2,3])
    [StringLength(2000)]
    [Display(Name = "Source Report IDs (JSON)")]
    public string? SourceReportIdsJson { get; set; }

    // Aggregation metadata for drill-down
    [StringLength(2000)]
    [Display(Name = "Aggregation Details (JSON)")]
    public string? AggregationDetailsJson { get; set; }

    // Status of this aggregation
    [Required]
    [StringLength(30)]
    [Display(Name = "Status")]
    public string Status { get; set; } = AggregatedValueStatus.Current;

    // Whether this value has been amended by a manager
    [Display(Name = "Has Amendment")]
    public bool HasAmendment { get; set; } = false;

    // Timestamp of last computation
    [Display(Name = "Computed At")]
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;

    // User who triggered the computation (null for auto)
    public int? ComputedById { get; set; }

    // Whether all expected source reports were included
    [Display(Name = "Is Complete")]
    public bool IsComplete { get; set; } = true;

    // Missing source org units that haven't submitted (JSON array)
    [StringLength(1000)]
    [Display(Name = "Missing Sources (JSON)")]
    public string? MissingSourcesJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public AggregationRule AggregationRule { get; set; } = null!;
    public ReportPeriod ReportPeriod { get; set; } = null!;
    public OrganizationalUnit OrganizationalUnit { get; set; } = null!;
    public User? ComputedBy { get; set; }
    public ICollection<ManagerAmendment> Amendments { get; set; } = new List<ManagerAmendment>();

    // Computed properties
    public bool IsStale => Status == AggregatedValueStatus.Stale;
    public bool IsPending => Status == AggregatedValueStatus.Pending;
    public bool NeedsRecomputation => IsStale || !IsComplete;

    public string StatusDisplayName => AggregatedValueStatus.DisplayName(Status);
    public string StatusBadgeClass => AggregatedValueStatus.BadgeClass(Status);

    /// <summary>
    /// Gets the display value, considering amendments.
    /// </summary>
    public string DisplayValue => HasAmendment && Amendments.Any(a => a.IsActive)
        ? Amendments.Where(a => a.IsActive).OrderByDescending(a => a.CreatedAt).First().AmendedValue ?? Value ?? ""
        : Value ?? "";
}

/// <summary>
/// Status values for aggregated data.
/// </summary>
public static class AggregatedValueStatus
{
    public const string Pending = "Pending";
    public const string Current = "Current";
    public const string Stale = "Stale";
    public const string Error = "Error";
    public const string ManualOverride = "ManualOverride";

    public static string DisplayName(string status) => status switch
    {
        Pending => "Pending",
        Current => "Current",
        Stale => "Stale",
        Error => "Error",
        ManualOverride => "Manual Override",
        _ => status
    };

    public static string BadgeClass(string status) => status switch
    {
        Current => "bg-success",
        Pending => "bg-warning",
        Stale => "bg-secondary",
        Error => "bg-danger",
        ManualOverride => "bg-info",
        _ => "bg-secondary"
    };

    public static IEnumerable<string> All => new[] { Pending, Current, Stale, Error, ManualOverride };
}
