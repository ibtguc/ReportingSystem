using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Services;

public class SearchService
{
    private readonly ApplicationDbContext _context;
    private readonly ConfidentialityService _confidentialityService;

    public SearchService(ApplicationDbContext context, ConfidentialityService confidentialityService)
    {
        _context = context;
        _confidentialityService = confidentialityService;
    }

    public async Task<SearchResults> SearchAsync(SearchQuery query, int userId)
    {
        var results = new SearchResults { Query = query };

        var tasks = new List<Task>();

        if (query.ContentType == null || query.ContentType == "Report")
            tasks.Add(SearchReportsAsync(query, userId, results));
        if (query.ContentType == null || query.ContentType == "Directive")
            tasks.Add(SearchDirectivesAsync(query, userId, results));
        if (query.ContentType == null || query.ContentType == "Meeting")
            tasks.Add(SearchMeetingsAsync(query, userId, results));
        if (query.ContentType == null || query.ContentType == "ActionItem")
            tasks.Add(SearchActionItemsAsync(query, userId, results));

        await Task.WhenAll(tasks);

        // Sort combined results
        results.Items = query.SortBy switch
        {
            "date_asc" => results.Items.OrderBy(i => i.Date).ToList(),
            "title" => results.Items.OrderBy(i => i.Title).ToList(),
            _ => results.Items.OrderByDescending(i => i.Date).ToList() // default: date_desc
        };

        results.TotalCount = results.Items.Count;
        return results;
    }

    private async Task SearchReportsAsync(SearchQuery query, int userId, SearchResults results)
    {
        var q = _context.Reports
            .Include(r => r.Author)
            .Include(r => r.Committee)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keywords))
        {
            var kw = query.Keywords.ToLower();
            q = q.Where(r =>
                r.Title.ToLower().Contains(kw) ||
                (r.BodyContent != null && r.BodyContent.ToLower().Contains(kw)));
        }

        if (query.CommitteeId.HasValue)
            q = q.Where(r => r.CommitteeId == query.CommitteeId.Value);
        if (query.FromDate.HasValue)
            q = q.Where(r => r.CreatedAt >= query.FromDate.Value);
        if (query.ToDate.HasValue)
            q = q.Where(r => r.CreatedAt <= query.ToDate.Value);
        if (!string.IsNullOrEmpty(query.Status))
        {
            if (Enum.TryParse<ReportStatus>(query.Status, out var status))
                q = q.Where(r => r.Status == status);
        }

        var reports = await q.Take(100).ToListAsync();

        // Filter confidential items
        reports = await _confidentialityService.FilterAccessibleReportsAsync(reports, userId);

        lock (results.Items)
        {
            foreach (var r in reports)
            {
                results.Items.Add(new SearchResultItem
                {
                    ItemType = "Report",
                    ItemId = r.Id,
                    Title = r.Title,
                    Snippet = GetSnippet(r.BodyContent, query.Keywords),
                    Author = r.Author?.Name ?? "Unknown",
                    Date = r.CreatedAt,
                    Committee = r.Committee?.Name,
                    Status = r.Status.ToString(),
                    Url = $"/Reports/Details/{r.Id}"
                });
            }
        }
    }

    private async Task SearchDirectivesAsync(SearchQuery query, int userId, SearchResults results)
    {
        var q = _context.Directives
            .Include(d => d.Issuer)
            .Include(d => d.TargetCommittee)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keywords))
        {
            var kw = query.Keywords.ToLower();
            q = q.Where(d =>
                d.Title.ToLower().Contains(kw) ||
                (d.BodyContent != null && d.BodyContent.ToLower().Contains(kw)));
        }

        if (query.CommitteeId.HasValue)
            q = q.Where(d => d.TargetCommitteeId == query.CommitteeId.Value);
        if (query.FromDate.HasValue)
            q = q.Where(d => d.CreatedAt >= query.FromDate.Value);
        if (query.ToDate.HasValue)
            q = q.Where(d => d.CreatedAt <= query.ToDate.Value);
        if (!string.IsNullOrEmpty(query.Status))
        {
            if (Enum.TryParse<DirectiveStatus>(query.Status, out var status))
                q = q.Where(d => d.Status == status);
        }

        var directives = await q.Take(100).ToListAsync();
        directives = await _confidentialityService.FilterAccessibleDirectivesAsync(directives, userId);

        lock (results.Items)
        {
            foreach (var d in directives)
            {
                results.Items.Add(new SearchResultItem
                {
                    ItemType = "Directive",
                    ItemId = d.Id,
                    Title = d.Title,
                    Snippet = GetSnippet(d.BodyContent, query.Keywords),
                    Author = d.Issuer?.Name ?? "Unknown",
                    Date = d.CreatedAt,
                    Committee = d.TargetCommittee?.Name,
                    Status = d.Status.ToString(),
                    Url = $"/Directives/Details/{d.Id}"
                });
            }
        }
    }

    private async Task SearchMeetingsAsync(SearchQuery query, int userId, SearchResults results)
    {
        var q = _context.Meetings
            .Include(m => m.Committee)
            .Include(m => m.Moderator)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keywords))
        {
            var kw = query.Keywords.ToLower();
            q = q.Where(m =>
                m.Title.ToLower().Contains(kw) ||
                (m.Description != null && m.Description.ToLower().Contains(kw)) ||
                (m.MinutesContent != null && m.MinutesContent.ToLower().Contains(kw)));
        }

        if (query.CommitteeId.HasValue)
            q = q.Where(m => m.CommitteeId == query.CommitteeId.Value);
        if (query.FromDate.HasValue)
            q = q.Where(m => m.ScheduledAt >= query.FromDate.Value);
        if (query.ToDate.HasValue)
            q = q.Where(m => m.ScheduledAt <= query.ToDate.Value);
        if (!string.IsNullOrEmpty(query.Status))
        {
            if (Enum.TryParse<MeetingStatus>(query.Status, out var status))
                q = q.Where(m => m.Status == status);
        }

        var meetings = await q.Take(100).ToListAsync();
        meetings = await _confidentialityService.FilterAccessibleMeetingsAsync(meetings, userId);

        lock (results.Items)
        {
            foreach (var m in meetings)
            {
                results.Items.Add(new SearchResultItem
                {
                    ItemType = "Meeting",
                    ItemId = m.Id,
                    Title = m.Title,
                    Snippet = GetSnippet(m.Description ?? m.MinutesContent, query.Keywords),
                    Author = m.Moderator?.Name ?? "Unknown",
                    Date = m.ScheduledAt,
                    Committee = m.Committee?.Name,
                    Status = m.Status.ToString(),
                    Url = $"/Meetings/Details/{m.Id}"
                });
            }
        }
    }

    private async Task SearchActionItemsAsync(SearchQuery query, int userId, SearchResults results)
    {
        var q = _context.ActionItems
            .Include(a => a.AssignedTo)
            .Include(a => a.Meeting)
                .ThenInclude(m => m.Committee)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keywords))
        {
            var kw = query.Keywords.ToLower();
            q = q.Where(a =>
                a.Title.ToLower().Contains(kw) ||
                (a.Description != null && a.Description.ToLower().Contains(kw)));
        }

        if (query.CommitteeId.HasValue)
            q = q.Where(a => a.Meeting.CommitteeId == query.CommitteeId.Value);
        if (query.FromDate.HasValue)
            q = q.Where(a => a.CreatedAt >= query.FromDate.Value);
        if (query.ToDate.HasValue)
            q = q.Where(a => a.CreatedAt <= query.ToDate.Value);
        if (!string.IsNullOrEmpty(query.Status))
        {
            if (Enum.TryParse<ActionItemStatus>(query.Status, out var status))
                q = q.Where(a => a.Status == status);
        }

        var items = await q.Take(100).ToListAsync();

        lock (results.Items)
        {
            foreach (var a in items)
            {
                results.Items.Add(new SearchResultItem
                {
                    ItemType = "ActionItem",
                    ItemId = a.Id,
                    Title = a.Title,
                    Snippet = GetSnippet(a.Description, query.Keywords),
                    Author = a.AssignedTo?.Name ?? "Unknown",
                    Date = a.CreatedAt,
                    Committee = a.Meeting?.Committee?.Name,
                    Status = a.Status.ToString(),
                    Url = $"/Meetings/ActionItems"
                });
            }
        }
    }

    private static string GetSnippet(string? content, string? keywords, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        if (string.IsNullOrWhiteSpace(keywords))
            return content.Length > maxLength ? content[..maxLength] + "..." : content;

        var kw = keywords.ToLower();
        var idx = content.ToLower().IndexOf(kw);
        if (idx < 0)
            return content.Length > maxLength ? content[..maxLength] + "..." : content;

        var start = Math.Max(0, idx - 50);
        var end = Math.Min(content.Length, idx + kw.Length + 150);
        var snippet = content[start..end];

        if (start > 0) snippet = "..." + snippet;
        if (end < content.Length) snippet += "...";

        return snippet;
    }
}

// ── DTOs ──

public class SearchQuery
{
    public string? Keywords { get; set; }
    public string? ContentType { get; set; }
    public int? CommitteeId { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string SortBy { get; set; } = "date_desc";
}

public class SearchResults
{
    public SearchQuery Query { get; set; } = new();
    public List<SearchResultItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
}

public class SearchResultItem
{
    public string ItemType { get; set; } = string.Empty;
    public int ItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? Committee { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
