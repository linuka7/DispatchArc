using DispatchArc.Domain.Enums;

namespace DispatchArc.Application.TeamMembers;

public sealed record TeamMemberResponse(
    Guid Id,
    Guid TenantId,
    string FullName,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);