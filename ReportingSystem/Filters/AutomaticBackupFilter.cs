using Microsoft.AspNetCore.Mvc.Filters;
using ReportingSystem.Services;

namespace ReportingSystem.Filters;

/// <summary>
/// Action filter that ensures a daily backup exists before any data modification (POST/PUT/DELETE) operation.
/// This filter runs before the action is executed, ensuring the backup is created before any changes are made.
/// </summary>
public class AutomaticBackupFilter : IAsyncActionFilter
{
    private readonly ILogger<AutomaticBackupFilter> _logger;

    public AutomaticBackupFilter(ILogger<AutomaticBackupFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Only trigger backup check for POST, PUT, DELETE requests (data modifications)
        var method = context.HttpContext.Request.Method;
        if (method == "POST" || method == "PUT" || method == "DELETE")
        {
            try
            {
                var backupService = context.HttpContext.RequestServices.GetService<DatabaseBackupService>();
                if (backupService != null)
                {
                    var userName = context.HttpContext.User?.Identity?.Name ?? "Anonymous";

                    var backup = await backupService.EnsureDailyBackupAsync(userName);
                    if (backup != null)
                    {
                        _logger.LogInformation(
                            "Automatic daily backup created before {Method} request to {Path} by {User}",
                            method,
                            context.HttpContext.Request.Path,
                            userName);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't prevent the action from executing
                _logger.LogError(ex, "Error creating automatic daily backup");
            }
        }

        // Continue with the action
        await next();
    }
}
