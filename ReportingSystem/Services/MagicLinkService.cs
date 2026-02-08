using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Services;

/// <summary>
/// Service for managing magic link authentication
/// </summary>
public class MagicLinkService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MagicLinkService> _logger;
    private readonly IWebHostEnvironment _environment;

    public MagicLinkService(
        ApplicationDbContext context,
        ILogger<MagicLinkService> logger,
        IWebHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Generate a magic link for a user
    /// </summary>
    public async Task<(bool Success, string? Token, string? MagicLinkUrl, string? UserName, string? ErrorMessage)> GenerateMagicLinkAsync(
        string email,
        string? ipAddress = null,
        string? userAgent = null)
    {
        try
        {
            // Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (user == null)
            {
                _logger.LogWarning("Magic link requested for non-existent email: {Email}", email);
                return (false, null, null, null, "No account found with this email address.");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Magic link requested for inactive user: {Email}", email);
                return (false, null, null, null, "This account is inactive. Please contact an administrator.");
            }

            // Generate secure token
            var token = GenerateSecureToken();

            // Create magic link record
            var magicLink = new MagicLink
            {
                UserId = user.Id,
                Token = token,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15), // 15 minute expiry
                IsUsed = false,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            _context.MagicLinks.Add(magicLink);
            await _context.SaveChangesAsync();

            // Build magic link URL
            var magicLinkUrl = $"/Auth/Verify?token={token}";

            _logger.LogInformation("Magic link generated for user {Email}", email);

            return (true, token, magicLinkUrl, user.Name, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating magic link for {Email}", email);
            return (false, null, null, null, $"An error occurred while generating the magic link: {GetFullExceptionMessage(ex)}");
        }
    }

    /// <summary>
    /// Verify and consume a magic link token
    /// </summary>
    public async Task<(bool Success, User? User, string? ErrorMessage)> VerifyMagicLinkAsync(
        string token,
        string? ipAddress = null)
    {
        try
        {
            var magicLink = await _context.MagicLinks
                .Include(ml => ml.User)
                .FirstOrDefaultAsync(ml => ml.Token == token);

            if (magicLink == null)
            {
                _logger.LogWarning("Invalid magic link token attempted: {Token}", token);
                return (false, null, "Invalid or expired link.");
            }

            // Check if already used
            if (magicLink.IsUsed)
            {
                _logger.LogWarning("Already used magic link token attempted: {Token}", token);
                return (false, null, "This link has already been used.");
            }

            // Check if expired
            if (magicLink.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Expired magic link token attempted: {Token}", token);
                return (false, null, "This link has expired. Please request a new one.");
            }

            // Check if user is still active
            if (!magicLink.User.IsActive)
            {
                _logger.LogWarning("Inactive user attempted login via magic link: {Email}", magicLink.User.Email);
                return (false, null, "This account is inactive.");
            }

            // Mark magic link as used
            magicLink.IsUsed = true;
            magicLink.UsedAt = DateTime.UtcNow;

            // Update user's last login
            magicLink.User.LastLoginAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {Email} successfully authenticated via magic link", magicLink.User.Email);

            return (true, magicLink.User, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying magic link token");
            return (false, null, $"An error occurred while verifying the magic link: {GetFullExceptionMessage(ex)}");
        }
    }

    /// <summary>
    /// Clean up expired magic links (should be run periodically)
    /// </summary>
    public async Task<int> CleanupExpiredLinksAsync()
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-7); // Remove links older than 7 days

            var expiredLinks = await _context.MagicLinks
                .Where(ml => ml.CreatedAt < cutoffDate)
                .ToListAsync();

            _context.MagicLinks.RemoveRange(expiredLinks);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} expired magic links", expiredLinks.Count);

            return expiredLinks.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired magic links");
            return 0;
        }
    }

    /// <summary>
    /// Generate a cryptographically secure random token
    /// </summary>
    private string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    /// <summary>
    /// Check if running in development environment
    /// </summary>
    public bool IsDevelopment()
    {
        return _environment.IsDevelopment();
    }

    /// <summary>
    /// Gets the full exception message including all inner exceptions
    /// </summary>
    private static string GetFullExceptionMessage(Exception ex)
    {
        var messages = new List<string>();
        var current = ex;

        while (current != null)
        {
            if (!string.IsNullOrWhiteSpace(current.Message))
            {
                messages.Add(current.Message);
            }
            current = current.InnerException;
        }

        return string.Join(" -> ", messages);
    }
}
