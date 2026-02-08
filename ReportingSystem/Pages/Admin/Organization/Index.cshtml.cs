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
