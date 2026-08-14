using DispatchArc.Application.Tenants;
using DispatchArc.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DispatchArc.Infrastructure.Persistence.Repositories;

public sealed class TenantRepository : ITenantRepository
{
    private readonly DispatchArcDbContext _database;

    public TenantRepository(DispatchArcDbContext database)
    {
        _database = database;
    }

    public Task<bool> SlugExistsAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        return _database.Tenants.AnyAsync(
            tenant => tenant.Slug == slug,
            cancellationToken);
    }

    public Task<Tenant?> GetByIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return _database.Tenants
            .AsNoTracking()
            .SingleOrDefaultAsync(
                tenant => tenant.Id == tenantId,
                cancellationToken);
    }

    public async Task AddAsync(
        Tenant tenant,
        CancellationToken cancellationToken)
    {
        await _database.Tenants.AddAsync(
            tenant,
            cancellationToken);

        await _database.SaveChangesAsync(cancellationToken);
    }
}