using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.Users;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public IndexModel(ApplicationDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    public List<User> Users { get; set; } = new();
    public Dictionary<int, List<CommitteeMembershipInfo>> UserCommittees { get; set; } = new();
    public bool IsDevelopment => _env.IsDevelopment();

    public async Task OnGetAsync()
    {
        Users = await _context.Users
            .OrderBy(u => u.Name)
            .ToListAsync();

        var allCommittees = await _context.Committees.ToListAsync();
        var lookup = allCommittees.ToDictionary(c => c.Id);

        var memberships = await _context.CommitteeMemberships
            .Include(m => m.Committee)
            .Where(m => m.EffectiveTo == null)
            .ToListAsync();

        UserCommittees = memberships
            .GroupBy(m => m.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(m => new CommitteeMembershipInfo
                {
                    CommitteeId = m.CommitteeId,
                    CommitteeName = m.Committee.Name,
                    Role = m.Role,
                    HierarchyPath = BuildHierarchyPath(m.Committee, lookup)
                }).OrderBy(x => x.HierarchyPath).ToList()
            );
    }

    private static string BuildHierarchyPath(Committee committee, Dictionary<int, Committee> lookup)
    {
        var parts = new List<string>();
        var current = committee;
        while (current != null)
        {
            parts.Insert(0, current.Name);
            if (current.ParentCommitteeId.HasValue && lookup.TryGetValue(current.ParentCommitteeId.Value, out var parent)
                && parent.HierarchyLevel >= HierarchyLevel.Directors)
            {
                current = parent;
            }
            else
            {
                break;
            }
        }
        return string.Join(" > ", parts);
    }
}
