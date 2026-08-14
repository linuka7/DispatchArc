using DispatchArc.Domain.Entities;

namespace DispatchArc.Application.Customers;

public interface ICustomerRepository
{
    Task AddAsync(
        Customer customer,
        CancellationToken cancellationToken);

    Task<Customer?> GetByIdAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Customer>> GetAllAsync(
        Guid tenantId,
        string? search,
        CancellationToken cancellationToken);
}
