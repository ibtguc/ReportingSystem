using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Services;

public class KnowledgeBaseService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<KnowledgeBaseService> _logger;

    public KnowledgeBaseService(ApplicationDbContext context, ILogger<KnowledgeBaseService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── Categories ──────────────────────────────────────────

    public async Task<List<KnowledgeCategory>> GetCategoriesAsync(bool includeInactive = false)
    {
        var query = _context.KnowledgeCategories
            .Include(c => c.SubCategories)
            .AsQueryable();

        if (!includeInactive)
            query = query.Where(c => c.IsActive);

        return await query.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync();
    }

    public async Task<List<KnowledgeCategory>> GetTopLevelCategoriesAsync()
    {
        return await _context.KnowledgeCategories
            .Include(c => c.SubCategories.Where(s => s.IsActive))
            .Where(c => c.IsActive && c.ParentCategoryId == null)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<KnowledgeCategory?> GetCategoryByIdAsync(int id)
    {
        return await _context.KnowledgeCategories
            .Include(c => c.SubCategories.Where(s => s.IsActive))
            .Include(c => c.ParentCategory)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<KnowledgeCategory> CreateCategoryAsync(KnowledgeCategory category)
    {
        _context.KnowledgeCategories.Add(category);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created knowledge category {Id}: {Name}", category.Id, category.Name);
        return category;
    }

    public async Task<KnowledgeCategory?> UpdateCategoryAsync(KnowledgeCategory category)
    {
        var existing = await _context.KnowledgeCategories.FindAsync(category.Id);
        if (existing == null) return null;

        existing.Name = category.Name;
        existing.Description = category.Description;
        existing.Icon = category.Icon;
        existing.ParentCategoryId = category.ParentCategoryId;
        existing.SortOrder = category.SortOrder;
        existing.IsActive = category.IsActive;

        await _context.SaveChangesAsync();
        return existing;
    }

    // ── Articles ──────────────────────────────────────────

    public async Task<List<KnowledgeArticle>> GetArticlesAsync(
        int? categoryId = null, string? search = null, int page = 1, int pageSize = 20)
    {
        var query = _context.KnowledgeArticles
            .Include(a => a.Category)
            .Include(a => a.Committee)
            .Include(a => a.CreatedBy)
            .Where(a => a.IsPublished)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(a => a.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var terms = search.ToLower();
            query = query.Where(a =>
                a.Title.ToLower().Contains(terms) ||
                (a.Summary != null && a.Summary.ToLower().Contains(terms)) ||
                a.Content.ToLower().Contains(terms) ||
                (a.Tags != null && a.Tags.ToLower().Contains(terms)));
        }

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetArticleCountAsync(int? categoryId = null, string? search = null)
    {
        var query = _context.KnowledgeArticles.Where(a => a.IsPublished).AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(a => a.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var terms = search.ToLower();
            query = query.Where(a =>
                a.Title.ToLower().Contains(terms) ||
                (a.Summary != null && a.Summary.ToLower().Contains(terms)) ||
                a.Content.ToLower().Contains(terms) ||
                (a.Tags != null && a.Tags.ToLower().Contains(terms)));
        }

        return await query.CountAsync();
    }

    public async Task<KnowledgeArticle?> GetArticleByIdAsync(int id)
    {
        return await _context.KnowledgeArticles
            .Include(a => a.Category)
            .Include(a => a.Committee)
            .Include(a => a.CreatedBy)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task IncrementViewCountAsync(int articleId)
    {
        var article = await _context.KnowledgeArticles.FindAsync(articleId);
        if (article != null)
        {
            article.ViewCount++;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<KnowledgeArticle> CreateArticleAsync(KnowledgeArticle article)
    {
        _context.KnowledgeArticles.Add(article);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created knowledge article {Id}: {Title}", article.Id, article.Title);
        return article;
    }

    public async Task<KnowledgeArticle?> UpdateArticleAsync(KnowledgeArticle article)
    {
        var existing = await _context.KnowledgeArticles.FindAsync(article.Id);
        if (existing == null) return null;

        existing.Title = article.Title;
        existing.Summary = article.Summary;
        existing.Content = article.Content;
        existing.CategoryId = article.CategoryId;
        existing.Tags = article.Tags;
        existing.IsPublished = article.IsPublished;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteArticleAsync(int id)
    {
        var article = await _context.KnowledgeArticles.FindAsync(id);
        if (article == null) return false;

        _context.KnowledgeArticles.Remove(article);
        await _context.SaveChangesAsync();
        return true;
    }

    // ── Auto-Index: Create articles from approved content ──

    /// <summary>
    /// Auto-index an approved, non-confidential report into the knowledge base.
    /// </summary>
    public async Task<KnowledgeArticle?> IndexReportAsync(int reportId, int createdById)
    {
        var report = await _context.Reports
            .Include(r => r.Committee)
            .Include(r => r.Author)
            .FirstOrDefaultAsync(r => r.Id == reportId);

        if (report == null || report.Status != ReportStatus.Approved || report.IsConfidential)
            return null;

        // Check if already indexed
        if (await _context.KnowledgeArticles.AnyAsync(a =>
            a.SourceType == KnowledgeSourceType.Report && a.SourceItemId == reportId))
            return null;

        var category = await GetOrCreateCategoryForCommitteeAsync(report.Committee);

        var article = new KnowledgeArticle
        {
            Title = report.Title,
            Summary = TruncateText(StripHtml(report.BodyContent), 300),
            Content = report.BodyContent,
            CategoryId = category.Id,
            SourceType = KnowledgeSourceType.Report,
            SourceItemId = reportId,
            CommitteeId = report.CommitteeId,
            Tags = $"{report.ReportType},{report.Committee?.Name}",
            CreatedById = createdById,
            IsPublished = true
        };

        return await CreateArticleAsync(article);
    }

    /// <summary>
    /// Auto-index a closed, non-confidential directive into the knowledge base.
    /// </summary>
    public async Task<KnowledgeArticle?> IndexDirectiveAsync(int directiveId, int createdById)
    {
        var directive = await _context.Directives
            .Include(d => d.TargetCommittee)
            .Include(d => d.Issuer)
            .FirstOrDefaultAsync(d => d.Id == directiveId);

        if (directive == null || directive.Status != DirectiveStatus.Closed || directive.IsConfidential)
            return null;

        if (await _context.KnowledgeArticles.AnyAsync(a =>
            a.SourceType == KnowledgeSourceType.Directive && a.SourceItemId == directiveId))
            return null;

        var category = await GetOrCreateCategoryForCommitteeAsync(directive.TargetCommittee);

        var article = new KnowledgeArticle
        {
            Title = $"Directive: {directive.Title}",
            Summary = TruncateText(StripHtml(directive.BodyContent), 300),
            Content = directive.BodyContent,
            CategoryId = category.Id,
            SourceType = KnowledgeSourceType.Directive,
            SourceItemId = directiveId,
            CommitteeId = directive.TargetCommitteeId,
            Tags = $"{directive.DirectiveType},{directive.Priority},{directive.TargetCommittee?.Name}",
            CreatedById = createdById,
            IsPublished = true
        };

        return await CreateArticleAsync(article);
    }

    /// <summary>
    /// Auto-index a meeting decision from a finalized meeting.
    /// </summary>
    public async Task<KnowledgeArticle?> IndexMeetingDecisionAsync(int decisionId, int createdById)
    {
        var decision = await _context.MeetingDecisions
            .Include(d => d.Meeting).ThenInclude(m => m.Committee)
            .FirstOrDefaultAsync(d => d.Id == decisionId);

        if (decision == null || decision.Meeting.Status != MeetingStatus.Finalized)
            return null;

        if (await _context.KnowledgeArticles.AnyAsync(a =>
            a.SourceType == KnowledgeSourceType.MeetingDecision && a.SourceItemId == decisionId))
            return null;

        var category = await GetOrCreateCategoryForCommitteeAsync(decision.Meeting.Committee);

        var article = new KnowledgeArticle
        {
            Title = $"Decision: {TruncateText(decision.DecisionText, 200)}",
            Summary = decision.DecisionText,
            Content = $"<h2>Meeting Decision</h2><p><strong>Meeting:</strong> {decision.Meeting.Title}</p><p><strong>Type:</strong> {decision.DecisionType}</p><p>{decision.DecisionText}</p>",
            CategoryId = category.Id,
            SourceType = KnowledgeSourceType.MeetingDecision,
            SourceItemId = decisionId,
            CommitteeId = decision.Meeting.CommitteeId,
            Tags = $"Decision,{decision.DecisionType},{decision.Meeting.Committee?.Name}",
            CreatedById = createdById,
            IsPublished = true
        };

        return await CreateArticleAsync(article);
    }

    /// <summary>
    /// Bulk-index all eligible approved content that hasn't been indexed yet.
    /// </summary>
    public async Task<int> BulkIndexAsync(int createdById)
    {
        var count = 0;

        // Index approved non-confidential reports
        var reports = await _context.Reports
            .Where(r => r.Status == ReportStatus.Approved && !r.IsConfidential)
            .Select(r => r.Id)
            .ToListAsync();

        foreach (var id in reports)
        {
            var result = await IndexReportAsync(id, createdById);
            if (result != null) count++;
        }

        // Index closed non-confidential directives
        var directives = await _context.Directives
            .Where(d => d.Status == DirectiveStatus.Closed && !d.IsConfidential)
            .Select(d => d.Id)
            .ToListAsync();

        foreach (var id in directives)
        {
            var result = await IndexDirectiveAsync(id, createdById);
            if (result != null) count++;
        }

        // Index decisions from finalized meetings
        var decisions = await _context.MeetingDecisions
            .Include(d => d.Meeting)
            .Where(d => d.Meeting.Status == MeetingStatus.Finalized)
            .Select(d => d.Id)
            .ToListAsync();

        foreach (var id in decisions)
        {
            var result = await IndexMeetingDecisionAsync(id, createdById);
            if (result != null) count++;
        }

        _logger.LogInformation("Bulk indexed {Count} knowledge articles", count);
        return count;
    }

    // ── Stats ──

    public async Task<KnowledgeBaseStats> GetStatsAsync()
    {
        return new KnowledgeBaseStats
        {
            TotalArticles = await _context.KnowledgeArticles.CountAsync(a => a.IsPublished),
            TotalCategories = await _context.KnowledgeCategories.CountAsync(c => c.IsActive),
            ReportArticles = await _context.KnowledgeArticles.CountAsync(a => a.SourceType == KnowledgeSourceType.Report && a.IsPublished),
            DirectiveArticles = await _context.KnowledgeArticles.CountAsync(a => a.SourceType == KnowledgeSourceType.Directive && a.IsPublished),
            DecisionArticles = await _context.KnowledgeArticles.CountAsync(a => a.SourceType == KnowledgeSourceType.MeetingDecision && a.IsPublished),
            ManualArticles = await _context.KnowledgeArticles.CountAsync(a => a.SourceType == KnowledgeSourceType.Manual && a.IsPublished),
            TotalViews = await _context.KnowledgeArticles.SumAsync(a => a.ViewCount),
            MostViewedArticles = await _context.KnowledgeArticles
                .Include(a => a.Category)
                .Where(a => a.IsPublished)
                .OrderByDescending(a => a.ViewCount)
                .Take(5)
                .ToListAsync(),
            RecentArticles = await _context.KnowledgeArticles
                .Include(a => a.Category)
                .Include(a => a.CreatedBy)
                .Where(a => a.IsPublished)
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .ToListAsync()
        };
    }

    // ── Seed default categories ──

    public async Task SeedDefaultCategoriesAsync()
    {
        if (await _context.KnowledgeCategories.AnyAsync())
            return;

        var categories = new List<KnowledgeCategory>
        {
            new() { Name = "Reports & Findings", Description = "Approved reports and their findings", Icon = "bi-file-earmark-text", SortOrder = 1 },
            new() { Name = "Directives & Policies", Description = "Closed directives and policy decisions", Icon = "bi-megaphone", SortOrder = 2 },
            new() { Name = "Meeting Decisions", Description = "Formal decisions from finalized meetings", Icon = "bi-calendar-check", SortOrder = 3 },
            new() { Name = "Procedures & Guidelines", Description = "Organizational procedures and guidelines", Icon = "bi-book", SortOrder = 4 },
            new() { Name = "Best Practices", Description = "Documented best practices and lessons learned", Icon = "bi-lightbulb", SortOrder = 5 }
        };

        _context.KnowledgeCategories.AddRange(categories);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} default knowledge categories", categories.Count);
    }

    // ── Helpers ──

    private async Task<KnowledgeCategory> GetOrCreateCategoryForCommitteeAsync(Committee? committee)
    {
        if (committee == null)
        {
            return await _context.KnowledgeCategories.FirstAsync(c => c.Name == "Reports & Findings");
        }

        // Try to find existing category named after the committee
        var existing = await _context.KnowledgeCategories
            .FirstOrDefaultAsync(c => c.Name == committee.Name && c.IsActive);

        if (existing != null) return existing;

        // Determine parent category based on source type
        var parentName = "Reports & Findings";
        var parent = await _context.KnowledgeCategories.FirstOrDefaultAsync(c => c.Name == parentName);

        var category = new KnowledgeCategory
        {
            Name = committee.Name,
            Description = $"Knowledge from {committee.Name} ({committee.HierarchyLevel})",
            Icon = "bi-diagram-3",
            ParentCategoryId = parent?.Id,
            SortOrder = 10
        };

        _context.KnowledgeCategories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ").Trim();
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text ?? string.Empty;
        return text[..maxLength] + "...";
    }
}

public class KnowledgeBaseStats
{
    public int TotalArticles { get; set; }
    public int TotalCategories { get; set; }
    public int ReportArticles { get; set; }
    public int DirectiveArticles { get; set; }
    public int DecisionArticles { get; set; }
    public int ManualArticles { get; set; }
    public int TotalViews { get; set; }
    public List<KnowledgeArticle> MostViewedArticles { get; set; } = new();
    public List<KnowledgeArticle> RecentArticles { get; set; } = new();
}
