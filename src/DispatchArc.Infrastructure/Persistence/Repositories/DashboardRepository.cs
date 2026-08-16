using DispatchArc.Application.Dashboard;
using DispatchArc.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DispatchArc.Infrastructure.Persistence.Repositories;

public sealed class DashboardRepository
    : IDashboardRepository
{
    private readonly DispatchArcDbContext _database;

    public DashboardRepository(
        DispatchArcDbContext database)
    {
        _database = database;
    }

    public async Task<DashboardMetricsData> GetMetricsAsync(
        Guid tenantId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken)
    {
        var normalizedAsOfUtc =
            asOfUtc.ToUniversalTime();

        var dayStartUtc =
            new DateTimeOffset(
                normalizedAsOfUtc.UtcDateTime.Date,
                TimeSpan.Zero);

        var dayEndUtc =
            dayStartUtc.AddDays(1);

        var monthStartUtc =
            new DateTimeOffset(
                normalizedAsOfUtc.Year,
                normalizedAsOfUtc.Month,
                1,
                0,
                0,
                0,
                TimeSpan.Zero);


        // -------------------------------------------------
        // People / customers
        // -------------------------------------------------

        var totalCustomers =
            await _database.Customers
                .AsNoTracking()
                .CountAsync(
                    customer =>
                        customer.TenantId == tenantId,
                    cancellationToken);

        var activeTechnicians =
            await _database.Users
                .AsNoTracking()
                .CountAsync(
                    user =>
                        user.TenantId == tenantId &&
                        user.Role == UserRole.Technician &&
                        user.IsActive,
                    cancellationToken);


        // -------------------------------------------------
        // Jobs
        // -------------------------------------------------

        var jobStatusRows =
            await _database.ServiceJobs
                .AsNoTracking()
                .Where(job =>
                    job.TenantId == tenantId)
                .GroupBy(job => job.Status)
                .Select(group => new
                {
                    Status = group.Key,
                    Count = group.Count()
                })
                .ToListAsync(
                    cancellationToken);

        IReadOnlyDictionary<JobStatus, int> jobsByStatus =
            jobStatusRows.ToDictionary(
                item => item.Status,
                item => item.Count);

        var scheduledToday =
            await _database.ServiceJobs
                .AsNoTracking()
                .CountAsync(
                    job =>
                        job.TenantId == tenantId &&
                        job.Status != JobStatus.Cancelled &&
                        job.ScheduledStartUtc != null &&
                        job.ScheduledStartUtc >= dayStartUtc &&
                        job.ScheduledStartUtc < dayEndUtc,
                    cancellationToken);


        // -------------------------------------------------
        // Invoice totals
        // -------------------------------------------------

        var totalInvoiced =
            await _database.Invoices
                .AsNoTracking()
                .Where(invoice =>
                    invoice.TenantId == tenantId &&
                    invoice.Status != InvoiceStatus.Void)
                .Select(invoice =>
                    (decimal?)invoice.Total)
                .SumAsync(
                    cancellationToken)
            ?? 0m;


        // -------------------------------------------------
        // Payments collected
        // -------------------------------------------------

        var totalCollected =
            await (
                from payment in
                    _database.Payments.AsNoTracking()
                join invoice in
                    _database.Invoices.AsNoTracking()
                    on payment.InvoiceId equals invoice.Id
                where
                    payment.TenantId == tenantId &&
                    invoice.TenantId == tenantId &&
                    invoice.Status != InvoiceStatus.Void
                select (decimal?)payment.Amount
            )
            .SumAsync(
                cancellationToken)
            ?? 0m;

        var collectedThisMonth =
            await (
                from payment in
                    _database.Payments.AsNoTracking()
                join invoice in
                    _database.Invoices.AsNoTracking()
                    on payment.InvoiceId equals invoice.Id
                where
                    payment.TenantId == tenantId &&
                    invoice.TenantId == tenantId &&
                    invoice.Status != InvoiceStatus.Void &&
                    payment.PaidAtUtc >= monthStartUtc &&
                    payment.PaidAtUtc <= normalizedAsOfUtc
                select (decimal?)payment.Amount
            )
            .SumAsync(
                cancellationToken)
            ?? 0m;


        // -------------------------------------------------
        // Outstanding invoices
        // -------------------------------------------------

        var outstandingInvoiceCount =
            await _database.Invoices
                .AsNoTracking()
                .CountAsync(
                    invoice =>
                        invoice.TenantId == tenantId &&
                        invoice.Total > 0 &&
                        (
                            invoice.Status ==
                                InvoiceStatus.Issued ||
                            invoice.Status ==
                                InvoiceStatus.PartiallyPaid
                        ),
                    cancellationToken);

        var outstandingBalance =
            RoundMoney(
                totalInvoiced -
                totalCollected);

        if (outstandingBalance < 0)
            outstandingBalance = 0m;


        // -------------------------------------------------
        // Overdue invoices
        // -------------------------------------------------

        var overdueInvoiceCount =
            await _database.Invoices
                .AsNoTracking()
                .CountAsync(
                    invoice =>
                        invoice.TenantId == tenantId &&
                        invoice.Total > 0 &&
                        invoice.DueAtUtc < normalizedAsOfUtc &&
                        (
                            invoice.Status ==
                                InvoiceStatus.Issued ||
                            invoice.Status ==
                                InvoiceStatus.PartiallyPaid
                        ),
                    cancellationToken);

        var overdueInvoiceTotal =
            await _database.Invoices
                .AsNoTracking()
                .Where(invoice =>
                    invoice.TenantId == tenantId &&
                    invoice.Total > 0 &&
                    invoice.DueAtUtc < normalizedAsOfUtc &&
                    (
                        invoice.Status ==
                            InvoiceStatus.Issued ||
                        invoice.Status ==
                            InvoiceStatus.PartiallyPaid
                    ))
                .Select(invoice =>
                    (decimal?)invoice.Total)
                .SumAsync(
                    cancellationToken)
            ?? 0m;

        var overduePayments =
            await (
                from payment in
                    _database.Payments.AsNoTracking()
                join invoice in
                    _database.Invoices.AsNoTracking()
                    on payment.InvoiceId equals invoice.Id
                where
                    payment.TenantId == tenantId &&
                    invoice.TenantId == tenantId &&
                    invoice.Total > 0 &&
                    invoice.DueAtUtc < normalizedAsOfUtc &&
                    (
                        invoice.Status ==
                            InvoiceStatus.Issued ||
                        invoice.Status ==
                            InvoiceStatus.PartiallyPaid
                    )
                select (decimal?)payment.Amount
            )
            .SumAsync(
                cancellationToken)
            ?? 0m;

        var overdueBalance =
            RoundMoney(
                overdueInvoiceTotal -
                overduePayments);

        if (overdueBalance < 0)
            overdueBalance = 0m;


        return new DashboardMetricsData(
            totalCustomers,
            activeTechnicians,
            jobsByStatus,
            scheduledToday,
            RoundMoney(totalInvoiced),
            RoundMoney(totalCollected),
            RoundMoney(collectedThisMonth),
            outstandingInvoiceCount,
            outstandingBalance,
            overdueInvoiceCount,
            overdueBalance);
    }

    private static decimal RoundMoney(
        decimal amount)
    {
        return decimal.Round(
            amount,
            2,
            MidpointRounding.AwayFromZero);
    }
}