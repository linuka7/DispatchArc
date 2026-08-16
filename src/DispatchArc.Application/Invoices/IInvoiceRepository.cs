using DispatchArc.Domain.Entities;

namespace DispatchArc.Application.Invoices;

public interface IInvoiceRepository
{
    Task AddAsync(
        Invoice invoice,
        CancellationToken cancellationToken);

    Task<Invoice?> GetByIdAsync(
        Guid tenantId,
        Guid invoiceId,
        CancellationToken cancellationToken);

    Task<Invoice?> GetForUpdateAsync(
        Guid tenantId,
        Guid invoiceId,
        CancellationToken cancellationToken);
    Task<Invoice?> GetByJobAsync(
        Guid tenantId,
        Guid serviceJobId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}