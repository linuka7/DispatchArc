using DispatchArc.Domain.Entities;

namespace DispatchArc.Application.Auth;

public interface IAppUserRepository
{
    Task<AppUser?> GetByEmailAsync(
        Guid tenantId,
        string email,
        CancellationToken cancellationToken = default);

    Task<AppUser?> GetByIdAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AppUser>> ListByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        AppUser user,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}