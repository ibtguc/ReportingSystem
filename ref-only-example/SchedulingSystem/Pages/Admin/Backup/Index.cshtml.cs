using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SchedulingSystem.Models;
using SchedulingSystem.Services;
using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Pages.Admin.Backup;

public class IndexModel : PageModel
{
    private readonly DatabaseBackupService _backupService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(DatabaseBackupService backupService, ILogger<IndexModel> logger)
    {
        _backupService = backupService;
        _logger = logger;
    }

    public List<DatabaseBackup> Backups { get; set; } = new();
    public BackupStatistics Statistics { get; set; } = new();
    public DatabaseFileInfo DatabaseInfo { get; set; } = new();

    [BindProperty]
    public CreateBackupInput CreateInput { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        Backups = await _backupService.GetAllBackupsAsync();
        Statistics = await _backupService.GetStatisticsAsync();
        DatabaseInfo = _backupService.GetDatabaseFileInfo();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(CreateInput.Name))
        {
            ErrorMessage = "Backup name is required.";
            return RedirectToPage();
        }

        try
        {
            var userName = User?.Identity?.Name ?? "Unknown";
            var backup = await _backupService.CreateManualBackupAsync(
                CreateInput.Name,
                CreateInput.Description,
                userName
            );

            SuccessMessage = $"Backup '{backup.Name}' created successfully.";

            // If download is requested, return the file directly
            if (CreateInput.DownloadAfterCreate)
            {
                var stream = _backupService.GetBackupFileStream(backup);
                if (stream != null)
                {
                    return File(stream, "application/octet-stream", backup.FileName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup");
            ErrorMessage = $"Failed to create backup: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateAjaxAsync([FromBody] CreateBackupAjaxRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return new JsonResult(new { success = false, error = "Backup name is required." }) { StatusCode = 400 };
        }

        try
        {
            var userName = User?.Identity?.Name ?? "Unknown";
            var backup = await _backupService.CreateManualBackupAsync(
                request.Name,
                request.Description,
                userName
            );

            return new JsonResult(new {
                success = true,
                backupId = backup.Id,
                backupName = backup.Name,
                message = $"Backup '{backup.Name}' created successfully."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup");
            return new JsonResult(new { success = false, error = ex.Message }) { StatusCode = 500 };
        }
    }

    public async Task<IActionResult> OnGetDownloadAsync(int id)
    {
        var backup = await _backupService.GetBackupByIdAsync(id);
        if (backup == null)
        {
            ErrorMessage = "Backup not found.";
            return RedirectToPage();
        }

        var stream = _backupService.GetBackupFileStream(backup);
        if (stream == null)
        {
            ErrorMessage = "Backup file not found on disk.";
            return RedirectToPage();
        }

        return File(stream, "application/octet-stream", backup.FileName);
    }

    public async Task<IActionResult> OnPostRestoreAsync(int id)
    {
        try
        {
            var backup = await _backupService.GetBackupByIdAsync(id);
            if (backup == null)
            {
                ErrorMessage = "Backup not found.";
                return RedirectToPage();
            }

            var userName = User?.Identity?.Name ?? "Unknown";
            var success = await _backupService.RestoreBackupAsync(id, userName);

            if (success)
            {
                SuccessMessage = $"Database restored from backup '{backup.Name}'. A pre-restore backup was created. Please restart the application for changes to take full effect.";
            }
            else
            {
                ErrorMessage = "Failed to restore backup.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore backup {BackupId}", id);
            ErrorMessage = $"Failed to restore backup: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            var backup = await _backupService.GetBackupByIdAsync(id);
            if (backup == null)
            {
                ErrorMessage = "Backup not found.";
                return RedirectToPage();
            }

            var backupName = backup.Name;
            var success = await _backupService.DeleteBackupAsync(id);

            if (success)
            {
                SuccessMessage = $"Backup '{backupName}' deleted successfully.";
            }
            else
            {
                ErrorMessage = "Failed to delete backup.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete backup {BackupId}", id);
            ErrorMessage = $"Failed to delete backup: {ex.Message}";
        }

        return RedirectToPage();
    }

    public bool BackupFileExists(DatabaseBackup backup)
    {
        return _backupService.BackupFileExists(backup);
    }

    public async Task<IActionResult> OnPostWalCheckpointAsync()
    {
        try
        {
            var result = await _backupService.ForceWalCheckpointAsync();

            if (result.FullSuccess)
            {
                SuccessMessage = $"Database cleanup completed successfully! " +
                    $"All changes written to main database and WAL/SHM files removed. " +
                    $"Space reclaimed: {result.SpaceReclaimedFormatted}. " +
                    $"Database is now in DELETE journal mode (will return to WAL on app restart).";
            }
            else if (result.CheckpointSuccess)
            {
                // Checkpoint succeeded but some files couldn't be deleted
                var issues = new List<string>();
                if (!result.WalDeleted && !string.IsNullOrEmpty(result.WalDeleteError))
                    issues.Add($"WAL: {result.WalDeleteError}");
                else if (!result.WalDeleted)
                    issues.Add("WAL file could not be removed");

                if (!result.ShmDeleted && !string.IsNullOrEmpty(result.ShmDeleteError))
                    issues.Add($"SHM: {result.ShmDeleteError}");
                else if (!result.ShmDeleted)
                    issues.Add("SHM file could not be removed");

                if (issues.Any())
                {
                    SuccessMessage = $"Checkpoint completed - all changes written to main database. " +
                        $"Some files could not be removed: {string.Join("; ", issues)}. " +
                        $"Try restarting the application if this persists.";
                }
                else
                {
                    SuccessMessage = $"Database cleanup completed successfully! " +
                        $"All changes written and temporary files removed. " +
                        $"Database is now in DELETE journal mode.";
                }
            }
            else
            {
                ErrorMessage = $"Checkpoint failed: {result.CheckpointError}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WAL checkpoint operation failed");
            ErrorMessage = $"Checkpoint operation failed: {ex.Message}";
        }

        return RedirectToPage();
    }

    public string FormatFileSize(long bytes)
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

    public string GetBackupTypeBadgeClass(BackupType type)
    {
        return type switch
        {
            BackupType.Manual => "bg-primary",
            BackupType.AutomaticDaily => "bg-success",
            BackupType.PreRestore => "bg-warning text-dark",
            _ => "bg-secondary"
        };
    }
}

public class CreateBackupInput
{
    [Required(ErrorMessage = "Backup name is required")]
    [StringLength(200, ErrorMessage = "Name must be less than 200 characters")]
    [Display(Name = "Backup Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description must be less than 500 characters")]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Download after creating")]
    public bool DownloadAfterCreate { get; set; } = true;
}

public class CreateBackupAjaxRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
