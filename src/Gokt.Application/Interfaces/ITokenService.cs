using Gokt.Domain.Entities;

namespace Gokt.Application.Interfaces;

public interface ITokenService
{
    (string Token, DateTime Expiry) GenerateAccessToken(User user);
    (string RawToken, DateTime Expiry) GenerateRefreshToken();
    string GenerateEmailVerificationCode();
    string GenerateSecureToken();
    string HashToken(string rawToken);
}
