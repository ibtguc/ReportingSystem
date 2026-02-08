using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

public enum HierarchyLevel
{
    TopLevel = 0,   // L0 — General Secretaries
    Directors = 1,  // L1 — Sector/Directorate heads
    Functions = 2,  // L2 — Department/Function heads
    Processes = 3,  // L3 — Process owners/team leads
    Tasks = 4       // L4 — Task executors
}

public class Committee
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public HierarchyLevel HierarchyLevel { get; set; }

    public int? ParentCommitteeId { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(100)]
    public string? Sector { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Committee? ParentCommittee { get; set; }
    public ICollection<Committee> SubCommittees { get; set; } = new List<Committee>();
    public ICollection<CommitteeMembership> Memberships { get; set; } = new List<CommitteeMembership>();
    public ICollection<ShadowAssignment> ShadowAssignments { get; set; } = new List<ShadowAssignment>();
}
