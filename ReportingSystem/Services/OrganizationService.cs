using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Services;

public class OrganizationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrganizationService> _logger;

    public OrganizationService(ApplicationDbContext context, ILogger<OrganizationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── Committee queries ──

    public async Task<List<Committee>> GetAllCommitteesAsync()
    {
        return await _context.Committees
            .Include(c => c.ParentCommittee)
            .Include(c => c.Memberships).ThenInclude(m => m.User)
            .OrderBy(c => c.HierarchyLevel)
            .ThenBy(c => c.Sector)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Committee?> GetCommitteeByIdAsync(int id)
    {
        return await _context.Committees
            .Include(c => c.ParentCommittee)
            .Include(c => c.SubCommittees)
            .Include(c => c.Memberships).ThenInclude(m => m.User)
            .Include(c => c.ShadowAssignments).ThenInclude(s => s.PrincipalUser)
            .Include(c => c.ShadowAssignments).ThenInclude(s => s.ShadowUser)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Committee>> GetCommitteesByLevelAsync(HierarchyLevel level)
    {
        return await _context.Committees
            .Include(c => c.ParentCommittee)
            .Include(c => c.Memberships).ThenInclude(m => m.User)
            .Where(c => c.HierarchyLevel == level && c.IsActive)
            .OrderBy(c => c.Sector)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<Committee>> GetSubCommitteesAsync(int parentId)
    {
        return await _context.Committees
            .Include(c => c.Memberships).ThenInclude(m => m.User)
            .Where(c => c.ParentCommitteeId == parentId && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Gets the full hierarchy tree starting from L0 for the org tree view.
    /// </summary>
    public async Task<List<Committee>> GetHierarchyTreeAsync()
    {
        return await _context.Committees
            .Include(c => c.Memberships).ThenInclude(m => m.User)
            .Where(c => c.IsActive)
            .OrderBy(c => c.HierarchyLevel)
            .ThenBy(c => c.Sector)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<Committee>> GetPotentialParentsAsync(int? excludeId = null)
    {
        var query = _context.Committees.Where(c => c.IsActive);
        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);
        return await query
            .OrderBy(c => c.HierarchyLevel)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task CreateCommitteeAsync(Committee committee)
    {
        committee.CreatedAt = DateTime.UtcNow;
        _context.Committees.Add(committee);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created committee '{Name}' at level {Level}", committee.Name, committee.HierarchyLevel);
    }

    public async Task UpdateCommitteeAsync(Committee committee)
    {
        _context.Committees.Update(committee);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated committee '{Name}'", committee.Name);
    }

    public async Task DeleteCommitteeAsync(int id)
    {
        var committee = await _context.Committees
            .Include(c => c.SubCommittees)
            .Include(c => c.Memberships)
            .Include(c => c.ShadowAssignments)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (committee == null) return;

        _context.ShadowAssignments.RemoveRange(committee.ShadowAssignments);
        _context.CommitteeMemberships.RemoveRange(committee.Memberships);
        _context.Committees.Remove(committee);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Deleted committee '{Name}'", committee.Name);
    }

    // ── Membership queries ──

    public async Task<List<CommitteeMembership>> GetMembershipsForCommitteeAsync(int committeeId)
    {
        return await _context.CommitteeMemberships
            .Include(m => m.User)
            .Include(m => m.Committee)
            .Where(m => m.CommitteeId == committeeId && m.EffectiveTo == null)
            .OrderBy(m => m.Role)
            .ThenBy(m => m.User.Name)
            .ToListAsync();
    }

    public async Task<List<CommitteeMembership>> GetMembershipsForUserAsync(int userId)
    {
        return await _context.CommitteeMemberships
            .Include(m => m.Committee)
            .Where(m => m.UserId == userId && m.EffectiveTo == null)
            .OrderBy(m => m.Committee.HierarchyLevel)
            .ThenBy(m => m.Committee.Name)
            .ToListAsync();
    }

    public async Task AddMembershipAsync(CommitteeMembership membership)
    {
        membership.EffectiveFrom = DateTime.UtcNow;
        _context.CommitteeMemberships.Add(membership);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Added user {UserId} to committee {CommitteeId} as {Role}",
            membership.UserId, membership.CommitteeId, membership.Role);
    }

    public async Task RemoveMembershipAsync(int membershipId)
    {
        var membership = await _context.CommitteeMemberships.FindAsync(membershipId);
        if (membership != null)
        {
            membership.EffectiveTo = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Removed membership {Id}", membershipId);
        }
    }

    public async Task UpdateMembershipRoleAsync(int membershipId, CommitteeRole newRole)
    {
        var membership = await _context.CommitteeMemberships.FindAsync(membershipId);
        if (membership != null)
        {
            membership.Role = newRole;
            await _context.SaveChangesAsync();
        }
    }

    // ── Shadow queries ──

    public async Task<List<ShadowAssignment>> GetShadowAssignmentsAsync()
    {
        return await _context.ShadowAssignments
            .Include(s => s.PrincipalUser)
            .Include(s => s.ShadowUser)
            .Include(s => s.Committee)
            .Where(s => s.IsActive)
            .OrderBy(s => s.Committee.Name)
            .ThenBy(s => s.PrincipalUser.Name)
            .ToListAsync();
    }

    public async Task<List<ShadowAssignment>> GetShadowsForCommitteeAsync(int committeeId)
    {
        return await _context.ShadowAssignments
            .Include(s => s.PrincipalUser)
            .Include(s => s.ShadowUser)
            .Where(s => s.CommitteeId == committeeId && s.IsActive)
            .ToListAsync();
    }

    public async Task AddShadowAssignmentAsync(ShadowAssignment shadow)
    {
        shadow.EffectiveFrom = DateTime.UtcNow;
        shadow.IsActive = true;
        _context.ShadowAssignments.Add(shadow);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Added shadow: {ShadowUser} for {Principal} in committee {Committee}",
            shadow.ShadowUserId, shadow.PrincipalUserId, shadow.CommitteeId);
    }

    public async Task RemoveShadowAssignmentAsync(int assignmentId)
    {
        var assignment = await _context.ShadowAssignments.FindAsync(assignmentId);
        if (assignment != null)
        {
            assignment.IsActive = false;
            assignment.EffectiveTo = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    // ── Stats ──

    public async Task<(int committees, int users, int memberships, int shadows)> GetOrganizationStatsAsync()
    {
        var committees = await _context.Committees.CountAsync(c => c.IsActive);
        var users = await _context.Users.CountAsync(u => u.IsActive);
        var memberships = await _context.CommitteeMemberships.CountAsync(m => m.EffectiveTo == null);
        var shadows = await _context.ShadowAssignments.CountAsync(s => s.IsActive);
        return (committees, users, memberships, shadows);
    }

    public async Task<List<User>> GetAvailableUsersAsync()
    {
        return await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .ToListAsync();
    }
}
