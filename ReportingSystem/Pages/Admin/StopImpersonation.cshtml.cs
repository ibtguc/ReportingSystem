using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Data;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Admin;

public class StopImpersonationModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly AuditService _auditService;
    private readonly IWebHostEnvironment _env;

    public StopImpersonationModel(ApplicationDbContext context, AuditService auditService, IWebHostEnvironment env)
    {
        _context = context;
        _auditService = auditService;
        _env = env;
    }

    public IActionResult OnGet() => RedirectToPage("/Dashboard/Index");

    public async Task<IActionResult> OnPostAsync()
    {
        // Impersonation is only available in development
        if (!_env.IsDevelopment())
            return NotFound();

        var originalUserIdStr = User.FindFirst("OriginalUserId")?.Value;
        if (string.IsNullOrEmpty(originalUserIdStr) || !int.TryParse(originalUserIdStr, out var originalUserId))
            return RedirectToPage("/Dashboard/Index");

        var adminUser = await _context.Users.FindAsync(originalUserId);
        if (adminUser == null)
            return RedirectToPage("/Dashboard/Index");

        var impersonatedName = User.Identity?.Name;

        // Restore the original admin session
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, adminUser.Id.ToString()),
            new Claim(ClaimTypes.Name, adminUser.Name),
            new Claim(ClaimTypes.Email, adminUser.Email),
            new Claim(ClaimTypes.Role, adminUser.SystemRole.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) });

        // Audit
        await _auditService.LogAsync(
            AuditActionType.Login,
            "User",
            itemId: adminUser.Id,
            itemTitle: adminUser.Name,
            userId: adminUser.Id,
            userName: adminUser.Name,
            details: $"Admin stopped impersonating user {impersonatedName}");

        return RedirectToPage("/Dashboard/Index");
    }
}
