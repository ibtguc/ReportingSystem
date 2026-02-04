using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SchedulingSystem.Services;

namespace SchedulingSystem.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly MagicLinkService _magicLinkService;
    private readonly EmailService _emailService;
    private readonly ILogger<LoginModel> _logger;
    private readonly IWebHostEnvironment _environment;

    public LoginModel(
        MagicLinkService magicLinkService,
        EmailService emailService,
        ILogger<LoginModel> logger,
        IWebHostEnvironment environment)
    {
        _magicLinkService = magicLinkService;
        _emailService = emailService;
        _logger = logger;
        _environment = environment;
    }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    public string? MagicLinkUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public string? EmailErrorMessage { get; set; }
    public bool ShowMagicLink { get; set; }
    public bool EmailSent { get; set; }
    public bool IsDevelopment => _environment.IsDevelopment();

    public void OnGet()
    {
        // Check if already authenticated
        if (User.Identity?.IsAuthenticated == true)
        {
            Response.Redirect("/Admin/Dashboard");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Please enter your email address.";
            return Page();
        }

        // Get IP address and user agent
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

        // Generate magic link
        var (success, token, magicLinkUrl, userName, errorMessage) = await _magicLinkService.GenerateMagicLinkAsync(
            Email,
            ipAddress,
            userAgent);

        if (!success)
        {
            ErrorMessage = errorMessage;
            return Page();
        }

        // Build full URL
        var fullMagicLinkUrl = $"{Request.Scheme}://{Request.Host}{magicLinkUrl}";

        // Send magic link email
        var (emailSent, emailError) = await _emailService.SendMagicLinkEmailAsync(
            Email,
            userName ?? Email,
            fullMagicLinkUrl);

        EmailSent = emailSent;

        if (emailSent)
        {
            _logger.LogInformation("Magic link email sent to {Email}", Email);
        }
        else
        {
            _logger.LogWarning("Failed to send magic link email to {Email}: {Error}", Email, emailError);
            EmailErrorMessage = emailError;
        }

        // Only display magic link on page in development environment
        if (_environment.IsDevelopment())
        {
            MagicLinkUrl = fullMagicLinkUrl;
            ShowMagicLink = true;
            _logger.LogWarning("⚠️ DEVELOPMENT: Magic link displayed on page for {Email}: {Url}", Email, MagicLinkUrl);
        }

        return Page();
    }
}
