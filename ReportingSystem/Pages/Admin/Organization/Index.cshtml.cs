using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Admin.Organization;

public class IndexModel : PageModel
{
    private readonly OrganizationService _orgService;

    public IndexModel(OrganizationService orgService)
    {
        _orgService = orgService;
    }

    public List<Committee> AllCommittees { get; set; } = new();
    public List<User> ChairmanOfficeMembers { get; set; } = new();
    public User? Chairman { get; set; }
    public (int committees, int users, int memberships, int shadows) Stats { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Highlight { get; set; }

    public List<int> HighlightAncestorIds { get; set; } = new();

    public async Task OnGetAsync()
    {
        AllCommittees = await _orgService.GetHierarchyTreeAsync();
        Stats = await _orgService.GetOrganizationStatsAsync();

        var allUsers = await _orgService.GetAvailableUsersAsync();
        Chairman = allUsers.FirstOrDefault(u => u.SystemRole == SystemRole.Chairman);
        ChairmanOfficeMembers = allUsers
            .Where(u => u.SystemRole == SystemRole.ChairmanOffice)
            .OrderBy(u => u.ChairmanOfficeRank)
            .ToList();

        if (Highlight.HasValue)
        {
            ComputeAncestorChain(Highlight.Value);
        }
    }

    private void ComputeAncestorChain(int committeeId)
    {
        var lookup = AllCommittees.ToDictionary(c => c.Id);
        if (!lookup.TryGetValue(committeeId, out var current)) return;

        while (current.ParentCommitteeId.HasValue)
        {
            HighlightAncestorIds.Add(current.ParentCommitteeId.Value);
            if (!lookup.TryGetValue(current.ParentCommitteeId.Value, out current)) break;
        }
    }

    public List<Committee> GetRootCommittees()
    {
        return AllCommittees.Where(c => c.ParentCommitteeId == null).ToList();
    }

    public List<Committee> GetChildren(int parentId)
    {
        return AllCommittees.Where(c => c.ParentCommitteeId == parentId).ToList();
    }
}
