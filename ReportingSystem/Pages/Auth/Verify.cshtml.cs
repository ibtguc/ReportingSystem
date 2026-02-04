using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Auth;

public class VerifyModel : PageModel
{
    private readonly MagicLinkService _magicLinkService;
    private readonly ILogger<VerifyModel> _logger;

    public VerifyModel(
        MagicLinkService magicLinkService,
        ILogger<VerifyModel> logger)
    {
        _magicLinkService = magicLinkService;
        _logger = logger;
    }

    public string? ErrorMessage { get; set; }
    public bool IsVerifying { get; set; } = true;

    public async Task<IActionResult> OnGetAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            ErrorMessage = "Invalid login link. Please request a new one.";
            IsVerifying = false;
            return Page();
        }

        // Get IP address
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        // Verify the magic link
        var (success, user, errorMessage) = await _magicLinkService.VerifyMagicLinkAsync(token, ipAddress);

        if (!success || user == null)
        {
            ErrorMessage = errorMessage;
            IsVerifying = false;
            return Page();
        }

        // Create claims for the authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        // Sign in the user
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            claimsPrincipal,
            new AuthenticationProperties
            {
                IsPersistent = true, // Remember me
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) // 30-day session
            });

        _logger.LogInformation("User {Email} successfully logged in", user.Email);

        // Redirect to dashboard
        return RedirectToPage("/Admin/Dashboard");
    }
}
