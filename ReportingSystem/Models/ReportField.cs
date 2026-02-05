using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

/// <summary>
/// Defines an individual field within a report template.
/// Supports multiple field types, validation rules, calculated fields, and conditional visibility.
/// </summary>
public class ReportField
{
    public int Id { get; set; }

    [Required]
    public int ReportTemplateId { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Field Label")]
    public string Label { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Field Key")]
    public string? FieldKey { get; set; }

    [StringLength(500)]
    [Display(Name = "Help Text")]
    public string? HelpText { get; set; }

    [Required]
    [Display(Name = "Field Type")]
    public FieldType Type { get; set; } = FieldType.Text;

    [StringLength(100)]
    [Display(Name = "Section")]
    public string? Section { get; set; }

    [Display(Name = "Section Order")]
    public int SectionOrder { get; set; }

    [Display(Name = "Field Order")]
    public int FieldOrder { get; set; }

    [Display(Name = "Required")]
    public bool IsRequired { get; set; }

    // Validation rules
    [Display(Name = "Minimum Value")]
    public double? MinValue { get; set; }

    [Display(Name = "Maximum Value")]
    public double? MaxValue { get; set; }

    [Display(Name = "Min Length")]
    public int? MinLength { get; set; }

    [Display(Name = "Max Length")]
    public int? MaxLength { get; set; }

    [StringLength(500)]
    [Display(Name = "Regex Pattern")]
    public string? RegexPattern { get; set; }

    [StringLength(200)]
    [Display(Name = "Validation Message")]
    public string? ValidationMessage { get; set; }

    // Dropdown/Checkbox options (JSON array: ["Option1","Option2"])
    [StringLength(2000)]
    [Display(Name = "Options (JSON)")]
    public string? OptionsJson { get; set; }

    // Calculated field support
    [Display(Name = "Calculated Field")]
    public bool IsCalculated { get; set; }

    [StringLength(500)]
    [Display(Name = "Formula")]
    public string? Formula { get; set; }

    // Conditional visibility (JSON: {"fieldKey":"status","operator":"equals","value":"Active"})
    [StringLength(1000)]
    [Display(Name = "Visibility Condition (JSON)")]
    public string? VisibilityConditionJson { get; set; }

    // Table/Grid field config (JSON: column definitions)
    [StringLength(2000)]
    [Display(Name = "Table Columns (JSON)")]
    public string? TableColumnsJson { get; set; }

    [Display(Name = "Default Value")]
    [StringLength(500)]
    public string? DefaultValue { get; set; }

    [Display(Name = "Pre-Populate from Previous")]
    public bool PrePopulateFromPrevious { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ReportTemplate ReportTemplate { get; set; } = null!;
    public ICollection<ReportFieldValue> Values { get; set; } = new List<ReportFieldValue>();

    /// <summary>
    /// Display-friendly type name.
    /// </summary>
    public string TypeDisplayName => Type switch
    {
        FieldType.Text => "Text",
        FieldType.Numeric => "Numeric",
        FieldType.Date => "Date",
        FieldType.Dropdown => "Dropdown",
        FieldType.Checkbox => "Checkbox",
        FieldType.FileUpload => "File Upload",
        FieldType.RichText => "Rich Text",
        FieldType.TableGrid => "Table/Grid",
        _ => Type.ToString()
    };
}

/// <summary>
/// Supported report field types per the SRS (FR-202).
/// </summary>
public enum FieldType
{
    [Display(Name = "Text")]
    Text = 0,

    [Display(Name = "Numeric")]
    Numeric = 1,

    [Display(Name = "Date")]
    Date = 2,

    [Display(Name = "Dropdown")]
    Dropdown = 3,

    [Display(Name = "Checkbox")]
    Checkbox = 4,

    [Display(Name = "File Upload")]
    FileUpload = 5,

    [Display(Name = "Rich Text")]
    RichText = 6,

    [Display(Name = "Table/Grid")]
    TableGrid = 7
}
