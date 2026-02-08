using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

public class ReportStatusHistory
{
    public int Id { get; set; }

    public int ReportId { get; set; }

    public ReportStatus OldStatus { get; set; }

    public ReportStatus NewStatus { get; set; }

    public int ChangedById { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    [StringLength(1000)]
    public string? Comments { get; set; }

    // Navigation properties
    public Report Report { get; set; } = null!;
    public User ChangedBy { get; set; } = null!;
}
