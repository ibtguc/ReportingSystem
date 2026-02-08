namespace ReportingSystem.Models;

/// <summary>
/// Links a summary report to its source reports (many-to-many).
/// A summary can aggregate multiple source reports, and a source
/// report can be referenced by multiple summaries at different levels.
/// </summary>
public class ReportSourceLink
{
    public int Id { get; set; }

    /// <summary>
    /// The summary/aggregated report.
    /// </summary>
    public int SummaryReportId { get; set; }

    /// <summary>
    /// The original source report being summarized.
    /// </summary>
    public int SourceReportId { get; set; }

    /// <summary>
    /// Optional annotation added by the summarizer about this source.
    /// </summary>
    public string? Annotation { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Report SummaryReport { get; set; } = null!;
    public Report SourceReport { get; set; } = null!;
}
