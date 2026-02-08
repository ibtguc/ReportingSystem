using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Services;

public class DirectiveService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DirectiveService> _logger;

    public DirectiveService(ApplicationDbContext context, ILogger<DirectiveService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── Queries ──

    public async Task<List<Directive>> GetDirectivesAsync(
        int? targetCommitteeId = null,
        int? issuerId = null,
        DirectiveStatus? status = null,
        DirectivePriority? priority = null,
        bool includeClosed = false)
    {
        var query = _context.Directives
            .Include(d => d.Issuer)
            .Include(d => d.TargetCommittee)
            .Include(d => d.TargetUser)
            .Include(d => d.ParentDirective)
            .AsQueryable();

        if (!includeClosed)
            query = query.Where(d => d.Status != DirectiveStatus.Closed);

        if (targetCommitteeId.HasValue)
            query = query.Where(d => d.TargetCommitteeId == targetCommitteeId.Value);

        if (issuerId.HasValue)
            query = query.Where(d => d.IssuerId == issuerId.Value);

        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);

        if (priority.HasValue)
            query = query.Where(d => d.Priority == priority.Value);

        return await query
            .OrderByDescending(d => d.Priority)
            .ThenByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<Directive?> GetDirectiveByIdAsync(int id)
    {
        return await _context.Directives
            .Include(d => d.Issuer)
            .Include(d => d.TargetCommittee)
            .Include(d => d.TargetUser)
            .Include(d => d.RelatedReport).ThenInclude(r => r!.Author)
            .Include(d => d.RelatedReport).ThenInclude(r => r!.Committee)
            .Include(d => d.ParentDirective).ThenInclude(p => p!.Issuer)
            .Include(d => d.ChildDirectives).ThenInclude(c => c.TargetCommittee)
            .Include(d => d.ChildDirectives).ThenInclude(c => c.Issuer)
            .Include(d => d.StatusHistory).ThenInclude(h => h.ChangedBy)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    /// <summary>
    /// Gets directives visible to a specific user — either issued by them,
    /// targeting their committees, or targeting them directly.
    /// </summary>
    public async Task<List<Directive>> GetDirectivesForUserAsync(int userId, bool includeClosed = false)
    {
        var userCommitteeIds = await _context.CommitteeMemberships
            .Where(m => m.UserId == userId && m.EffectiveTo == null)
            .Select(m => m.CommitteeId)
            .ToListAsync();

        var query = _context.Directives
            .Include(d => d.Issuer)
            .Include(d => d.TargetCommittee)
            .Include(d => d.TargetUser)
            .Where(d => d.IssuerId == userId
                     || userCommitteeIds.Contains(d.TargetCommitteeId)
                     || d.TargetUserId == userId);

        if (!includeClosed)
            query = query.Where(d => d.Status != DirectiveStatus.Closed);

        return await query
            .OrderByDescending(d => d.Priority)
            .ThenByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    // ── Create ──

    public async Task<Directive> CreateDirectiveAsync(Directive directive, int userId)
    {
        directive.IssuerId = userId;
        directive.CreatedAt = DateTime.UtcNow;
        directive.Status = DirectiveStatus.Issued;

        _context.Directives.Add(directive);
        await _context.SaveChangesAsync();

        await AddStatusHistoryAsync(directive.Id, DirectiveStatus.Issued, DirectiveStatus.Issued,
            userId, "Directive issued");

        _logger.LogInformation("Directive '{Title}' (ID: {Id}) created by user {UserId} targeting committee {CommitteeId}",
            directive.Title, directive.Id, userId, directive.TargetCommitteeId);

        return directive;
    }

    /// <summary>
    /// Forward (propagate) a directive downward: creates a child directive linked to the parent,
    /// targeting a sub-committee, optionally with an annotation.
    /// </summary>
    public async Task<Directive> ForwardDirectiveAsync(int parentDirectiveId, int targetCommitteeId,
        string? annotation, int userId)
    {
        var parent = await _context.Directives.FindAsync(parentDirectiveId);
        if (parent == null)
            throw new InvalidOperationException("Parent directive not found.");

        var child = new Directive
        {
            Title = parent.Title,
            DirectiveType = parent.DirectiveType,
            Priority = parent.Priority,
            Status = DirectiveStatus.Issued,
            IssuerId = userId,
            TargetCommitteeId = targetCommitteeId,
            RelatedReportId = parent.RelatedReportId,
            ParentDirectiveId = parentDirectiveId,
            BodyContent = parent.BodyContent,
            ForwardingAnnotation = annotation,
            Deadline = parent.Deadline,
            CreatedAt = DateTime.UtcNow
        };

        _context.Directives.Add(child);
        await _context.SaveChangesAsync();

        await AddStatusHistoryAsync(child.Id, DirectiveStatus.Issued, DirectiveStatus.Issued,
            userId, $"Forwarded from Directive #{parentDirectiveId}");

        _logger.LogInformation("Directive #{ParentId} forwarded as #{ChildId} to committee {CommitteeId} by user {UserId}",
            parentDirectiveId, child.Id, targetCommitteeId, userId);

        return child;
    }

    // ── Status Transitions ──

    public async Task<bool> MarkDeliveredAsync(int directiveId, int userId)
    {
        var directive = await _context.Directives.FindAsync(directiveId);
        if (directive == null || directive.Status != DirectiveStatus.Issued)
            return false;

        var oldStatus = directive.Status;
        directive.Status = DirectiveStatus.Delivered;
        directive.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await AddStatusHistoryAsync(directiveId, oldStatus, DirectiveStatus.Delivered, userId,
            "Directive delivered — viewed by target");
        return true;
    }

    public async Task<bool> AcknowledgeAsync(int directiveId, int userId)
    {
        var directive = await _context.Directives.FindAsync(directiveId);
        if (directive == null || (directive.Status != DirectiveStatus.Delivered && directive.Status != DirectiveStatus.Issued))
            return false;

        var oldStatus = directive.Status;
        directive.Status = DirectiveStatus.Acknowledged;
        directive.AcknowledgedAt = DateTime.UtcNow;
        directive.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await AddStatusHistoryAsync(directiveId, oldStatus, DirectiveStatus.Acknowledged, userId,
            "Directive acknowledged by target");

        _logger.LogInformation("Directive {Id} acknowledged by user {UserId}", directiveId, userId);
        return true;
    }

    public async Task<bool> StartProgressAsync(int directiveId, int userId)
    {
        var directive = await _context.Directives.FindAsync(directiveId);
        if (directive == null || directive.Status != DirectiveStatus.Acknowledged)
            return false;

        var oldStatus = directive.Status;
        directive.Status = DirectiveStatus.InProgress;
        directive.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await AddStatusHistoryAsync(directiveId, oldStatus, DirectiveStatus.InProgress, userId,
            "Work started on directive");
        return true;
    }

    public async Task<bool> MarkImplementedAsync(int directiveId, int userId, string? comments = null)
    {
        var directive = await _context.Directives.FindAsync(directiveId);
        if (directive == null || (directive.Status != DirectiveStatus.InProgress && directive.Status != DirectiveStatus.Acknowledged))
            return false;

        var oldStatus = directive.Status;
        directive.Status = DirectiveStatus.Implemented;
        directive.ImplementedAt = DateTime.UtcNow;
        directive.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await AddStatusHistoryAsync(directiveId, oldStatus, DirectiveStatus.Implemented, userId,
            comments ?? "Directive implemented");

        _logger.LogInformation("Directive {Id} marked as implemented by user {UserId}", directiveId, userId);
        return true;
    }

    public async Task<bool> VerifyAsync(int directiveId, int userId, string? comments = null)
    {
        var directive = await _context.Directives.FindAsync(directiveId);
        if (directive == null || directive.Status != DirectiveStatus.Implemented)
            return false;

        var oldStatus = directive.Status;
        directive.Status = DirectiveStatus.Verified;
        directive.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await AddStatusHistoryAsync(directiveId, oldStatus, DirectiveStatus.Verified, userId,
            comments ?? "Directive implementation verified");
        return true;
    }

    public async Task<bool> CloseAsync(int directiveId, int userId, string? comments = null)
    {
        var directive = await _context.Directives.FindAsync(directiveId);
        if (directive == null || (directive.Status != DirectiveStatus.Verified && directive.Status != DirectiveStatus.Implemented))
            return false;

        var oldStatus = directive.Status;
        directive.Status = DirectiveStatus.Closed;
        directive.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await AddStatusHistoryAsync(directiveId, oldStatus, DirectiveStatus.Closed, userId,
            comments ?? "Directive closed");

        _logger.LogInformation("Directive {Id} closed by user {UserId}", directiveId, userId);
        return true;
    }

    // ── Propagation Tree ──

    /// <summary>
    /// Builds the full propagation tree starting from the root directive.
    /// Walks up to find root, then builds children tree downward.
    /// </summary>
    public async Task<DirectivePropagationNode> GetPropagationTreeAsync(int directiveId)
    {
        // Find root of the chain
        var rootId = await GetRootDirectiveIdAsync(directiveId);
        var root = await _context.Directives
            .Include(d => d.Issuer)
            .Include(d => d.TargetCommittee)
            .FirstOrDefaultAsync(d => d.Id == rootId);

        if (root == null)
            return new DirectivePropagationNode { Directive = new Directive { Title = "Not Found" } };

        return await BuildPropagationNodeAsync(root, 0);
    }

    private async Task<int> GetRootDirectiveIdAsync(int directiveId)
    {
        var directive = await _context.Directives.FindAsync(directiveId);
        if (directive == null) return directiveId;

        while (directive.ParentDirectiveId.HasValue)
        {
            directive = await _context.Directives.FindAsync(directive.ParentDirectiveId.Value);
            if (directive == null) break;
        }
        return directive?.Id ?? directiveId;
    }

    private async Task<DirectivePropagationNode> BuildPropagationNodeAsync(Directive directive, int depth)
    {
        var node = new DirectivePropagationNode
        {
            Directive = directive,
            Depth = depth
        };

        var children = await _context.Directives
            .Include(d => d.Issuer)
            .Include(d => d.TargetCommittee)
            .Where(d => d.ParentDirectiveId == directive.Id)
            .OrderBy(d => d.TargetCommittee.HierarchyLevel)
            .ThenBy(d => d.CreatedAt)
            .ToListAsync();

        foreach (var child in children)
        {
            var childNode = await BuildPropagationNodeAsync(child, depth + 1);
            node.Children.Add(childNode);
        }

        return node;
    }

    // ── Overdue Tracking ──

    /// <summary>
    /// Gets all directives that are past their deadline and not yet closed/verified.
    /// </summary>
    public async Task<List<Directive>> GetOverdueDirectivesAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.Directives
            .Include(d => d.Issuer)
            .Include(d => d.TargetCommittee)
            .Include(d => d.TargetUser)
            .Where(d => d.Deadline.HasValue
                     && d.Deadline.Value < now
                     && d.Status != DirectiveStatus.Closed
                     && d.Status != DirectiveStatus.Verified)
            .OrderBy(d => d.Deadline)
            .ThenByDescending(d => d.Priority)
            .ToListAsync();
    }

    /// <summary>
    /// Gets directives approaching their deadline (within specified days).
    /// </summary>
    public async Task<List<Directive>> GetApproachingDeadlineDirectivesAsync(int withinDays = 3)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(withinDays);
        return await _context.Directives
            .Include(d => d.Issuer)
            .Include(d => d.TargetCommittee)
            .Include(d => d.TargetUser)
            .Where(d => d.Deadline.HasValue
                     && d.Deadline.Value >= now
                     && d.Deadline.Value <= cutoff
                     && d.Status != DirectiveStatus.Closed
                     && d.Status != DirectiveStatus.Verified)
            .OrderBy(d => d.Deadline)
            .ThenByDescending(d => d.Priority)
            .ToListAsync();
    }

    // ── Access Checks ──

    /// <summary>
    /// Can the user issue directives? Chairman, ChairmanOffice, SystemAdmin,
    /// or head of a committee.
    /// </summary>
    public async Task<bool> CanUserIssueDirectivesAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        if (user.SystemRole == SystemRole.SystemAdmin
            || user.SystemRole == SystemRole.Chairman
            || user.SystemRole == SystemRole.ChairmanOffice)
            return true;

        // Committee heads can issue directives to their sub-committees
        return await _context.CommitteeMemberships
            .AnyAsync(m => m.UserId == userId && m.Role == CommitteeRole.Head && m.EffectiveTo == null);
    }

    /// <summary>
    /// Checks if user is the target of this directive (member of target committee or target user).
    /// </summary>
    public async Task<bool> IsUserTargetOfDirectiveAsync(int userId, Directive directive)
    {
        if (directive.TargetUserId == userId) return true;

        return await _context.CommitteeMemberships
            .AnyAsync(m => m.UserId == userId && m.CommitteeId == directive.TargetCommitteeId && m.EffectiveTo == null);
    }

    /// <summary>
    /// Gets committees that the user can target with directives:
    /// - Chairman/ChairmanOffice/SystemAdmin: all committees
    /// - Committee heads: their sub-committees
    /// </summary>
    public async Task<List<Committee>> GetTargetableCommitteesAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return new();

        if (user.SystemRole == SystemRole.SystemAdmin
            || user.SystemRole == SystemRole.Chairman
            || user.SystemRole == SystemRole.ChairmanOffice)
        {
            return await _context.Committees
                .Where(c => c.IsActive)
                .OrderBy(c => c.HierarchyLevel)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        // For committee heads: get their committees and all descendant committees
        var headOfCommittees = await _context.CommitteeMemberships
            .Where(m => m.UserId == userId && m.Role == CommitteeRole.Head && m.EffectiveTo == null)
            .Select(m => m.CommitteeId)
            .ToListAsync();

        var targetableIds = new HashSet<int>();
        foreach (var committeeId in headOfCommittees)
        {
            var descendants = await GetDescendantCommitteeIdsAsync(committeeId);
            foreach (var id in descendants) targetableIds.Add(id);
            targetableIds.Add(committeeId);
        }

        return await _context.Committees
            .Where(c => targetableIds.Contains(c.Id) && c.IsActive)
            .OrderBy(c => c.HierarchyLevel)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Gets sub-committees of the directive's target committee for forwarding.
    /// </summary>
    public async Task<List<Committee>> GetForwardableCommitteesAsync(int directiveId)
    {
        var directive = await _context.Directives.FindAsync(directiveId);
        if (directive == null) return new();

        return await _context.Committees
            .Where(c => c.ParentCommitteeId == directive.TargetCommitteeId && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    // ── Stats ──

    public async Task<(int total, int issued, int acknowledged, int inProgress, int implemented, int overdue)> GetDirectiveStatsAsync()
    {
        var now = DateTime.UtcNow;
        var total = await _context.Directives.CountAsync(d => d.Status != DirectiveStatus.Closed);
        var issued = await _context.Directives.CountAsync(d => d.Status == DirectiveStatus.Issued || d.Status == DirectiveStatus.Delivered);
        var acknowledged = await _context.Directives.CountAsync(d => d.Status == DirectiveStatus.Acknowledged);
        var inProgress = await _context.Directives.CountAsync(d => d.Status == DirectiveStatus.InProgress);
        var implemented = await _context.Directives.CountAsync(d => d.Status == DirectiveStatus.Implemented || d.Status == DirectiveStatus.Verified);
        var overdue = await _context.Directives.CountAsync(d =>
            d.Deadline.HasValue && d.Deadline.Value < now
            && d.Status != DirectiveStatus.Closed && d.Status != DirectiveStatus.Verified);
        return (total, issued, acknowledged, inProgress, implemented, overdue);
    }

    // ── Helpers ──

    private async Task AddStatusHistoryAsync(int directiveId, DirectiveStatus oldStatus,
        DirectiveStatus newStatus, int changedById, string? comments)
    {
        var history = new DirectiveStatusHistory
        {
            DirectiveId = directiveId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedById = changedById,
            ChangedAt = DateTime.UtcNow,
            Comments = comments
        };
        _context.DirectiveStatusHistories.Add(history);
        await _context.SaveChangesAsync();
    }

    private async Task<List<int>> GetDescendantCommitteeIdsAsync(int parentId)
    {
        var result = new List<int>();
        var children = await _context.Committees
            .Where(c => c.ParentCommitteeId == parentId && c.IsActive)
            .Select(c => c.Id)
            .ToListAsync();

        result.AddRange(children);
        foreach (var childId in children)
        {
            var descendants = await GetDescendantCommitteeIdsAsync(childId);
            result.AddRange(descendants);
        }
        return result;
    }
}

/// <summary>
/// Tree node for directive propagation visualization.
/// </summary>
public class DirectivePropagationNode
{
    public Directive Directive { get; set; } = null!;
    public int Depth { get; set; }
    public List<DirectivePropagationNode> Children { get; set; } = new();
}
