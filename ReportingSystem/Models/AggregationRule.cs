using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

/// <summary>
/// Defines how a report field should be aggregated when rolling up data
/// from lower hierarchy levels to higher levels.
/// </summary>
public class AggregationRule
{
    public int Id { get; set; }

    [Required]
    public int ReportFieldId { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Rule Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required]
    [StringLength(30)]
    [Display(Name = "Aggregation Method")]
    public string Method { get; set; } = AggregationMethod.Sum;

    // For weighted average - the field key to use for weights
    [StringLength(50)]
    [Display(Name = "Weight Field Key")]
    public string? WeightFieldKey { get; set; }

    // Custom formula for calculated aggregates (e.g., "SUM(value) / COUNT(*) * 100")
    [StringLength(500)]
    [Display(Name = "Custom Formula")]
    public string? CustomFormula { get; set; }

    // For textual aggregation - how to combine text values
    [StringLength(30)]
    [Display(Name = "Text Aggregation Mode")]
    public string? TextAggregationMode { get; set; }

    // Maximum number of text items to include when concatenating
    [Display(Name = "Max Text Items")]
    public int? MaxTextItems { get; set; }

    // Separator for text concatenation
    [StringLength(20)]
    [Display(Name = "Text Separator")]
    public string? TextSeparator { get; set; }

    // Whether to include null/empty values in aggregation
    [Display(Name = "Include Empty Values")]
    public bool IncludeEmptyValues { get; set; } = false;

    // Minimum number of source values required for valid aggregation
    [Display(Name = "Min Source Values")]
    public int MinSourceValues { get; set; } = 1;

    // Rounding precision for numeric results
    [Display(Name = "Decimal Precision")]
    public int DecimalPrecision { get; set; } = 2;

    // Display format for the aggregated value
    [StringLength(50)]
    [Display(Name = "Display Format")]
    public string? DisplayFormat { get; set; }

    // Whether this rule is applied automatically or manually triggered
    [Display(Name = "Auto Aggregate")]
    public bool AutoAggregate { get; set; } = true;

    // Priority when multiple rules exist for same field (lower = higher priority)
    [Display(Name = "Priority")]
    public int Priority { get; set; } = 100;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ReportField ReportField { get; set; } = null!;
    public ICollection<AggregatedValue> AggregatedValues { get; set; } = new List<AggregatedValue>();

    // Computed properties
    public bool IsNumericMethod => Method is AggregationMethod.Sum or AggregationMethod.Average
        or AggregationMethod.WeightedAverage or AggregationMethod.Min or AggregationMethod.Max
        or AggregationMethod.Count or AggregationMethod.Percentage;

    public bool IsTextMethod => Method is AggregationMethod.Concatenate or AggregationMethod.SelectFirst
        or AggregationMethod.SelectLast or AggregationMethod.SelectMostCommon or AggregationMethod.ManualSynthesis;

    public bool RequiresWeightField => Method == AggregationMethod.WeightedAverage;

    public bool RequiresCustomFormula => Method == AggregationMethod.Custom;

    public string MethodDisplayName => AggregationMethod.DisplayName(Method);
}

/// <summary>
/// Available aggregation methods for numeric and textual data.
/// </summary>
public static class AggregationMethod
{
    // Numeric methods
    public const string Sum = "Sum";
    public const string Average = "Average";
    public const string WeightedAverage = "WeightedAverage";
    public const string Min = "Min";
    public const string Max = "Max";
    public const string Count = "Count";
    public const string Percentage = "Percentage";
    public const string Custom = "Custom";

    // Textual methods
    public const string Concatenate = "Concatenate";
    public const string SelectFirst = "SelectFirst";
    public const string SelectLast = "SelectLast";
    public const string SelectMostCommon = "SelectMostCommon";
    public const string ManualSynthesis = "ManualSynthesis";

    public static string DisplayName(string method) => method switch
    {
        Sum => "Sum",
        Average => "Average",
        WeightedAverage => "Weighted Average",
        Min => "Minimum",
        Max => "Maximum",
        Count => "Count",
        Percentage => "Percentage",
        Custom => "Custom Formula",
        Concatenate => "Concatenate",
        SelectFirst => "Select First",
        SelectLast => "Select Last",
        SelectMostCommon => "Most Common",
        ManualSynthesis => "Manual Synthesis",
        _ => method
    };

    public static string BadgeClass(string method) => method switch
    {
        Sum or Average or WeightedAverage => "bg-primary",
        Min or Max => "bg-info",
        Count or Percentage => "bg-secondary",
        Custom => "bg-warning",
        Concatenate or SelectFirst or SelectLast => "bg-success",
        SelectMostCommon or ManualSynthesis => "bg-dark",
        _ => "bg-secondary"
    };

    public static IEnumerable<string> NumericMethods => new[] { Sum, Average, WeightedAverage, Min, Max, Count, Percentage, Custom };
    public static IEnumerable<string> TextMethods => new[] { Concatenate, SelectFirst, SelectLast, SelectMostCommon, ManualSynthesis };
    public static IEnumerable<string> All => NumericMethods.Concat(TextMethods);
}

/// <summary>
/// Modes for aggregating text values.
/// </summary>
public static class TextAggregationMode
{
    public const string BulletList = "BulletList";
    public const string NumberedList = "NumberedList";
    public const string CommaSeparated = "CommaSeparated";
    public const string NewLineSeparated = "NewLineSeparated";
    public const string Paragraph = "Paragraph";

    public static string DisplayName(string mode) => mode switch
    {
        BulletList => "Bullet List",
        NumberedList => "Numbered List",
        CommaSeparated => "Comma Separated",
        NewLineSeparated => "New Line Separated",
        Paragraph => "Paragraph",
        _ => mode
    };

    public static IEnumerable<string> All => new[] { BulletList, NumberedList, CommaSeparated, NewLineSeparated, Paragraph };
}
