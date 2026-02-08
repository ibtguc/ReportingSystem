using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Auth;

public class LogoutModel : PageModel
{
    private readonly AuditService _auditService;
    private readonly ILogger<LogoutModel> _logger;

    public LogoutModel(AuditService auditService, ILogger<LogoutModel> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userEmail = User.Identity?.Name ?? "Unknown";
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(userIdStr, out var userId);

        // Audit log before sign out (while we still have user context)
        await _auditService.LogAsync(
            AuditActionType.Logout,
            "User",
            userId,
            userEmail,
            userId,
            userEmail);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        _logger.LogInformation("User {Email} logged out", userEmail);

        return RedirectToPage("/Auth/Login");
    }
}
