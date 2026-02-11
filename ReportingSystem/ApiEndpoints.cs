using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem;

public static class ApiEndpoints
{
    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api").RequireAuthorization();

        // ── Reports ──
        api.MapGet("/reports", async (ApplicationDbContext db, ReportService reportService, HttpContext httpContext, int? committeeId, string? status, int page, int pageSize) =>
        {
            var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Results.Forbid();

            var role = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
            var isGlobal = role is "Chairman" or "ChairmanOffice" or "Admin";

            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

            var query = db.Reports
                .Include(r => r.Author)
                .Include(r => r.Committee)
                .Where(r => !r.IsConfidential && r.Status != ReportStatus.Draft)
                .AsQueryable();

            // Visibility filtering
            if (!isGlobal)
            {
                var visibleCommitteeIds = await reportService.GetVisibleCommitteeIdsAsync(userId);
                query = query.Where(r => r.AuthorId == userId || visibleCommitteeIds.Contains(r.CommitteeId));
            }

            if (committeeId.HasValue)
                query = query.Where(r => r.CommitteeId == committeeId.Value);
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ReportStatus>(status, true, out var rs))
                query = query.Where(r => r.Status == rs);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(r => new
                {
                    r.Id, r.Title, Status = r.Status.ToString(), r.ReportType,
                    Author = r.Author.Name, Committee = r.Committee.Name,
                    r.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, items });
        });

        api.MapGet("/reports/{id:int}", async (ApplicationDbContext db, ReportService reportService, HttpContext httpContext, int id) =>
        {
            var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Results.Forbid();

            var report = await db.Reports
                .Include(r => r.Author).Include(r => r.Committee)
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsConfidential);
            if (report == null) return Results.NotFound();

            if (!await reportService.CanUserViewReportAsync(userId, report))
                return Results.NotFound();

            return Results.Ok(new
            {
                report.Id, report.Title, Status = report.Status.ToString(),
                report.ReportType, Author = report.Author.Name,
                Committee = report.Committee.Name, report.BodyContent,
                report.CreatedAt, report.UpdatedAt
            });
        });

        // ── Directives ──
        api.MapGet("/directives", async (ApplicationDbContext db, int? committeeId, string? status, int page, int pageSize) =>
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

            var query = db.Directives
                .Include(d => d.Issuer)
                .Include(d => d.TargetCommittee)
                .Where(d => !d.IsConfidential)
                .AsQueryable();

            if (committeeId.HasValue)
                query = query.Where(d => d.TargetCommitteeId == committeeId.Value);
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<DirectiveStatus>(status, true, out var ds))
                query = query.Where(d => d.Status == ds);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(d => new
                {
                    d.Id, d.Title, Status = d.Status.ToString(),
                    d.DirectiveType, d.Priority,
                    Issuer = d.Issuer.Name, TargetCommittee = d.TargetCommittee.Name,
                    d.Deadline, d.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, items });
        });

        // ── Meetings ──
        api.MapGet("/meetings", async (ApplicationDbContext db, int? committeeId, string? status, int page, int pageSize) =>
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

            var query = db.Meetings
                .Include(m => m.Committee)
                .Include(m => m.Moderator)
                .Where(m => !m.IsConfidential)
                .AsQueryable();

            if (committeeId.HasValue)
                query = query.Where(m => m.CommitteeId == committeeId.Value);
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<MeetingStatus>(status, true, out var ms))
                query = query.Where(m => m.Status == ms);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(m => m.ScheduledAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(m => new
                {
                    m.Id, m.Title, Status = m.Status.ToString(),
                    Committee = m.Committee.Name, Moderator = m.Moderator.Name,
                    m.ScheduledAt, m.Location
                })
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, items });
        });

        // ── Committees ──
        api.MapGet("/committees", async (ApplicationDbContext db) =>
        {
            var items = await db.Committees
                .Where(c => c.IsActive)
                .OrderBy(c => c.HierarchyLevel).ThenBy(c => c.Name)
                .Select(c => new
                {
                    c.Id, c.Name, HierarchyLevel = c.HierarchyLevel.ToString(),
                    c.Sector, c.ParentCommitteeId
                })
                .ToListAsync();

            return Results.Ok(items);
        });

        // ── Search ──
        api.MapGet("/search", async (SearchService searchService, HttpContext httpContext, string q, int page, int pageSize) =>
        {
            if (string.IsNullOrWhiteSpace(q))
                return Results.BadRequest(new { error = "Query parameter 'q' is required." });

            var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Results.Forbid();

            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 50);

            var searchQuery = new SearchQuery { Keywords = q };
            var results = await searchService.SearchAsync(searchQuery, userId);
            var paged = results.Items.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return Results.Ok(new { total = results.TotalCount, page, pageSize, items = paged });
        });

        // ── Analytics ──
        api.MapGet("/analytics/overview", async (AnalyticsService analytics) =>
            Results.Ok(await analytics.GetOrganizationAnalyticsAsync()));

        api.MapGet("/analytics/trends", async (AnalyticsService analytics) =>
            Results.Ok(await analytics.GetMonthlyTrendsAsync()));

        api.MapGet("/analytics/committees", async (AnalyticsService analytics) =>
            Results.Ok(await analytics.GetCommitteeMetricsAsync()));

        api.MapGet("/analytics/compliance", async (AnalyticsService analytics) =>
            Results.Ok(await analytics.GetComplianceMetricsAsync()));

        // ── Knowledge Base ──
        api.MapGet("/knowledge/articles", async (KnowledgeBaseService kb, int? categoryId, string? search, int page, int pageSize) =>
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 50);

            var total = await kb.GetArticleCountAsync(categoryId, search);
            var items = await kb.GetArticlesAsync(categoryId, search, page, pageSize);

            return Results.Ok(new
            {
                total, page, pageSize,
                items = items.Select(a => new
                {
                    a.Id, a.Title, a.Summary, SourceType = a.SourceType.ToString(),
                    Category = a.Category?.Name, a.ViewCount, a.CreatedAt
                })
            });
        });

        api.MapGet("/knowledge/categories", async (KnowledgeBaseService kb) =>
        {
            var cats = await kb.GetTopLevelCategoriesAsync();
            return Results.Ok(cats.Select(c => new
            {
                c.Id, c.Name, c.Description, c.Icon,
                SubCategories = c.SubCategories.Select(s => new { s.Id, s.Name, s.Description })
            }));
        });

        return app;
    }
}
