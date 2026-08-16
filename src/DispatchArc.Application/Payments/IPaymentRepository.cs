using DispatchArc.Domain.Entities;

namespace DispatchArc.Application.Payments;

public interface IPaymentRepository
{
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken);

    Task AddAsync(
        Payment payment,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Payment>> GetByInvoiceAsync(
        Guid tenantId,
        Guid invoiceId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}