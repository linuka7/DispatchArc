using DispatchArc.Domain.Entities;

namespace DispatchArc.Application.Tenants;

public sealed class TenantService
{
    private readonly ITenantRepository _repository;

    public TenantService(ITenantRepository repository)
    {
        _repository = repository;
    }

    public async Task<TenantResponse?> CreateAsync(
        string name,
        string slug,
        CancellationToken cancellationToken)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();

        if (await _repository.SlugExistsAsync(
                normalizedSlug,
                cancellationToken))
        {
            return null;
        }

        var tenant = new Tenant(name, normalizedSlug);

        await _repository.AddAsync(tenant, cancellationToken);

        return Map(tenant);
    }

    public async Task<TenantResponse?> GetByIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var tenant = await _repository.GetByIdAsync(
            tenantId,
            cancellationToken);

        return tenant is null ? null : Map(tenant);
    }

    private static TenantResponse Map(Tenant tenant)
    {
        return new TenantResponse(
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.IsActive,
            tenant.CreatedAtUtc);
    }
}