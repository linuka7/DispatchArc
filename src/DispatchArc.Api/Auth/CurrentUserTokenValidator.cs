using System.Security.Claims;
using DispatchArc.Application.Auth;

namespace DispatchArc.Api.Auth;

public sealed class CurrentUserTokenValidator
{
    private readonly IAppUserRepository _users;

    public CurrentUserTokenValidator(
        IAppUserRepository users)
    {
        _users = users;
    }

    public async Task<string?> GetFailureReasonAsync(
        ClaimsPrincipal? principal,
        CancellationToken cancellationToken)
    {
        if (principal is null)
        {
            return "Authenticated user context is missing.";
        }

        var userIdValue =
            principal.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        var tenantIdValue =
            principal.FindFirst(
                "tenant_id")?.Value;

        var roleValue =
            principal.FindFirst(
                ClaimTypes.Role)?.Value;

        if (!Guid.TryParse(
                userIdValue,
                out var userId) ||
            !Guid.TryParse(
                tenantIdValue,
                out var tenantId) ||
            string.IsNullOrWhiteSpace(
                roleValue))
        {
            return "Token identity claims are invalid.";
        }

        var user =
            await _users.GetByIdAsync(
                tenantId,
                userId,
                cancellationToken);

        if (user is null)
        {
            return "The authenticated user no longer exists.";
        }

        if (!user.IsActive)
        {
            return "The authenticated user is inactive.";
        }

        if (!string.Equals(
                roleValue,
                user.Role.ToString(),
                StringComparison.Ordinal))
        {
            return "The authenticated user's role has changed.";
        }

        return null;
    }
}