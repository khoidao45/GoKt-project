using Gokt.Domain.Entities;
using Gokt.Domain.Enums;

namespace Gokt.Application.DTOs;

public record UserDto(
    Guid Id,
    string Email,
    string? Phone,
    bool EmailVerified,
    UserStatus Status,
    string? FirstName,
    string? LastName,
    string? AvatarUrl,
    IEnumerable<string> Roles,
    DateTime CreatedAt
)
{
    public static UserDto From(User user) => new(
        user.Id,
        user.Email,
        user.Phone,
        user.EmailVerified,
        user.Status,
        user.Profile?.FirstName,
        user.Profile?.LastName,
        user.Profile?.AvatarUrl,
        user.GetRoleNames(),
        user.CreatedAt
    );
}

public record AuthTokensDto(
    string AccessToken,
    DateTime AccessTokenExpiry,
    string RefreshToken,
    DateTime RefreshTokenExpiry,
    UserDto User
);

public record SessionDto(
    Guid Id,
    string? IpAddress,
    DateTime CreatedAt,
    DateTime LastUsedAt,
    DateTime ExpiresAt,
    bool IsCurrent
);
