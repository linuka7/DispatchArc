using DispatchArc.Domain.Entities;

namespace DispatchArc.Application.Tenants;

public interface ITenantRepository
{
    Task<bool> SlugExistsAsync(
        string slug,
        CancellationToken cancellationToken);

    Task<Tenant?> GetByIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task AddAsync(
        Tenant tenant,
        CancellationToken cancellationToken);
}