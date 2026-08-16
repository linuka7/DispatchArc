using DispatchArc.Application.Invoices;
using DispatchArc.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DispatchArc.Infrastructure.Persistence.Repositories;

public sealed class InvoiceRepository
    : IInvoiceRepository
{
    private readonly DispatchArcDbContext _database;

    public InvoiceRepository(
        DispatchArcDbContext database)
    {
        _database = database;
    }

    public async Task AddAsync(
        Invoice invoice,
        CancellationToken cancellationToken)
    {
        await _database.Invoices.AddAsync(
            invoice,
            cancellationToken);
    }

    public Task<Invoice?> GetByIdAsync(
        Guid tenantId,
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        return _database.Invoices
            .AsNoTracking()
            .SingleOrDefaultAsync(
                invoice =>
                    invoice.TenantId == tenantId &&
                    invoice.Id == invoiceId,
                cancellationToken);
    }

    public Task<Invoice?> GetForUpdateAsync(
        Guid tenantId,
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        if (_database.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "Invoice row locking requires an active database transaction.");
        }

        return _database.Invoices
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM invoices
                WHERE "TenantId" = {tenantId}
                  AND "Id" = {invoiceId}
                FOR UPDATE
                """)
            .AsTracking()
            .SingleOrDefaultAsync(
                cancellationToken);
    }

    public Task<Invoice?> GetByJobAsync(
        Guid tenantId,
        Guid serviceJobId,
        CancellationToken cancellationToken)
    {
        return _database.Invoices
            .AsNoTracking()
            .SingleOrDefaultAsync(
                invoice =>
                    invoice.TenantId == tenantId &&
                    invoice.ServiceJobId == serviceJobId,
                cancellationToken);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return _database.SaveChangesAsync(
            cancellationToken);
    }
}