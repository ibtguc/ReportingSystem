using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Services;

public class AuditService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ApplicationDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── Append-only logging ──

    public async Task LogAsync(
        AuditActionType actionType,
        string itemType,
        int? itemId = null,
        string? itemTitle = null,
        int? userId = null,
        string? userName = null,
        string? beforeValue = null,
        string? afterValue = null,
        string? details = null,
        string? ipAddress = null,
        string? sessionId = null,
        int? committeeId = null)
    {
        var entry = new AuditLog
        {
            Timestamp = DateTime.UtcNow,
            UserId = userId,
            UserName = userName,
            ActionType = actionType,
            ItemType = itemType,
            ItemId = itemId,
            ItemTitle = Truncate(itemTitle, 500),
            BeforeValue = Truncate(beforeValue, 2000),
            AfterValue = Truncate(afterValue, 2000),
            Details = Truncate(details, 500),
            IpAddress = ipAddress,
            SessionId = sessionId,
            CommitteeId = committeeId
        };

        _context.AuditLogs.Add(entry);
        await _context.SaveChangesAsync();
    }

    public async Task LogStatusChangeAsync(
        string itemType,
        int itemId,
        string? itemTitle,
        string oldStatus,
        string newStatus,
        int? userId = null,
        string? userName = null,
        string? comments = null,
        int? committeeId = null)
    {
        await LogAsync(
            AuditActionType.StatusChange,
            itemType,
            itemId,
            itemTitle,
            userId,
            userName,
            beforeValue: oldStatus,
            afterValue: newStatus,
            details: comments,
            committeeId: committeeId);
    }

    public async Task LogAccessDecisionAsync(
        bool granted,
        string itemType,
        int itemId,
        int userId,
        string? userName = null,
        string? details = null)
    {
        await LogAsync(
            granted ? AuditActionType.AccessGranted : AuditActionType.AccessDenied,
            itemType,
            itemId,
            userId: userId,
            userName: userName,
            details: details);
    }

    // ── Query / Viewer ──

    public async Task<(List<AuditLog> logs, int totalCount)> GetAuditLogsAsync(
        int? userId = null,
        AuditActionType? actionType = null,
        string? itemType = null,
        int? committeeId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);
        if (actionType.HasValue)
            query = query.Where(a => a.ActionType == actionType.Value);
        if (!string.IsNullOrEmpty(itemType))
            query = query.Where(a => a.ItemType == itemType);
        if (committeeId.HasValue)
            query = query.Where(a => a.CommitteeId == committeeId.Value);
        if (fromDate.HasValue)
            query = query.Where(a => a.Timestamp >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(a => a.Timestamp <= toDate.Value);

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (logs, totalCount);
    }

    public async Task<AuditLog?> GetAuditLogByIdAsync(int id)
    {
        return await _context.AuditLogs.FindAsync(id);
    }

    public async Task<List<AuditLog>> GetItemHistoryAsync(string itemType, int itemId)
    {
        return await _context.AuditLogs
            .Where(a => a.ItemType == itemType && a.ItemId == itemId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    }

    public async Task<(int total, int today, int thisWeek)> GetAuditStatsAsync()
    {
        var total = await _context.AuditLogs.CountAsync();
        var todayStart = DateTime.UtcNow.Date;
        var weekStart = todayStart.AddDays(-7);
        var today = await _context.AuditLogs.CountAsync(a => a.Timestamp >= todayStart);
        var thisWeek = await _context.AuditLogs.CountAsync(a => a.Timestamp >= weekStart);
        return (total, today, thisWeek);
    }

    // ── Export ──

    public async Task<string> ExportToCsvAsync(
        int? userId = null,
        AuditActionType? actionType = null,
        string? itemType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var (logs, _) = await GetAuditLogsAsync(userId, actionType, itemType,
            fromDate: fromDate, toDate: toDate, page: 1, pageSize: 10000);

        var lines = new List<string>
        {
            "Timestamp,UserId,UserName,ActionType,ItemType,ItemId,ItemTitle,BeforeValue,AfterValue,Details,IpAddress"
        };

        foreach (var log in logs)
        {
            lines.Add(string.Join(",",
                CsvEscape(log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")),
                log.UserId?.ToString() ?? "",
                CsvEscape(log.UserName ?? ""),
                log.ActionType.ToString(),
                CsvEscape(log.ItemType),
                log.ItemId?.ToString() ?? "",
                CsvEscape(log.ItemTitle ?? ""),
                CsvEscape(log.BeforeValue ?? ""),
                CsvEscape(log.AfterValue ?? ""),
                CsvEscape(log.Details ?? ""),
                CsvEscape(log.IpAddress ?? "")));
        }

        return string.Join("\n", lines);
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (value == null) return null;
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
