using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Search;

[Authorize]
public class IndexModel : PageModel
{
    private readonly SearchService _searchService;
    private readonly AuditService _auditService;
    private readonly ApplicationDbContext _context;

    public IndexModel(SearchService searchService, AuditService auditService, ApplicationDbContext context)
    {
        _searchService = searchService;
        _auditService = auditService;
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public string? Keywords { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ContentType { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CommitteeId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = "date_desc";

    public SearchResults? Results { get; set; }
    public List<SelectListItem> Committees { get; set; } = new();
    public bool HasSearched { get; set; }

    public async Task OnGetAsync()
    {
        await LoadCommitteesAsync();

        if (!string.IsNullOrWhiteSpace(Keywords) || ContentType != null || CommitteeId.HasValue ||
            !string.IsNullOrEmpty(Status) || FromDate.HasValue || ToDate.HasValue)
        {
            HasSearched = true;
            var query = new SearchQuery
            {
                Keywords = Keywords,
                ContentType = ContentType,
                CommitteeId = CommitteeId,
                Status = Status,
                FromDate = FromDate,
                ToDate = ToDate,
                SortBy = SortBy
            };

            var userId = GetUserId();
            Results = await _searchService.SearchAsync(query, userId);

            // Log search
            await _auditService.LogAsync(
                Models.AuditActionType.SearchPerformed,
                "Search",
                userId: userId,
                userName: User.Identity?.Name,
                details: $"Keywords: {Keywords}, Type: {ContentType ?? "All"}, Results: {Results.TotalCount}");
        }
    }

    private async Task LoadCommitteesAsync()
    {
        Committees = await _context.Committees
            .Where(c => c.IsActive)
            .OrderBy(c => c.HierarchyLevel).ThenBy(c => c.Name)
            .Select(c => new SelectListItem($"[{c.HierarchyLevel}] {c.Name}", c.Id.ToString()))
            .ToListAsync();
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
}
