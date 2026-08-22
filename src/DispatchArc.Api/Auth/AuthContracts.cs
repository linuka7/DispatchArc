using DispatchArc.Domain.Enums;

namespace DispatchArc.Api.Auth;

public sealed record RegisterRequest(
    Guid TenantId,
    string FullName,
    string Email,
    string Password);

public sealed record LoginRequest(
    string Email,
    string Password);

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    Guid UserId,
    Guid TenantId,
    string FullName,
    string Email,
    UserRole Role);