using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

public class DirectiveStatusHistory
{
    public int Id { get; set; }

    public int DirectiveId { get; set; }

    public DirectiveStatus OldStatus { get; set; }

    public DirectiveStatus NewStatus { get; set; }

    public int ChangedById { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    [StringLength(1000)]
    public string? Comments { get; set; }

    // Navigation properties
    public Directive Directive { get; set; } = null!;
    public User ChangedBy { get; set; } = null!;
}
