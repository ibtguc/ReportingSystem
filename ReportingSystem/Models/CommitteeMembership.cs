using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

public enum CommitteeRole
{
    Head,
    Member
}

public class CommitteeMembership
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int CommitteeId { get; set; }

    public CommitteeRole Role { get; set; } = CommitteeRole.Member;

    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;

    public DateTime? EffectiveTo { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Committee Committee { get; set; } = null!;
}
