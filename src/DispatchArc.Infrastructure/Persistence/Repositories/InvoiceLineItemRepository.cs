using DispatchArc.Application.Invoices;
using DispatchArc.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DispatchArc.Infrastructure.Persistence.Repositories;

public sealed class InvoiceLineItemRepository
    : IInvoiceLineItemRepository
{
    private readonly DispatchArcDbContext _database;

    public InvoiceLineItemRepository(
        DispatchArcDbContext database)
    {
        _database = database;
    }

    public async Task AddRangeAsync(
        IReadOnlyCollection<InvoiceLineItem> items,
        CancellationToken cancellationToken)
    {
        await _database.InvoiceLineItems.AddRangeAsync(
            items,
            cancellationToken);
    }

    public async Task<IReadOnlyList<InvoiceLineItem>>
        GetByInvoiceAsync(
            Guid tenantId,
            Guid invoiceId,
            CancellationToken cancellationToken)
    {
        return await _database.InvoiceLineItems
            .AsNoTracking()
            .Where(item =>
                item.TenantId == tenantId &&
                item.InvoiceId == invoiceId)
            .OrderBy(item => item.CreatedAtUtc)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
    }
}