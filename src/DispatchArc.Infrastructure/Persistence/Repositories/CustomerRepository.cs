using DispatchArc.Application.Customers;
using DispatchArc.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DispatchArc.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly DispatchArcDbContext _database;

    public CustomerRepository(DispatchArcDbContext database)
    {
        _database = database;
    }

    public async Task AddAsync(
        Customer customer,
        CancellationToken cancellationToken)
    {
        await _database.Customers.AddAsync(
            customer,
            cancellationToken);

        await _database.SaveChangesAsync(cancellationToken);
    }

    public Task<Customer?> GetByIdAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return _database.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(
                customer =>
                    customer.TenantId == tenantId &&
                    customer.Id == customerId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync(
        Guid tenantId,
        string? search,
        CancellationToken cancellationToken)
    {
        var query = _database.Customers
            .AsNoTracking()
            .Where(customer => customer.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";

            query = query.Where(customer =>
                EF.Functions.ILike(customer.Name, pattern) ||
                EF.Functions.ILike(customer.Phone, pattern) ||
                (
                    customer.Email != null &&
                    EF.Functions.ILike(customer.Email, pattern)
                ));
        }

        return await query
            .OrderBy(customer => customer.Name)
            .ToListAsync(cancellationToken);
    }
}
