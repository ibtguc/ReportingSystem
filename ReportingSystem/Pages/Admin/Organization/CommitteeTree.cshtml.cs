using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Admin.Organization;

public class CommitteeTreeModel : PageModel
{
    private readonly OrganizationService _orgService;

    public CommitteeTreeModel(OrganizationService orgService)
    {
        _orgService = orgService;
    }

    public List<Committee> AllCommittees { get; set; } = new();

    /// <summary>
    /// Committees grouped by hierarchy level, ordered top-down (L0 first).
    /// Each level is a horizontal row in the visual tree.
    /// </summary>
    public List<TreeLevel> Levels { get; set; } = new();

    public async Task OnGetAsync()
    {
        AllCommittees = await _orgService.GetHierarchyTreeAsync();
        BuildLevels();
    }

    private void BuildLevels()
    {
        var grouped = AllCommittees
            .GroupBy(c => c.HierarchyLevel)
            .OrderBy(g => g.Key)
            .ToList();

        foreach (var group in grouped)
        {
            Levels.Add(new TreeLevel
            {
                Level = group.Key,
                Committees = group.OrderBy(c => c.Sector ?? "").ThenBy(c => c.Name).ToList()
            });
        }
    }

    public List<Committee> GetChildren(int parentId)
    {
        return AllCommittees.Where(c => c.ParentCommitteeId == parentId).ToList();
    }

    public class TreeLevel
    {
        public HierarchyLevel Level { get; set; }
        public List<Committee> Committees { get; set; } = new();
    }
}
