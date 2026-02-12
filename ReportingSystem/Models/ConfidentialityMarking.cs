using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

public enum ConfidentialItemType
{
    Report,
    Directive,
    Meeting
}

/// <summary>
/// Tracks confidentiality markings on items (reports, directives, meetings).
/// Implements FR-4.5.1 — Confidentiality Marking with full audit trail.
/// </summary>
public class ConfidentialityMarking
{
    public int Id { get; set; }

    public ConfidentialItemType ItemType { get; set; }

    /// <summary>
    /// The ID of the marked item (Report.Id, Directive.Id, or Meeting.Id).
    /// </summary>
    public int ItemId { get; set; }

    /// <summary>
    /// The user who applied the confidentiality marking.
    /// </summary>
    public int MarkedById { get; set; }

    /// <summary>
    /// The hierarchy level of the marker's committee at the time of marking.
    /// Used to determine who can access: only users at higher (lower numeric) levels.
    /// </summary>
    public HierarchyLevel MarkerCommitteeLevel { get; set; }

    /// <summary>
    /// The committee context in which the item was marked confidential.
    /// </summary>
    public int MarkerCommitteeId { get; set; }

    /// <summary>
    /// For Chairman's Office items: minimum rank required to access (1=senior, 4=junior).
    /// Null for non-CO items. Users with equal or higher rank (lower number) retain access.
    /// FR-4.5.2: Rank-based access within Chairman's Office.
    /// </summary>
    public int? MinChairmanOfficeRank { get; set; }

    /// <summary>
    /// Whether this marking is currently active. False = unmarked/reversed.
    /// FR-4.5.1.7: Markings are reversible by original marker or SystemAdmin.
    /// </summary>
    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Reason { get; set; }

    public DateTime MarkedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the marking was reversed (if applicable).
    /// </summary>
    public DateTime? UnmarkedAt { get; set; }

    /// <summary>
    /// Who reversed the marking (original marker or SystemAdmin).
    /// </summary>
    public int? UnmarkedById { get; set; }

    // Navigation properties
    public User MarkedBy { get; set; } = null!;
    public Committee MarkerCommittee { get; set; } = null!;
    public User? UnmarkedBy { get; set; }
}
