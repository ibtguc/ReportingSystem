using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Services;

public class ReportTemplateService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReportTemplateService> _logger;

    public ReportTemplateService(
        ApplicationDbContext context,
        ILogger<ReportTemplateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all active templates, optionally filtered.
    /// </summary>
    public async Task<List<ReportTemplate>> GetTemplatesAsync(bool includeInactive = false)
    {
        var query = _context.ReportTemplates
            .Include(t => t.CreatedBy)
            .Include(t => t.Committee)
            .AsQueryable();

        if (!includeInactive)
            query = query.Where(t => t.IsActive);

        return await query
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Get a template by ID.
    /// </summary>
    public async Task<ReportTemplate?> GetTemplateByIdAsync(int id)
    {
        return await _context.ReportTemplates
            .Include(t => t.CreatedBy)
            .Include(t => t.Committee)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    /// <summary>
    /// Get applicable templates for a committee (FR-4.2.4.3).
    /// Returns templates that match: the specific committee, its hierarchy level, or are universal.
    /// </summary>
    public async Task<List<ReportTemplate>> GetTemplatesForCommitteeAsync(int committeeId)
    {
        var committee = await _context.Committees.FindAsync(committeeId);
        if (committee == null)
            return new List<ReportTemplate>();

        return await _context.ReportTemplates
            .Where(t => t.IsActive &&
                (t.CommitteeId == null && t.HierarchyLevel == null) ||  // Universal
                (t.CommitteeId == committeeId) ||                        // Committee-specific
                (t.HierarchyLevel == committee.HierarchyLevel && t.CommitteeId == null))  // Level-specific
            .OrderByDescending(t => t.CommitteeId != null)     // Committee-specific first
            .ThenByDescending(t => t.HierarchyLevel != null)   // Level-specific second
            .ThenBy(t => t.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Create a new report template (FR-4.2.4.2).
    /// </summary>
    public async Task<ReportTemplate> CreateTemplateAsync(ReportTemplate template, int createdById)
    {
        template.CreatedById = createdById;
        template.CreatedAt = DateTime.UtcNow;

        _context.ReportTemplates.Add(template);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created report template {TemplateId}: {Name}", template.Id, template.Name);
        return template;
    }

    /// <summary>
    /// Update a report template.
    /// </summary>
    public async Task<ReportTemplate?> UpdateTemplateAsync(ReportTemplate template)
    {
        var existing = await _context.ReportTemplates.FindAsync(template.Id);
        if (existing == null) return null;

        existing.Name = template.Name;
        existing.Description = template.Description;
        existing.ReportType = template.ReportType;
        existing.HierarchyLevel = template.HierarchyLevel;
        existing.CommitteeId = template.CommitteeId;
        existing.BodyTemplate = template.BodyTemplate;
        existing.IncludeSuggestedAction = template.IncludeSuggestedAction;
        existing.IncludeNeededResources = template.IncludeNeededResources;
        existing.IncludeNeededSupport = template.IncludeNeededSupport;
        existing.IncludeSpecialRemarks = template.IncludeSpecialRemarks;
        existing.RequireSuggestedAction = template.RequireSuggestedAction;
        existing.RequireNeededResources = template.RequireNeededResources;
        existing.RequireNeededSupport = template.RequireNeededSupport;
        existing.RequireSpecialRemarks = template.RequireSpecialRemarks;
        existing.IsActive = template.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated report template {TemplateId}: {Name}", existing.Id, existing.Name);
        return existing;
    }

    /// <summary>
    /// Delete a template (only non-default templates).
    /// </summary>
    public async Task<bool> DeleteTemplateAsync(int templateId)
    {
        var template = await _context.ReportTemplates.FindAsync(templateId);
        if (template == null) return false;
        if (template.IsDefault) return false;

        // Check if any reports use this template
        var usedByReports = await _context.Reports.AnyAsync(r => r.TemplateId == templateId);
        if (usedByReports)
        {
            // Soft delete — just deactivate
            template.IsActive = false;
            template.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deactivated report template {TemplateId} (in use by reports)", templateId);
            return true;
        }

        _context.ReportTemplates.Remove(template);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted report template {TemplateId}: {Name}", templateId, template.Name);
        return true;
    }

    /// <summary>
    /// Get template usage count (how many reports use it).
    /// </summary>
    public async Task<int> GetTemplateUsageCountAsync(int templateId)
    {
        return await _context.Reports.CountAsync(r => r.TemplateId == templateId);
    }

    /// <summary>
    /// Seed the 5 default templates (FR-4.2.4.4).
    /// Called from Program.cs during startup.
    /// </summary>
    public async Task SeedDefaultTemplatesAsync()
    {
        if (await _context.ReportTemplates.AnyAsync(t => t.IsDefault))
            return; // Already seeded

        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.SystemRole == SystemRole.SystemAdmin);
        var createdById = adminUser?.Id ?? 1;

        var defaults = new List<ReportTemplate>
        {
            new()
            {
                Name = "Progress Report",
                Description = "Standard progress report template for periodic updates on ongoing work, milestones, and next steps.",
                ReportType = Models.ReportType.Detailed,
                IsDefault = true,
                IsActive = true,
                CreatedById = createdById,
                BodyTemplate = "<h2>Executive Summary</h2>\n<p>[Brief overview of progress during this reporting period]</p>\n\n<h2>Accomplishments</h2>\n<ul>\n<li>[Key accomplishment 1]</li>\n<li>[Key accomplishment 2]</li>\n</ul>\n\n<h2>Challenges &amp; Risks</h2>\n<p>[Description of any challenges encountered or risks identified]</p>\n\n<h2>Next Steps</h2>\n<ul>\n<li>[Planned activity 1]</li>\n<li>[Planned activity 2]</li>\n</ul>\n\n<h2>Timeline</h2>\n<p>[Status of milestones and deadlines]</p>",
                IncludeSuggestedAction = true,
                IncludeNeededResources = true,
                IncludeNeededSupport = true,
                IncludeSpecialRemarks = false,
                RequireSuggestedAction = false,
                RequireNeededResources = false,
                RequireNeededSupport = false,
                RequireSpecialRemarks = false
            },
            new()
            {
                Name = "Incident Report",
                Description = "Template for reporting incidents, issues, or problems that require attention and resolution.",
                ReportType = Models.ReportType.Detailed,
                IsDefault = true,
                IsActive = true,
                CreatedById = createdById,
                BodyTemplate = "<h2>Incident Description</h2>\n<p>[What happened, when, and where]</p>\n\n<h2>Impact Assessment</h2>\n<p>[Who and what was affected, severity level]</p>\n\n<h2>Root Cause Analysis</h2>\n<p>[Identified or suspected causes]</p>\n\n<h2>Immediate Actions Taken</h2>\n<ul>\n<li>[Action 1]</li>\n<li>[Action 2]</li>\n</ul>\n\n<h2>Corrective Measures</h2>\n<p>[Proposed corrective and preventive actions]</p>",
                IncludeSuggestedAction = true,
                IncludeNeededResources = true,
                IncludeNeededSupport = true,
                IncludeSpecialRemarks = true,
                RequireSuggestedAction = true,
                RequireNeededResources = false,
                RequireNeededSupport = false,
                RequireSpecialRemarks = false
            },
            new()
            {
                Name = "Decision Request",
                Description = "Template for requesting decisions or approvals from higher authority. Includes options analysis and recommendation.",
                ReportType = Models.ReportType.Detailed,
                IsDefault = true,
                IsActive = true,
                CreatedById = createdById,
                BodyTemplate = "<h2>Background</h2>\n<p>[Context and background information for the decision]</p>\n\n<h2>Options Analysis</h2>\n<h3>Option A: [Name]</h3>\n<p>Pros: [advantages]</p>\n<p>Cons: [disadvantages]</p>\n<p>Cost/Impact: [estimate]</p>\n\n<h3>Option B: [Name]</h3>\n<p>Pros: [advantages]</p>\n<p>Cons: [disadvantages]</p>\n<p>Cost/Impact: [estimate]</p>\n\n<h2>Recommendation</h2>\n<p>[Recommended option with justification]</p>\n\n<h2>Timeline for Decision</h2>\n<p>[When the decision is needed and why]</p>",
                IncludeSuggestedAction = true,
                IncludeNeededResources = true,
                IncludeNeededSupport = false,
                IncludeSpecialRemarks = true,
                RequireSuggestedAction = true,
                RequireNeededResources = false,
                RequireNeededSupport = false,
                RequireSpecialRemarks = false
            },
            new()
            {
                Name = "Status Update",
                Description = "Brief status update template for quick, regular updates on project or task status.",
                ReportType = Models.ReportType.Summary,
                IsDefault = true,
                IsActive = true,
                CreatedById = createdById,
                BodyTemplate = "<h2>Current Status</h2>\n<p><strong>Overall Status:</strong> [On Track / At Risk / Delayed]</p>\n\n<h2>Key Updates</h2>\n<ul>\n<li>[Update 1]</li>\n<li>[Update 2]</li>\n<li>[Update 3]</li>\n</ul>\n\n<h2>Blockers</h2>\n<p>[Any blockers or issues requiring escalation, or \"None\"]</p>\n\n<h2>Upcoming Milestones</h2>\n<p>[Next key milestones and expected dates]</p>",
                IncludeSuggestedAction = false,
                IncludeNeededResources = false,
                IncludeNeededSupport = true,
                IncludeSpecialRemarks = false,
                RequireSuggestedAction = false,
                RequireNeededResources = false,
                RequireNeededSupport = false,
                RequireSpecialRemarks = false
            },
            new()
            {
                Name = "Meeting Preparation Brief",
                Description = "Template for preparing briefing documents ahead of meetings. Summarizes topics, context, and expected outcomes.",
                ReportType = Models.ReportType.Summary,
                IsDefault = true,
                IsActive = true,
                CreatedById = createdById,
                BodyTemplate = "<h2>Meeting Context</h2>\n<p>[Purpose and context of the upcoming meeting]</p>\n\n<h2>Key Topics</h2>\n<ol>\n<li><strong>[Topic 1]:</strong> [Brief description and context]</li>\n<li><strong>[Topic 2]:</strong> [Brief description and context]</li>\n</ol>\n\n<h2>Background Materials</h2>\n<p>[References to relevant reports, directives, or previous meeting minutes]</p>\n\n<h2>Expected Outcomes</h2>\n<ul>\n<li>[Expected decision or outcome 1]</li>\n<li>[Expected decision or outcome 2]</li>\n</ul>\n\n<h2>Preparation Notes</h2>\n<p>[Items participants should review or prepare before the meeting]</p>",
                IncludeSuggestedAction = true,
                IncludeNeededResources = false,
                IncludeNeededSupport = false,
                IncludeSpecialRemarks = true,
                RequireSuggestedAction = false,
                RequireNeededResources = false,
                RequireNeededSupport = false,
                RequireSpecialRemarks = false
            }
        };

        _context.ReportTemplates.AddRange(defaults);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} default report templates", defaults.Count);
    }
}
