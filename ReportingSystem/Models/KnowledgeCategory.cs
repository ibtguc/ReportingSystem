using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

public class KnowledgeCategory
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Icon class (Bootstrap Icons) for display.
    /// </summary>
    [StringLength(50)]
    public string Icon { get; set; } = "bi-folder";

    public int? ParentCategoryId { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public KnowledgeCategory? ParentCategory { get; set; }
    public List<KnowledgeCategory> SubCategories { get; set; } = new();
    public List<KnowledgeArticle> Articles { get; set; } = new();
}
