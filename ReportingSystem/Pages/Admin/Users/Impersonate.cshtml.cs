using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Data;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Admin.Users;

public class ImpersonateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly AuditService _auditService;
    private readonly IWebHostEnvironment _env;

    public ImpersonateModel(ApplicationDbContext context, AuditService auditService, IWebHostEnvironment env)
    {
        _context = context;
        _auditService = auditService;
        _env = env;
    }

    public IActionResult OnGet() => RedirectToPage("Index");

    public async Task<IActionResult> OnPostAsync(int id)
    {
        // Impersonation is only available in development
        if (!_env.IsDevelopment())
            return NotFound();

        // Only SystemAdmin can impersonate
        var currentRole = User.FindFirst(ClaimTypes.Role)?.Value;
        if (currentRole != nameof(SystemRole.SystemAdmin))
            return Forbid();

        // Prevent impersonating while already impersonating
        if (User.FindFirst("OriginalUserId") != null)
            return RedirectToPage("Index");

        var targetUser = await _context.Users.FindAsync(id);
        if (targetUser == null)
            return NotFound();

        // Don't impersonate yourself
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId == id.ToString())
            return RedirectToPage("Details", new { id });

        // Build claims for the target user, carrying the original admin identity
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, targetUser.Id.ToString()),
            new Claim(ClaimTypes.Name, targetUser.Name),
            new Claim(ClaimTypes.Email, targetUser.Email),
            new Claim(ClaimTypes.Role, targetUser.SystemRole.ToString()),
            new Claim("OriginalUserId", currentUserId!),
            new Claim("OriginalUserName", User.Identity?.Name ?? "Admin")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(4) });

        // Audit the impersonation
        await _auditService.LogAsync(
            AuditActionType.Login,
            "User",
            itemId: targetUser.Id,
            itemTitle: targetUser.Name,
            userId: int.Parse(currentUserId!),
            userName: User.Identity?.Name,
            details: $"Admin impersonated user {targetUser.Name} ({targetUser.Email})");

        return RedirectToPage("/Dashboard/Index");
    }
}
