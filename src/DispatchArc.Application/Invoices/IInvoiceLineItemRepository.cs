using DispatchArc.Domain.Entities;

namespace DispatchArc.Application.Invoices;

public interface IInvoiceLineItemRepository
{
    Task AddRangeAsync(
        IReadOnlyCollection<InvoiceLineItem> items,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<InvoiceLineItem>> GetByInvoiceAsync(
        Guid tenantId,
        Guid invoiceId,
        CancellationToken cancellationToken);
}