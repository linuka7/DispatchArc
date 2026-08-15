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

    public Task<AppUser?> GetByIdAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return database.Users.SingleOrDefaultAsync(
            user =>
                user.TenantId == tenantId &&
                user.Id == userId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<AppUser>> ListByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await database.Users
            .AsNoTracking()
            .Where(user => user.TenantId == tenantId)
            .OrderBy(user => user.FullName)
            .ToListAsync(cancellationToken);
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