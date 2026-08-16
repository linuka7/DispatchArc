using DispatchArc.Application.Payments;
using DispatchArc.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DispatchArc.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository
    : IPaymentRepository
{
    private readonly DispatchArcDbContext _database;

    public PaymentRepository(
        DispatchArcDbContext database)
    {
        _database = database;
    }

    public async Task AddAsync(
        Payment payment,
        CancellationToken cancellationToken)
    {
        await _database.Payments.AddAsync(
            payment,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>>
        GetByInvoiceAsync(
            Guid tenantId,
            Guid invoiceId,
            CancellationToken cancellationToken)
    {
        return await _database.Payments
            .AsNoTracking()
            .Where(payment =>
                payment.TenantId == tenantId &&
                payment.InvoiceId == invoiceId)
            .OrderBy(payment => payment.PaidAtUtc)
            .ThenBy(payment => payment.CreatedAtUtc)
            .ThenBy(payment => payment.Id)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return _database.SaveChangesAsync(
            cancellationToken);
    }
}