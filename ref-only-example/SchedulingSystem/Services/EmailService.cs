using Azure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;
using SchedulingSystem.Models;

namespace SchedulingSystem.Services;

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
    /// Send substitute assignment notification email
    /// </summary>
    public async Task<bool> SendSubstituteAssignmentEmailAsync(
        string toEmail,
        string substituteName,
        string originalTeacherName,
        string className,
        string subjectName,
        DayOfWeek dayOfWeek,
        string periodTime,
        string roomName,
        DateTime absenceDate,
        string? notes = null)
    {
        try
        {
            var subject = $"Substitute Assignment - {subjectName} {className}";

            var body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2 style='color: #0066cc;'>Substitute Teaching Assignment</h2>

    <p>Hello {substituteName},</p>

    <p>You have been assigned as a substitute teacher for the following lesson:</p>

    <div style='background-color: #f5f5f5; padding: 15px; border-left: 4px solid #0066cc;'>
        <p><strong>Date:</strong> {absenceDate:dddd, MMMM dd, yyyy}</p>
        <p><strong>Day:</strong> {dayOfWeek}</p>
        <p><strong>Period:</strong> {periodTime}</p>
        <p><strong>Subject:</strong> {subjectName}</p>
        <p><strong>Class:</strong> {className}</p>
        <p><strong>Room:</strong> {roomName}</p>
        <p><strong>Regular Teacher:</strong> {originalTeacherName}</p>
    </div>

    {(notes != null ? $"<p><strong>Notes:</strong> {notes}</p>" : "")}

    <p>Please arrive at the classroom a few minutes before the lesson starts.</p>

    <p>If you have any questions or concerns, please contact the administration office.</p>

    <p>Thank you,<br/>
    School Administration</p>
</body>
</html>";

            return await SendEmailAsync(toEmail, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send substitute assignment email to {Email}", toEmail);
            return false;
        }
    }

    /// <summary>
    /// Send absence confirmation email to teacher
    /// </summary>
    public async Task<bool> SendAbsenceConfirmationEmailAsync(
        string toEmail,
        string teacherName,
        DateTime absenceDate,
        AbsenceType absenceType,
        int affectedLessonsCount,
        int coveredLessonsCount)
    {
        try
        {
            var subject = $"Absence Confirmation - {absenceDate:MMMM dd, yyyy}";

            var coverageStatus = coveredLessonsCount == affectedLessonsCount
                ? "<span style='color: green;'>All lessons covered</span>"
                : $"<span style='color: orange;'>{coveredLessonsCount} of {affectedLessonsCount} lessons covered</span>";

            var body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2 style='color: #0066cc;'>Absence Confirmation</h2>

    <p>Hello {teacherName},</p>

    <p>Your absence has been recorded and is being processed:</p>

    <div style='background-color: #f5f5f5; padding: 15px; border-left: 4px solid #0066cc;'>
        <p><strong>Date:</strong> {absenceDate:dddd, MMMM dd, yyyy}</p>
        <p><strong>Type:</strong> {absenceType}</p>
        <p><strong>Affected Lessons:</strong> {affectedLessonsCount}</p>
        <p><strong>Coverage Status:</strong> {coverageStatus}</p>
    </div>

    <p>We are working on arranging substitute teachers for your lessons.</p>

    <p>Get well soon!</p>

    <p>Best regards,<br/>
    School Administration</p>
</body>
</html>";

            return await SendEmailAsync(toEmail, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send absence confirmation email to {Email}", toEmail);
            return false;
        }
    }

    /// <summary>
    /// Send daily substitution summary to administrators
    /// </summary>
    public async Task<bool> SendDailySubstitutionSummaryAsync(
        string toEmail,
        DateTime date,
        int totalAbsences,
        int totalSubstitutions,
        int uncoveredLessons,
        List<(string TeacherName, int LessonsCount)> absentTeachers)
    {
        try
        {
            var subject = $"Daily Substitution Summary - {date:MMMM dd, yyyy}";

            var teacherList = string.Join("", absentTeachers.Select(t =>
                $"<li>{t.TeacherName} - {t.LessonsCount} lesson(s)</li>"));

            var body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2 style='color: #0066cc;'>Daily Substitution Summary</h2>

    <p><strong>Date:</strong> {date:dddd, MMMM dd, yyyy}</p>

    <div style='background-color: #f5f5f5; padding: 15px; margin: 20px 0;'>
        <h3>Overview</h3>
        <p><strong>Total Absences:</strong> {totalAbsences}</p>
        <p><strong>Total Substitutions:</strong> {totalSubstitutions}</p>
        <p><strong>Uncovered Lessons:</strong>
            <span style='color: {(uncoveredLessons > 0 ? "red" : "green")};'>{uncoveredLessons}</span>
        </p>
    </div>

    {(absentTeachers.Any() ? $@"
    <div>
        <h3>Absent Teachers</h3>
        <ul>
            {teacherList}
        </ul>
    </div>" : "")}

    <p>Please review the substitution board for full details.</p>

    <p>School Administration System</p>
</body>
</html>";

            return await SendEmailAsync(toEmail, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send daily substitution summary to {Email}", toEmail);
            return false;
        }
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
            var subject = "Your Login Link - Scheduling System";

            var body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2 style='color: #667eea;'>Login to Scheduling System</h2>

    <p>Hello {userName},</p>

    <p>You requested a login link for the Scheduling System. Click the button below to sign in:</p>

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
        School Administration
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
    /// This is the mailbox from which emails will be sent
    /// </summary>
    public string SenderUserId { get; set; } = string.Empty;

    /// <summary>
    /// The email address of the sender (for display purposes)
    /// </summary>
    public string SenderEmail { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the sender
    /// </summary>
    public string SenderName { get; set; } = "School Administration";

    /// <summary>
    /// Whether to save sent emails to the Sent Items folder
    /// </summary>
    public bool SaveToSentItems { get; set; } = true;
}
