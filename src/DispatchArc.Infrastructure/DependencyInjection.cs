using DispatchArc.Application.Customers;
using DispatchArc.Application.Jobs;
using DispatchArc.Application.Tenants;
using DispatchArc.Infrastructure.Persistence;
using DispatchArc.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DispatchArc.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<DispatchArcDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IServiceJobRepository, ServiceJobRepository>();

        return services;
    }
}
