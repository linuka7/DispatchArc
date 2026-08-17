using DispatchArc.Application.Alerts;
using DispatchArc.Application.Auth;
using DispatchArc.Application.Customers;
using DispatchArc.Application.Dashboard;
using DispatchArc.Application.Jobs;
using DispatchArc.Application.Invoices;
using DispatchArc.Application.Payments;
using DispatchArc.Application.Tenants;
using DispatchArc.Infrastructure.Auth;
using DispatchArc.Infrastructure.Persistence;
using DispatchArc.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DispatchArc.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        bool enableRetryOnFailure = false)
    {
        services.AddDbContext<DispatchArcDbContext>(
            options =>
                options.UseNpgsql(
                    connectionString,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.CommandTimeout(30);

                        if (enableRetryOnFailure)
                        {
                            npgsqlOptions.EnableRetryOnFailure(
                                maxRetryCount: 5,
                                maxRetryDelay:
                                    TimeSpan.FromSeconds(10),
                                errorCodesToAdd: null);
                        }
                    }));

        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IServiceJobRepository, ServiceJobRepository>();
        services.AddScoped<IJobNoteRepository, JobNoteRepository>();
        services.AddScoped<IJobLineItemRepository, JobLineItemRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IInvoiceLineItemRepository, InvoiceLineItemRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IOperationalAlertRepository, OperationalAlertRepository>();
        services.AddScoped<IAppUserRepository, AppUserRepository>();

        return services;
    }
}
