using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

/// <summary>
/// Stores the actual value entered by a user for a specific field in a report.
/// Values are stored as strings and interpreted based on the field type.
/// </summary>
public class ReportFieldValue
{
    public int Id { get; set; }

    [Required]
    public int ReportId { get; set; }

    [Required]
    public int ReportFieldId { get; set; }

    /// <summary>
    /// The stored value as text. Interpretation depends on the field type:
    /// - Text/RichText: raw text or HTML
    /// - Numeric: parseable number string
    /// - Date: ISO 8601 date string
    /// - Dropdown: selected option value
    /// - Checkbox: "true"/"false"
    /// - FileUpload: attachment ID reference
    /// - TableGrid: JSON array of row objects
    /// </summary>
    [Display(Name = "Value")]
    public string? Value { get; set; }

    /// <summary>
    /// For numeric fields, the parsed numeric value for aggregation.
    /// </summary>
    [Display(Name = "Numeric Value")]
    public double? NumericValue { get; set; }

    /// <summary>
    /// Whether this value was pre-populated from a previous period.
    /// </summary>
    [Display(Name = "Pre-Populated")]
    public bool WasPrePopulated { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Report Report { get; set; } = null!;
    public ReportField ReportField { get; set; } = null!;
}
