using DispatchArc.Application.Auth;
using DispatchArc.Domain.Entities;

namespace DispatchArc.Application.TeamMembers;

public sealed class TeamMemberService(
    IAppUserRepository users)
{
    public async Task<IReadOnlyList<TeamMemberResponse>> ListAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var teamMembers = await users.ListByTenantAsync(
            tenantId,
            cancellationToken);

        return teamMembers
            .Select(ToResponse)
            .ToList();
    }

    public async Task<TeamMemberResponse?> GetByIdAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var teamMember = await users.GetByIdAsync(
            tenantId,
            userId,
            cancellationToken);

        return teamMember is null
            ? null
            : ToResponse(teamMember);
    }

    private static TeamMemberResponse ToResponse(AppUser user)
    {
        return new TeamMemberResponse(
            user.Id,
            user.TenantId,
            user.FullName,
            user.Email,
            user.Role,
            user.IsActive,
            user.CreatedAtUtc,
            user.UpdatedAtUtc);
    }
}