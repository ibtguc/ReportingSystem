using Azure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;

namespace ReportingSystem.Services;

/// <summary>
/// Service for sending email notifications using Microsoft Graph API
/// </summary>
public class EmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;
    private GraphServiceClient? _graphClient;

    public EmailService(
        IOptions<EmailSettings> emailSettings,
        ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Gets or creates the Graph Service Client
    /// </summary>
    private GraphServiceClient GetGraphClient()
    {
        if (_graphClient == null)
        {
            var credential = new ClientSecretCredential(
                _emailSettings.TenantId,
                _emailSettings.ClientId,
                _emailSettings.ClientSecret);

            _graphClient = new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });
        }

        return _graphClient;
    }

    /// <summary>
    /// Send magic link login email
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> SendMagicLinkEmailAsync(
        string toEmail,
        string userName,
        string magicLinkUrl)
    {
        try
        {
            var subject = "Your Login Link - Reporting System";

            var body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2 style='color: #667eea;'>Login to Reporting System</h2>

    <p>Hello {userName},</p>

    <p>You requested a login link for the Reporting System. Click the button below to sign in:</p>

    <div style='margin: 30px 0;'>
        <a href='{magicLinkUrl}'
           style='background-color: #667eea; color: white; padding: 14px 28px; text-decoration: none; border-radius: 6px; display: inline-block; font-weight: bold;'>
            Sign In Now
        </a>
    </div>

    <p style='color: #666;'>Or copy and paste this link into your browser:</p>
    <p style='background-color: #f5f5f5; padding: 10px; border-radius: 4px; word-break: break-all; font-family: monospace; font-size: 0.9em;'>
        {magicLinkUrl}
    </p>

    <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee;'>
        <p style='color: #888; font-size: 0.9em;'>
            <strong>Security Notes:</strong>
        </p>
        <ul style='color: #888; font-size: 0.9em;'>
            <li>This link expires in 15 minutes</li>
            <li>This link can only be used once</li>
            <li>If you didn't request this link, please ignore this email</li>
        </ul>
    </div>

    <p style='margin-top: 30px;'>
        Best regards,<br/>
        System Administration
    </p>
</body>
</html>";

            return await SendEmailWithDetailsAsync(toEmail, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send magic link email to {Email}", toEmail);
            return (false, $"Failed to send magic link email: {GetFullExceptionMessage(ex)}");
        }
    }

    /// <summary>
    /// Send a general notification email
    /// </summary>
    public async Task<bool> SendNotificationEmailAsync(
        string toEmail,
        string recipientName,
        string notificationTitle,
        string notificationMessage,
        string? actionUrl = null)
    {
        try
        {
            var subject = $"Notification: {notificationTitle}";

            var actionButton = actionUrl != null
                ? $@"<div style='margin: 20px 0;'>
                        <a href='{actionUrl}' style='background-color: #0066cc; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px;'>
                            View Details
                        </a>
                     </div>"
                : "";

            var body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2 style='color: #0066cc;'>{notificationTitle}</h2>

    <p>Hello {recipientName},</p>

    <div style='background-color: #f5f5f5; padding: 15px; border-left: 4px solid #0066cc; margin: 20px 0;'>
        <p>{notificationMessage}</p>
    </div>

    {actionButton}

    <p>Best regards,<br/>
    Reporting System</p>
</body>
</html>";

            return await SendEmailAsync(toEmail, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification email to {Email}", toEmail);
            return false;
        }
    }

    /// <summary>
    /// Core email sending method using Microsoft Graph API
    /// </summary>
    private async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var (success, _) = await SendEmailWithDetailsAsync(toEmail, subject, htmlBody);
        return success;
    }

    /// <summary>
    /// Core email sending method using Microsoft Graph API with detailed error reporting
    /// </summary>
    private async Task<(bool Success, string? ErrorMessage)> SendEmailWithDetailsAsync(string toEmail, string subject, string htmlBody)
    {
        if (!_emailSettings.Enabled)
        {
            _logger.LogInformation("Email sending is disabled. Would have sent: {Subject} to {To}",
                subject, toEmail);
            return (false, "Email sending is disabled in the application configuration.");
        }

        try
        {
            var graphClient = GetGraphClient();

            var message = new Message
            {
                Subject = subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = htmlBody
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = toEmail
                        }
                    }
                },
                From = new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = _emailSettings.SenderEmail,
                        Name = _emailSettings.SenderName
                    }
                }
            };

            var sendMailRequest = new SendMailPostRequestBody
            {
                Message = message,
                SaveToSentItems = _emailSettings.SaveToSentItems
            };

            await graphClient.Users[_emailSettings.SenderUserId]
                .SendMail
                .PostAsync(sendMailRequest);

            _logger.LogInformation("Email sent successfully via Graph API to {Email}: {Subject}", toEmail, subject);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via Graph API to {Email}", toEmail);
            return (false, $"Failed to send email: {GetFullExceptionMessage(ex)}");
        }
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

/// <summary>
/// Email configuration settings for Microsoft Graph API
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// Whether email sending is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Azure AD Tenant ID
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Azure AD Application (Client) ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Azure AD Client Secret
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// The User ID (Object ID) or User Principal Name of the sender account
    /// </summary>
    public string SenderUserId { get; set; } = string.Empty;

    /// <summary>
    /// The email address of the sender (for display purposes)
    /// </summary>
    public string SenderEmail { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the sender
    /// </summary>
    public string SenderName { get; set; } = "System Administration";

    /// <summary>
    /// Whether to save sent emails to the Sent Items folder
    /// </summary>
    public bool SaveToSentItems { get; set; } = true;
}
