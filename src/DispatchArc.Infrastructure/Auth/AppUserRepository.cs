using DispatchArc.Application.Auth;
using DispatchArc.Domain.Entities;
using DispatchArc.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DispatchArc.Infrastructure.Auth;

public sealed class AppUserRepository(
    DispatchArcDbContext database) : IAppUserRepository
{
    public Task<AppUser?> GetByEmailAsync(
        Guid tenantId,
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return database.Users.SingleOrDefaultAsync(
            user =>
                user.TenantId == tenantId &&
                user.Email == normalizedEmail,
            cancellationToken);
    }

    public async Task AddAsync(
        AppUser user,
        CancellationToken cancellationToken = default)
    {
        await database.Users.AddAsync(user, cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await database.SaveChangesAsync(cancellationToken);
    }
}