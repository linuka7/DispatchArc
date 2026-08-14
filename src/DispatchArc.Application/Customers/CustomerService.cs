using DispatchArc.Application.Tenants;
using DispatchArc.Domain.Entities;

namespace DispatchArc.Application.Customers;

public sealed class CustomerService
{
    private readonly ICustomerRepository _customers;
    private readonly ITenantRepository _tenants;

    public CustomerService(
        ICustomerRepository customers,
        ITenantRepository tenants)
    {
        _customers = customers;
        _tenants = tenants;
    }

    public async Task<CustomerResponse?> CreateAsync(
        Guid tenantId,
        string name,
        string phone,
        string? email,
        string? addressLine,
        string? city,
        CancellationToken cancellationToken)
    {
        var tenant = await _tenants.GetByIdAsync(
            tenantId,
            cancellationToken);

        if (tenant is null || !tenant.IsActive)
        {
            return null;
        }

        var customer = new Customer(
            tenantId,
            name,
            phone,
            email);

        customer.UpdateAddress(addressLine, city);

        await _customers.AddAsync(
            customer,
            cancellationToken);

        return Map(customer);
    }

    public async Task<CustomerResponse?> GetByIdAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(
            tenantId,
            customerId,
            cancellationToken);

        return customer is null ? null : Map(customer);
    }

    public async Task<IReadOnlyList<CustomerResponse>> GetAllAsync(
        Guid tenantId,
        string? search,
        CancellationToken cancellationToken)
    {
        var customers = await _customers.GetAllAsync(
            tenantId,
            search,
            cancellationToken);

        return customers.Select(Map).ToList();
    }

    private static CustomerResponse Map(Customer customer)
    {
        return new CustomerResponse(
            customer.Id,
            customer.TenantId,
            customer.Name,
            customer.Phone,
            customer.Email,
            customer.AddressLine,
            customer.City,
            customer.CreatedAtUtc,
            customer.UpdatedAtUtc);
    }
}
