namespace Gokt.Application.Interfaces;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string toEmail, string rawToken, Guid userId, CancellationToken ct = default);
    Task SendPasswordResetEmailAsync(string toEmail, string rawToken, CancellationToken ct = default);
    Task SendWelcomeEmailAsync(string toEmail, string firstName, CancellationToken ct = default);
}
