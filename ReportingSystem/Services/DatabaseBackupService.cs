using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;
using System.Text.RegularExpressions;

namespace ReportingSystem.Services;

/// <summary>
/// Service for managing database backups
/// </summary>
public class DatabaseBackupService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseBackupService> _logger;
    private readonly IWebHostEnvironment _environment;

    private string BackupDirectory => Path.Combine(_environment.ContentRootPath, "db", "Backups");

    public DatabaseBackupService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<DatabaseBackupService> logger,
        IWebHostEnvironment environment)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Gets the path to the SQLite database file
    /// </summary>
    private string GetDatabasePath()
    {
        var connectionString = _configuration["DatabaseSettings:ConnectionStrings:SQLite"]
            ?? "Data Source=reporting.db";

        var match = Regex.Match(connectionString, @"Data Source=(.+?)(?:;|$)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var dbPath = match.Groups[1].Value.Trim();
            if (!Path.IsPathRooted(dbPath))
            {
                dbPath = Path.Combine(_environment.ContentRootPath, dbPath);
            }
            return dbPath;
        }

        return Path.Combine(_environment.ContentRootPath, "reporting.db");
    }

    private void EnsureBackupDirectoryExists()
    {
        if (!Directory.Exists(BackupDirectory))
        {
            Directory.CreateDirectory(BackupDirectory);
            _logger.LogInformation("Created backup directory: {BackupDirectory}", BackupDirectory);
        }
    }

    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        sanitized = sanitized.Replace(' ', '_');
        if (sanitized.Length > 100)
        {
            sanitized = sanitized.Substring(0, 100);
        }
        return sanitized;
    }

    private string GetTimestamp()
    {
        return DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
    }

    /// <summary>
    /// Checks if an automatic backup exists within the specified hours
    /// </summary>
    public async Task<bool> HasRecentAutomaticBackupAsync(int withinHours = 12)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-withinHours);

        return await _context.DatabaseBackups
            .AnyAsync(b => b.IsAutomaticDailyBackup &&
                          b.CreatedAt >= cutoffTime);
    }

    /// <summary>
    /// Creates an automatic backup if one doesn't exist within the last 12 hours (twice daily)
    /// </summary>
    public async Task<DatabaseBackup?> EnsureAutomaticBackupAsync(string? triggeredBy = null)
    {
        if (await HasRecentAutomaticBackupAsync(withinHours: 12))
        {
            _logger.LogDebug("Automatic backup already exists within the last 12 hours");
            return null;
        }

        var timestamp = GetTimestamp();
        var fileName = $"reporting_auto_{timestamp}.db";
        var period = DateTime.Now.Hour < 12 ? "AM" : "PM";

        return await CreateBackupAsync(
            name: $"Auto Backup {DateTime.Now:yyyy-MM-dd} {period}",
            description: $"Automatic backup triggered by {triggeredBy ?? "system"}",
            fileName: fileName,
            type: BackupType.AutomaticDaily,
            isAutomaticDailyBackup: true,
            createdBy: triggeredBy
        );
    }

    public Task<bool> HasDailyBackupForTodayAsync() => HasRecentAutomaticBackupAsync(12);
    public Task<DatabaseBackup?> EnsureDailyBackupAsync(string? triggeredBy = null) => EnsureAutomaticBackupAsync(triggeredBy);

    /// <summary>
    /// Creates a manual backup with user-specified name and description
    /// </summary>
    public async Task<DatabaseBackup> CreateManualBackupAsync(
        string name,
        string? description,
        string? createdBy)
    {
        var sanitizedName = SanitizeFileName(name);
        var timestamp = GetTimestamp();
        var fileName = $"{sanitizedName}_{timestamp}.db";

        return await CreateBackupAsync(
            name: name,
            description: description,
            fileName: fileName,
            type: BackupType.Manual,
            isAutomaticDailyBackup: false,
            createdBy: createdBy
        );
    }

    /// <summary>
    /// Creates a pre-restore backup before restoring another backup
    /// </summary>
    public async Task<DatabaseBackup> CreatePreRestoreBackupAsync(string? createdBy)
    {
        var timestamp = GetTimestamp();
        var fileName = $"pre_restore_{timestamp}.db";

        return await CreateBackupAsync(
            name: $"Pre-Restore Backup {DateTime.Now:yyyy-MM-dd HH:mm}",
            description: "Automatic backup created before restore operation",
            fileName: fileName,
            type: BackupType.PreRestore,
            isAutomaticDailyBackup: false,
            createdBy: createdBy
        );
    }

    private async Task<DatabaseBackup> CreateBackupAsync(
        string name,
        string? description,
        string fileName,
        BackupType type,
        bool isAutomaticDailyBackup,
        string? createdBy)
    {
        EnsureBackupDirectoryExists();

        var sourcePath = GetDatabasePath();
        var destPath = Path.Combine(BackupDirectory, fileName);

        _logger.LogInformation("Creating backup: {FileName} from {SourcePath}", fileName, sourcePath);

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Database file not found: {sourcePath}");
        }

        try
        {
            await _context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE);");
            _logger.LogDebug("WAL checkpoint completed before backup");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WAL checkpoint failed, continuing with backup");
        }

        await using (var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        await using (var destStream = new FileStream(destPath, FileMode.Create, FileAccess.Write))
        {
            await sourceStream.CopyToAsync(destStream);
        }

        var fileInfo = new FileInfo(destPath);

        var backup = new DatabaseBackup
        {
            Name = name,
            Description = description,
            FileName = fileName,
            FilePath = destPath,
            FileSizeBytes = fileInfo.Length,
            CreatedAt = DateTime.UtcNow,
            Type = type,
            IsAutomaticDailyBackup = isAutomaticDailyBackup,
            CreatedBy = createdBy
        };

        _context.DatabaseBackups.Add(backup);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Backup created successfully: {BackupId} - {FileName} ({Size} bytes)",
            backup.Id, fileName, backup.FileSizeBytes);

        return backup;
    }

    public async Task<List<DatabaseBackup>> GetAllBackupsAsync()
    {
        return await _context.DatabaseBackups
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<DatabaseBackup?> GetBackupByIdAsync(int id)
    {
        return await _context.DatabaseBackups.FindAsync(id);
    }

    public FileStream? GetBackupFileStream(DatabaseBackup backup)
    {
        if (!File.Exists(backup.FilePath))
        {
            _logger.LogWarning("Backup file not found: {FilePath}", backup.FilePath);
            return null;
        }

        return new FileStream(backup.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public async Task<bool> RestoreBackupAsync(int backupId, string? restoredBy)
    {
        var backup = await GetBackupByIdAsync(backupId);
        if (backup == null)
        {
            _logger.LogError("Backup not found: {BackupId}", backupId);
            return false;
        }

        if (!File.Exists(backup.FilePath))
        {
            _logger.LogError("Backup file not found: {FilePath}", backup.FilePath);
            return false;
        }

        var preRestoreBackup = await CreatePreRestoreBackupAsync(restoredBy);

        var allBackups = await _context.DatabaseBackups
            .AsNoTracking()
            .ToListAsync();

        var targetPath = GetDatabasePath();
        var walPath = targetPath + "-wal";
        var shmPath = targetPath + "-shm";

        _logger.LogInformation("Restoring backup {BackupId} from {SourcePath} to {TargetPath}",
            backupId, backup.FilePath, targetPath);

        try
        {
            _logger.LogInformation("Closing database connections before restore...");
            await _context.Database.CloseConnectionAsync();
            SqliteConnection.ClearAllPools();

            await Task.Delay(200);

            if (File.Exists(walPath))
            {
                File.Delete(walPath);
                _logger.LogInformation("Deleted WAL file before restore");
            }
            if (File.Exists(shmPath))
            {
                File.Delete(shmPath);
                _logger.LogInformation("Deleted SHM file before restore");
            }

            await using (var sourceStream = new FileStream(backup.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            await using (var destStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
            {
                await sourceStream.CopyToAsync(destStream);
            }

            _logger.LogInformation("Backup restored successfully: {BackupId}", backupId);

            _context.ChangeTracker.Clear();

            _logger.LogInformation("Preserving {Count} backup records in restored database...", allBackups.Count);

            var existingFileNames = await _context.DatabaseBackups
                .Select(b => b.FileName)
                .ToListAsync();

            var recordsToAdd = new List<DatabaseBackup>();
            foreach (var backupRecord in allBackups)
            {
                if (!existingFileNames.Contains(backupRecord.FileName))
                {
                    recordsToAdd.Add(new DatabaseBackup
                    {
                        Name = backupRecord.Name,
                        Description = backupRecord.Description,
                        FileName = backupRecord.FileName,
                        FilePath = backupRecord.FilePath,
                        FileSizeBytes = backupRecord.FileSizeBytes,
                        CreatedAt = backupRecord.CreatedAt,
                        Type = backupRecord.Type,
                        IsAutomaticDailyBackup = backupRecord.IsAutomaticDailyBackup,
                        CreatedBy = backupRecord.CreatedBy
                    });
                }
            }

            if (recordsToAdd.Any())
            {
                _context.DatabaseBackups.AddRange(recordsToAdd);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Added {Count} backup records to restored database", recordsToAdd.Count);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore backup {BackupId}", backupId);
            throw;
        }
    }

    public async Task<bool> DeleteBackupAsync(int backupId, bool deleteFile = true)
    {
        var backup = await GetBackupByIdAsync(backupId);
        if (backup == null)
        {
            return false;
        }

        if (deleteFile && File.Exists(backup.FilePath))
        {
            try
            {
                File.Delete(backup.FilePath);
                _logger.LogInformation("Deleted backup file: {FilePath}", backup.FilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete backup file: {FilePath}", backup.FilePath);
            }
        }

        _context.DatabaseBackups.Remove(backup);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted backup record: {BackupId}", backupId);
        return true;
    }

    public async Task<BackupStatistics> GetStatisticsAsync()
    {
        var backups = await _context.DatabaseBackups.ToListAsync();

        return new BackupStatistics
        {
            TotalBackups = backups.Count,
            TotalSizeBytes = backups.Sum(b => b.FileSizeBytes),
            AutomaticBackups = backups.Count(b => b.Type == BackupType.AutomaticDaily),
            ManualBackups = backups.Count(b => b.Type == BackupType.Manual),
            PreRestoreBackups = backups.Count(b => b.Type == BackupType.PreRestore),
            LastBackupDate = backups.OrderByDescending(b => b.CreatedAt).FirstOrDefault()?.CreatedAt,
            LastAutomaticBackupDate = backups.Where(b => b.IsAutomaticDailyBackup)
                .OrderByDescending(b => b.CreatedAt).FirstOrDefault()?.CreatedAt
        };
    }

    public bool BackupFileExists(DatabaseBackup backup)
    {
        return File.Exists(backup.FilePath);
    }

    public DatabaseFileInfo GetDatabaseFileInfo()
    {
        var dbPath = GetDatabasePath();
        var walPath = dbPath + "-wal";
        var shmPath = dbPath + "-shm";

        var info = new DatabaseFileInfo
        {
            DatabasePath = dbPath,
            WalPath = walPath,
            ShmPath = shmPath
        };

        if (File.Exists(dbPath))
        {
            var dbFile = new FileInfo(dbPath);
            info.DatabaseSize = dbFile.Length;
            info.DatabaseExists = true;
        }

        if (File.Exists(walPath))
        {
            var walFile = new FileInfo(walPath);
            info.WalSize = walFile.Length;
            info.WalExists = true;
        }

        if (File.Exists(shmPath))
        {
            var shmFile = new FileInfo(shmPath);
            info.ShmSize = shmFile.Length;
            info.ShmExists = true;
        }

        return info;
    }

    public async Task<WalCheckpointResult> ForceWalCheckpointAsync()
    {
        var result = new WalCheckpointResult();
        var dbPath = GetDatabasePath();
        var walPath = dbPath + "-wal";
        var shmPath = dbPath + "-shm";

        if (File.Exists(walPath))
        {
            result.WalSizeBefore = new FileInfo(walPath).Length;
        }
        if (File.Exists(shmPath))
        {
            result.ShmSizeBefore = new FileInfo(shmPath).Length;
        }
        if (File.Exists(dbPath))
        {
            result.DatabaseSizeBefore = new FileInfo(dbPath).Length;
        }

        try
        {
            _logger.LogInformation("Executing WAL checkpoint (TRUNCATE)...");
            await _context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE);");
            result.CheckpointSuccess = true;
            _logger.LogInformation("WAL checkpoint completed successfully");

            _logger.LogInformation("Switching to DELETE journal mode to remove WAL/SHM files...");
            await _context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=DELETE;");

            await _context.Database.CloseConnectionAsync();
            SqliteConnection.ClearAllPools();
            await Task.Delay(100);

            result.WalDeleted = !File.Exists(walPath);
            result.ShmDeleted = !File.Exists(shmPath);

            if (File.Exists(dbPath))
            {
                result.DatabaseSizeAfter = new FileInfo(dbPath).Length;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WAL checkpoint failed");
            result.CheckpointSuccess = false;
            result.CheckpointError = ex.Message;
        }

        return result;
    }
}

public class BackupStatistics
{
    public int TotalBackups { get; set; }
    public long TotalSizeBytes { get; set; }
    public int AutomaticBackups { get; set; }
    public int ManualBackups { get; set; }
    public int PreRestoreBackups { get; set; }
    public DateTime? LastBackupDate { get; set; }
    public DateTime? LastAutomaticBackupDate { get; set; }

    public string TotalSizeFormatted => FormatFileSize(TotalSizeBytes);

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

public class DatabaseFileInfo
{
    public string DatabasePath { get; set; } = string.Empty;
    public string WalPath { get; set; } = string.Empty;
    public string ShmPath { get; set; } = string.Empty;
    public long DatabaseSize { get; set; }
    public long WalSize { get; set; }
    public long ShmSize { get; set; }
    public bool DatabaseExists { get; set; }
    public bool WalExists { get; set; }
    public bool ShmExists { get; set; }
    public long TotalSize => DatabaseSize + WalSize + ShmSize;

    public string DatabaseSizeFormatted => FormatFileSize(DatabaseSize);
    public string WalSizeFormatted => FormatFileSize(WalSize);
    public string ShmSizeFormatted => FormatFileSize(ShmSize);
    public string TotalSizeFormatted => FormatFileSize(TotalSize);

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

public class WalCheckpointResult
{
    public bool CheckpointSuccess { get; set; }
    public string? CheckpointError { get; set; }
    public bool WalDeleted { get; set; }
    public string? WalDeleteError { get; set; }
    public bool ShmDeleted { get; set; }
    public string? ShmDeleteError { get; set; }
    public long WalSizeBefore { get; set; }
    public long ShmSizeBefore { get; set; }
    public long DatabaseSizeBefore { get; set; }
    public long DatabaseSizeAfter { get; set; }

    public bool FullSuccess => CheckpointSuccess && (WalDeleted || WalSizeBefore == 0) && (ShmDeleted || ShmSizeBefore == 0);
    public long SpaceReclaimed => WalSizeBefore + ShmSizeBefore;
    public string SpaceReclaimedFormatted => FormatFileSize(SpaceReclaimed);

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
