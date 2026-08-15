using DispatchArc.Domain.Enums;

namespace DispatchArc.Api.Contracts.TeamMembers;

public sealed record CreateTeamMemberRequest(
    string FullName,
    string Email,
    string Password,
    UserRole Role);