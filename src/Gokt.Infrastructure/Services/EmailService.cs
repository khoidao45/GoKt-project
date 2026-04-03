using Gokt.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Gokt.Infrastructure.Services;

public class EmailService(IConfiguration configuration, ILogger<EmailService> logger) : IEmailService
{
    private readonly string? _apiKey = configuration["SendGrid:ApiKey"];
    private readonly string _fromEmail = configuration["SendGrid:FromEmail"] ?? "noreply@gokt.app";
    private readonly string _fromName = configuration["SendGrid:FromName"] ?? "Gokt";
    private readonly string _appUrl = configuration["App:BaseUrl"] ?? "http://localhost:5042";

    public async Task SendVerificationEmailAsync(string toEmail, string rawToken, Guid userId, CancellationToken ct = default)
    {
        var link = $"{_appUrl}/api/v1/auth/verify-email?userId={userId}&token={Uri.EscapeDataString(rawToken)}";
        var subject = "Verify your Gokt account";
        var body = $"""
            <h2>Welcome to Gokt!</h2>
            <p>Please verify your email address by clicking the link below:</p>
            <p><a href="{link}" style="background:#4CAF50;color:white;padding:12px 24px;text-decoration:none;border-radius:4px;">Verify Email</a></p>
            <p>This link expires in 10 minutes.</p>
            <p>If you did not create an account, please ignore this email.</p>
            """;

        await SendEmailAsync(toEmail, subject, body, ct);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string rawToken, CancellationToken ct = default)
    {
        var link = $"{_appUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";
        var subject = "Reset your Gokt password";
        var body = $"""
            <h2>Password Reset Request</h2>
            <p>We received a request to reset your password. Click the button below to reset it:</p>
            <p><a href="{link}" style="background:#2196F3;color:white;padding:12px 24px;text-decoration:none;border-radius:4px;">Reset Password</a></p>
            <p>This link expires in 30 minutes.</p>
            <p>If you did not request a password reset, please ignore this email and your password will remain unchanged.</p>
            """;

        await SendEmailAsync(toEmail, subject, body, ct);
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string firstName, CancellationToken ct = default)
    {
        var subject = "Welcome to Gokt!";
        var body = $"""
            <h2>Welcome aboard, {firstName}!</h2>
            <p>Your account is verified and ready to use.</p>
            <p>Start by booking your first ride or signing up as a driver.</p>
            """;

        await SendEmailAsync(toEmail, subject, body, ct);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            // Development fallback: log instead of sending
            logger.LogInformation("[EMAIL] To: {Email} | Subject: {Subject}", toEmail, subject);
            return;
        }

        try
        {
            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlBody);
            var response = await client.SendEmailAsync(msg, ct);

            if (!response.IsSuccessStatusCode)
                logger.LogWarning("SendGrid returned {Status} for {Email}", response.StatusCode, toEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Email}", toEmail);
        }
    }
}
