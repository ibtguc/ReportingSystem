using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

public class ShadowAssignment
{
    public int Id { get; set; }

    public int PrincipalUserId { get; set; }

    public int ShadowUserId { get; set; }

    public int CommitteeId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;

    public DateTime? EffectiveTo { get; set; }

    // Navigation properties
    public User PrincipalUser { get; set; } = null!;
    public User ShadowUser { get; set; } = null!;
    public Committee Committee { get; set; } = null!;
}
