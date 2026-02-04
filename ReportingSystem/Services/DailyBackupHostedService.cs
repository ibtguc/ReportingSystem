namespace ReportingSystem.Services;

/// <summary>
/// Background service that ensures automatic backups are created twice daily.
/// Runs on application startup and then checks periodically.
/// </summary>
public class DailyBackupHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DailyBackupHostedService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour

    public DailyBackupHostedService(
        IServiceProvider serviceProvider,
        ILogger<DailyBackupHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Daily Backup Service started");

        // Initial delay to let the application fully start
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        // Check immediately on startup
        await EnsureDailyBackupAsync();

        // Then check periodically
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
                await EnsureDailyBackupAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in daily backup check loop");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Daily Backup Service stopped");
    }

    private async Task EnsureDailyBackupAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var backupService = scope.ServiceProvider.GetRequiredService<DatabaseBackupService>();

            if (await backupService.HasDailyBackupForTodayAsync())
            {
                _logger.LogDebug("Daily backup already exists for today");
                return;
            }

            _logger.LogInformation("Creating automatic daily backup...");
            var backup = await backupService.EnsureDailyBackupAsync("Scheduled Background Service");

            if (backup != null)
            {
                _logger.LogInformation(
                    "Automatic daily backup created successfully: {BackupName} ({Size})",
                    backup.Name,
                    FormatFileSize(backup.FileSizeBytes));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create automatic daily backup");
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
