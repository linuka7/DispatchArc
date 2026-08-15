using DispatchArc.Domain.Entities;

namespace DispatchArc.Application.Auth;

public interface IAppUserRepository
{
    Task<AppUser?> GetByEmailAsync(
        Guid tenantId,
        string email,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        AppUser user,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}