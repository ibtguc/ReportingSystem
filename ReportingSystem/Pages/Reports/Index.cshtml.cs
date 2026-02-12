using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Reports;

// Confidentiality filtering applied via ConfidentialityService

[Authorize]
public class IndexModel : PageModel
{
    private readonly ReportService _reportService;
    private readonly ConfidentialityService _confidentialityService;

    public IndexModel(ReportService reportService, ConfidentialityService confidentialityService)
    {
        _reportService = reportService;
        _confidentialityService = confidentialityService;
    }

    public List<Report> Reports { get; set; } = new();
    public List<SelectListItem> CommitteeOptions { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? CommitteeId { get; set; }

    [BindProperty(SupportsGet = true)]
    public ReportStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public ReportType? ReportType { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool ShowMine { get; set; }

    public async Task OnGetAsync()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

        var committees = await _reportService.GetVisibleCommitteesAsync(userId);
        CommitteeOptions = BuildHierarchicalOptions(committees);

        int? authorFilter = ShowMine ? userId : null;

        // When a committee is selected, also include its sub-committees
        List<int>? committeeIds = null;
        if (CommitteeId.HasValue)
        {
            var descendants = committees
                .Where(c => IsDescendantOf(c, CommitteeId.Value, committees))
                .Select(c => c.Id)
                .ToList();
            committeeIds = new List<int> { CommitteeId.Value };
            committeeIds.AddRange(descendants);
        }

        Reports = await _reportService.GetReportsAsync(
            userId: userId,
            authorId: authorFilter,
            status: Status,
            reportType: ReportType,
            committeeIds: committeeIds);

        // Filter out confidential items the user cannot access
        Reports = await _confidentialityService.FilterAccessibleReportsAsync(Reports, userId);
    }

    /// <summary>
    /// Builds a hierarchical list of SelectListItems with indentation based on committee level.
    /// </summary>
    private static List<SelectListItem> BuildHierarchicalOptions(List<Committee> committees)
    {
        var result = new List<SelectListItem>();
        var lookup = committees.ToDictionary(c => c.Id);

        // Find root committees (no parent, or parent not in the visible list)
        var roots = committees
            .Where(c => c.ParentCommitteeId == null || !lookup.ContainsKey(c.ParentCommitteeId.Value))
            .OrderBy(c => c.HierarchyLevel).ThenBy(c => c.Name)
            .ToList();

        foreach (var root in roots)
            AddCommitteeAndChildren(root, committees, lookup, result, 0);

        return result;
    }

    private static void AddCommitteeAndChildren(
        Committee committee, List<Committee> all, Dictionary<int, Committee> lookup,
        List<SelectListItem> result, int depth)
    {
        var indent = depth > 0 ? new string('\u00A0', depth * 4) + "└ " : "";
        result.Add(new SelectListItem($"{indent}{committee.Name}", committee.Id.ToString()));

        var children = all
            .Where(c => c.ParentCommitteeId == committee.Id)
            .OrderBy(c => c.Name)
            .ToList();

        foreach (var child in children)
            AddCommitteeAndChildren(child, all, lookup, result, depth + 1);
    }

    /// <summary>
    /// Checks if a committee is a descendant of the given ancestor ID.
    /// </summary>
    private static bool IsDescendantOf(Committee committee, int ancestorId, List<Committee> all)
    {
        var lookup = all.ToDictionary(c => c.Id);
        var current = committee;
        while (current.ParentCommitteeId.HasValue)
        {
            if (current.ParentCommitteeId.Value == ancestorId)
                return true;
            if (!lookup.TryGetValue(current.ParentCommitteeId.Value, out current))
                break;
        }
        return false;
    }
}
