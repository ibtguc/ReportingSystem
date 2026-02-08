using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Services;

public class ConfidentialityService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ConfidentialityService> _logger;

    public ConfidentialityService(ApplicationDbContext context, ILogger<ConfidentialityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ───── Mark / Unmark ─────

    /// <summary>
    /// Mark an item as confidential. FR-4.5.1.1–4.5.1.3.
    /// </summary>
    public async Task<ConfidentialityMarking> MarkAsConfidentialAsync(
        ConfidentialItemType itemType, int itemId, int markedByUserId,
        int committeeId, string? reason = null, int? minChairmanOfficeRank = null)
    {
        var committee = await _context.Committees.FindAsync(committeeId)
            ?? throw new InvalidOperationException("Committee not found.");

        // Set the IsConfidential flag on the item itself
        await SetItemConfidentialFlag(itemType, itemId, true);

        // Deactivate any existing active marking
        var existing = await _context.Set<ConfidentialityMarking>()
            .Where(m => m.ItemType == itemType && m.ItemId == itemId && m.IsActive)
            .FirstOrDefaultAsync();
        if (existing != null)
        {
            existing.IsActive = false;
            existing.UnmarkedAt = DateTime.UtcNow;
            existing.UnmarkedById = markedByUserId;
        }

        var marking = new ConfidentialityMarking
        {
            ItemType = itemType,
            ItemId = itemId,
            MarkedById = markedByUserId,
            MarkerCommitteeLevel = committee.HierarchyLevel,
            MarkerCommitteeId = committeeId,
            MinChairmanOfficeRank = minChairmanOfficeRank,
            Reason = reason,
            IsActive = true,
            MarkedAt = DateTime.UtcNow
        };

        _context.Set<ConfidentialityMarking>().Add(marking);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Item {Type}:{Id} marked confidential by user {UserId} at committee level {Level}",
            itemType, itemId, markedByUserId, committee.HierarchyLevel);

        return marking;
    }

    /// <summary>
    /// Remove confidentiality marking. FR-4.5.1.7: Reversible by original marker or SystemAdmin.
    /// </summary>
    public async Task<bool> RemoveConfidentialMarkingAsync(
        ConfidentialItemType itemType, int itemId, int userId)
    {
        var marking = await _context.Set<ConfidentialityMarking>()
            .Where(m => m.ItemType == itemType && m.ItemId == itemId && m.IsActive)
            .FirstOrDefaultAsync();

        if (marking == null) return false;

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        // Only original marker or SystemAdmin can reverse
        if (marking.MarkedById != userId && user.SystemRole != SystemRole.SystemAdmin)
            return false;

        marking.IsActive = false;
        marking.UnmarkedAt = DateTime.UtcNow;
        marking.UnmarkedById = userId;

        await SetItemConfidentialFlag(itemType, itemId, false);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Confidentiality removed from {Type}:{Id} by user {UserId}",
            itemType, itemId, userId);

        return true;
    }

    /// <summary>
    /// Get the active confidentiality marking for an item.
    /// </summary>
    public async Task<ConfidentialityMarking?> GetActiveMarkingAsync(
        ConfidentialItemType itemType, int itemId)
    {
        return await _context.Set<ConfidentialityMarking>()
            .Include(m => m.MarkedBy)
            .Include(m => m.MarkerCommittee)
            .Where(m => m.ItemType == itemType && m.ItemId == itemId && m.IsActive)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Get all markings (including historical) for audit trail.
    /// </summary>
    public async Task<List<ConfidentialityMarking>> GetMarkingHistoryAsync(
        ConfidentialItemType itemType, int itemId)
    {
        return await _context.Set<ConfidentialityMarking>()
            .Include(m => m.MarkedBy)
            .Include(m => m.MarkerCommittee)
            .Include(m => m.UnmarkedBy)
            .Where(m => m.ItemType == itemType && m.ItemId == itemId)
            .OrderByDescending(m => m.MarkedAt)
            .ToListAsync();
    }

    // ───── Access Control Logic ─────

    /// <summary>
    /// Core access check: Can a user access a confidential item?
    /// Implements FR-4.5.1.4, FR-4.5.1.5, FR-4.5.1.6, FR-4.5.2, FR-4.5.3.
    /// </summary>
    public async Task<bool> CanUserAccessConfidentialItemAsync(
        ConfidentialItemType itemType, int itemId, int userId)
    {
        var marking = await _context.Set<ConfidentialityMarking>()
            .Where(m => m.ItemType == itemType && m.ItemId == itemId && m.IsActive)
            .FirstOrDefaultAsync();

        // No active marking = not confidential = accessible
        if (marking == null) return true;

        var user = await _context.Users
            .Include(u => u.CommitteeMemberships)
                .ThenInclude(cm => cm.Committee)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return false;

        // FR-4.5.1.5: Chairman always has access
        if (user.SystemRole == SystemRole.Chairman) return true;

        // SystemAdmin always has access
        if (user.SystemRole == SystemRole.SystemAdmin) return true;

        // FR-4.5.1.6: Shadow members lose access to confidential items
        var isShadow = await IsUserShadowForItemAsync(userId, itemType, itemId);
        if (isShadow) return false;

        // FR-4.5.3.4: Explicit access grant overrides hierarchy rules
        var hasExplicitGrant = await _context.Set<AccessGrant>()
            .AnyAsync(g => g.ItemType == itemType && g.ItemId == itemId
                        && g.GrantedToUserId == userId && g.IsActive);
        if (hasExplicitGrant) return true;

        // FR-4.5.2: Chairman's Office rank-based access
        if (user.SystemRole == SystemRole.ChairmanOffice && marking.MinChairmanOfficeRank.HasValue)
        {
            // Lower rank number = higher seniority. Access if user rank <= min rank.
            if (user.ChairmanOfficeRank.HasValue && user.ChairmanOfficeRank.Value <= marking.MinChairmanOfficeRank.Value)
                return true;

            // CO users without sufficient rank can't access CO-restricted items
            return false;
        }

        // FR-4.5.1.4: Higher hierarchy levels (lower numeric value) can access
        // The item was marked at MarkerCommitteeLevel; users at strictly higher levels can access
        var userCommitteeLevels = user.CommitteeMemberships
            .Where(cm => cm.EffectiveTo == null)
            .Select(cm => cm.Committee.HierarchyLevel)
            .ToList();

        // User has access if they belong to any committee at a higher level (lower numeric)
        if (userCommitteeLevels.Any(level => level < marking.MarkerCommitteeLevel))
            return true;

        // FR-4.5.3: Cross-committee access via membership — if user is a member
        // of the same committee where the item was marked, they have access
        var isMarkerCommitteeMember = user.CommitteeMemberships
            .Any(cm => cm.CommitteeId == marking.MarkerCommitteeId && cm.EffectiveTo == null);
        if (isMarkerCommitteeMember) return true;

        return false;
    }

    /// <summary>
    /// Get the access impact preview before marking an item confidential.
    /// FR-4.5.1.2: Shows who will lose/retain access.
    /// </summary>
    public async Task<AccessImpactPreview> GetAccessImpactPreviewAsync(
        ConfidentialItemType itemType, int itemId, int committeeId, int? minCoRank = null)
    {
        var committee = await _context.Committees.FindAsync(committeeId);
        if (committee == null) return new AccessImpactPreview();

        // Get all active users
        var users = await _context.Users
            .Include(u => u.CommitteeMemberships)
                .ThenInclude(cm => cm.Committee)
            .Where(u => u.IsActive)
            .ToListAsync();

        // Get shadow assignments relevant to the item's committee
        var shadows = await _context.ShadowAssignments
            .Where(s => s.IsActive && s.CommitteeId == committeeId)
            .Select(s => s.ShadowUserId)
            .ToListAsync();

        var retainAccess = new List<User>();
        var loseAccess = new List<User>();

        foreach (var user in users)
        {
            // Chairman and SystemAdmin always retain
            if (user.SystemRole == SystemRole.Chairman || user.SystemRole == SystemRole.SystemAdmin)
            {
                retainAccess.Add(user);
                continue;
            }

            // Shadows lose access
            if (shadows.Contains(user.Id))
            {
                loseAccess.Add(user);
                continue;
            }

            // CO rank-based
            if (user.SystemRole == SystemRole.ChairmanOffice && minCoRank.HasValue)
            {
                if (user.ChairmanOfficeRank.HasValue && user.ChairmanOfficeRank.Value <= minCoRank.Value)
                    retainAccess.Add(user);
                else
                    loseAccess.Add(user);
                continue;
            }

            // Hierarchy check
            var userLevels = user.CommitteeMemberships
                .Where(cm => cm.EffectiveTo == null)
                .Select(cm => cm.Committee.HierarchyLevel)
                .ToList();

            var isSameCommitteeMember = user.CommitteeMemberships
                .Any(cm => cm.CommitteeId == committeeId && cm.EffectiveTo == null);

            if (isSameCommitteeMember || userLevels.Any(l => l < committee.HierarchyLevel))
                retainAccess.Add(user);
            else
                loseAccess.Add(user);
        }

        return new AccessImpactPreview
        {
            RetainAccess = retainAccess,
            LoseAccess = loseAccess
        };
    }

    // ───── Explicit Access Grants (FR-4.5.3.4) ─────

    /// <summary>
    /// Grant explicit access to a confidential item.
    /// </summary>
    public async Task<AccessGrant> GrantAccessAsync(
        ConfidentialItemType itemType, int itemId, int grantedToUserId,
        int grantedByUserId, string? reason = null)
    {
        // Check if grant already exists
        var existing = await _context.Set<AccessGrant>()
            .FirstOrDefaultAsync(g => g.ItemType == itemType && g.ItemId == itemId
                                   && g.GrantedToUserId == grantedToUserId && g.IsActive);
        if (existing != null) return existing;

        var grant = new AccessGrant
        {
            ItemType = itemType,
            ItemId = itemId,
            GrantedToUserId = grantedToUserId,
            GrantedById = grantedByUserId,
            Reason = reason,
            IsActive = true,
            GrantedAt = DateTime.UtcNow
        };

        _context.Set<AccessGrant>().Add(grant);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Access granted for {Type}:{Id} to user {ToUserId} by user {ByUserId}",
            itemType, itemId, grantedToUserId, grantedByUserId);

        return grant;
    }

    /// <summary>
    /// Revoke an explicit access grant.
    /// </summary>
    public async Task<bool> RevokeAccessAsync(int grantId, int revokedByUserId)
    {
        var grant = await _context.Set<AccessGrant>().FindAsync(grantId);
        if (grant == null || !grant.IsActive) return false;

        grant.IsActive = false;
        grant.RevokedAt = DateTime.UtcNow;
        grant.RevokedById = revokedByUserId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Access grant {GrantId} revoked by user {UserId}", grantId, revokedByUserId);

        return true;
    }

    /// <summary>
    /// Get all active access grants for an item.
    /// </summary>
    public async Task<List<AccessGrant>> GetAccessGrantsAsync(
        ConfidentialItemType itemType, int itemId)
    {
        return await _context.Set<AccessGrant>()
            .Include(g => g.GrantedTo)
            .Include(g => g.GrantedBy)
            .Where(g => g.ItemType == itemType && g.ItemId == itemId && g.IsActive)
            .OrderBy(g => g.GrantedAt)
            .ToListAsync();
    }

    // ───── Filtering Helpers ─────

    /// <summary>
    /// Filter a list of reports to only those the user can access.
    /// </summary>
    public async Task<List<Report>> FilterAccessibleReportsAsync(List<Report> reports, int userId)
    {
        var accessible = new List<Report>();
        foreach (var report in reports)
        {
            if (!report.IsConfidential)
            {
                accessible.Add(report);
                continue;
            }
            if (await CanUserAccessConfidentialItemAsync(ConfidentialItemType.Report, report.Id, userId))
                accessible.Add(report);
        }
        return accessible;
    }

    /// <summary>
    /// Filter a list of directives to only those the user can access.
    /// </summary>
    public async Task<List<Directive>> FilterAccessibleDirectivesAsync(List<Directive> directives, int userId)
    {
        var accessible = new List<Directive>();
        foreach (var directive in directives)
        {
            if (!directive.IsConfidential)
            {
                accessible.Add(directive);
                continue;
            }
            if (await CanUserAccessConfidentialItemAsync(ConfidentialItemType.Directive, directive.Id, userId))
                accessible.Add(directive);
        }
        return accessible;
    }

    /// <summary>
    /// Filter a list of meetings to only those the user can access.
    /// </summary>
    public async Task<List<Meeting>> FilterAccessibleMeetingsAsync(List<Meeting> meetings, int userId)
    {
        var accessible = new List<Meeting>();
        foreach (var meeting in meetings)
        {
            if (!meeting.IsConfidential)
            {
                accessible.Add(meeting);
                continue;
            }
            if (await CanUserAccessConfidentialItemAsync(ConfidentialItemType.Meeting, meeting.Id, userId))
                accessible.Add(meeting);
        }
        return accessible;
    }

    // ───── Permission Checks ─────

    /// <summary>
    /// Check if user can mark/unmark an item as confidential.
    /// Owner of the item (author/issuer/moderator) or SystemAdmin.
    /// </summary>
    public async Task<bool> CanUserMarkConfidentialAsync(
        ConfidentialItemType itemType, int itemId, int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;
        if (user.SystemRole == SystemRole.SystemAdmin) return true;

        return itemType switch
        {
            ConfidentialItemType.Report => await _context.Reports.AnyAsync(r => r.Id == itemId && r.AuthorId == userId),
            ConfidentialItemType.Directive => await _context.Directives.AnyAsync(d => d.Id == itemId && d.IssuerId == userId),
            ConfidentialItemType.Meeting => await _context.Meetings.AnyAsync(m => m.Id == itemId && m.ModeratorId == userId),
            _ => false
        };
    }

    /// <summary>
    /// Get the committee associated with an item, for determining hierarchy context.
    /// </summary>
    public async Task<Committee?> GetItemCommitteeAsync(ConfidentialItemType itemType, int itemId)
    {
        return itemType switch
        {
            ConfidentialItemType.Report => await _context.Reports
                .Where(r => r.Id == itemId).Select(r => r.Committee).FirstOrDefaultAsync(),
            ConfidentialItemType.Directive => await _context.Directives
                .Where(d => d.Id == itemId).Select(d => d.TargetCommittee).FirstOrDefaultAsync(),
            ConfidentialItemType.Meeting => await _context.Meetings
                .Where(m => m.Id == itemId).Select(m => m.Committee).FirstOrDefaultAsync(),
            _ => null
        };
    }

    // ───── Private Helpers ─────

    private async Task SetItemConfidentialFlag(ConfidentialItemType itemType, int itemId, bool isConfidential)
    {
        switch (itemType)
        {
            case ConfidentialItemType.Report:
                var report = await _context.Reports.FindAsync(itemId);
                if (report != null) report.IsConfidential = isConfidential;
                break;
            case ConfidentialItemType.Directive:
                var directive = await _context.Directives.FindAsync(itemId);
                if (directive != null) directive.IsConfidential = isConfidential;
                break;
            case ConfidentialItemType.Meeting:
                var meeting = await _context.Meetings.FindAsync(itemId);
                if (meeting != null) meeting.IsConfidential = isConfidential;
                break;
        }
    }

    /// <summary>
    /// Check if a user is a shadow member in the context of this item.
    /// FR-4.5.1.6: Shadows lose access to confidential items.
    /// </summary>
    private async Task<bool> IsUserShadowForItemAsync(int userId, ConfidentialItemType itemType, int itemId)
    {
        int? committeeId = itemType switch
        {
            ConfidentialItemType.Report => await _context.Reports
                .Where(r => r.Id == itemId).Select(r => (int?)r.CommitteeId).FirstOrDefaultAsync(),
            ConfidentialItemType.Directive => await _context.Directives
                .Where(d => d.Id == itemId).Select(d => (int?)d.TargetCommitteeId).FirstOrDefaultAsync(),
            ConfidentialItemType.Meeting => await _context.Meetings
                .Where(m => m.Id == itemId).Select(m => (int?)m.CommitteeId).FirstOrDefaultAsync(),
            _ => null
        };

        if (!committeeId.HasValue) return false;

        return await _context.ShadowAssignments
            .AnyAsync(s => s.ShadowUserId == userId && s.CommitteeId == committeeId.Value && s.IsActive);
    }
}

/// <summary>
/// Preview of who will retain/lose access when marking an item confidential.
/// </summary>
public class AccessImpactPreview
{
    public List<User> RetainAccess { get; set; } = new();
    public List<User> LoseAccess { get; set; } = new();
}
