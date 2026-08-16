using DispatchArc.Application.Alerts;
using DispatchArc.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DispatchArc.Infrastructure.Persistence.Repositories;

public sealed class OperationalAlertRepository
    : IOperationalAlertRepository
{
    private readonly DispatchArcDbContext _database;

    public OperationalAlertRepository(
        DispatchArcDbContext database)
    {
        _database = database;
    }

    public async Task<IReadOnlyList<OperationalAlertData>>
        GetAlertsAsync(
            Guid tenantId,
            DateTimeOffset asOfUtc,
            DateTimeOffset jobStartingSoonUntilUtc,
            DateTimeOffset invoiceDueSoonUntilUtc,
            CancellationToken cancellationToken)
    {
        var normalizedAsOfUtc =
            asOfUtc.ToUniversalTime();

        var normalizedJobWindowEnd =
            jobStartingSoonUntilUtc.ToUniversalTime();

        var normalizedInvoiceWindowEnd =
            invoiceDueSoonUntilUtc.ToUniversalTime();

        var alerts =
            new List<OperationalAlertData>();


        // =================================================
        // Operational job alerts
        // =================================================

        var jobRows =
            await _database.ServiceJobs
                .AsNoTracking()
                .Where(job =>
                    job.TenantId == tenantId &&
                    (
                        job.Status == JobStatus.Approved ||
                        job.Status == JobStatus.Completed ||
                        (
                            job.Status == JobStatus.Scheduled &&
                            job.ScheduledStartUtc != null &&
                            job.ScheduledStartUtc <=
                                normalizedJobWindowEnd
                        )
                    ))
                .Select(job => new
                {
                    job.Id,
                    job.JobNumber,
                    job.Status,
                    job.ScheduledStartUtc,
                    job.UpdatedAtUtc
                })
                .ToListAsync(
                    cancellationToken);

        foreach (var job in jobRows)
        {
            if (job.Status == JobStatus.Approved)
            {
                alerts.Add(
                    new OperationalAlertData(
                        OperationalAlertType
                            .ApprovedJobNeedsScheduling,
                        OperationalAlertAudience
                            .Operations,
                        OperationalAlertSeverity
                            .Warning,
                        job.Id,
                        job.JobNumber,
                        null,
                        null,
                        null,
                        job.UpdatedAtUtc));

                continue;
            }

            if (job.Status == JobStatus.Completed)
            {
                alerts.Add(
                    new OperationalAlertData(
                        OperationalAlertType
                            .CompletedJobNeedsInvoice,
                        OperationalAlertAudience
                            .Finance,
                        OperationalAlertSeverity
                            .Warning,
                        job.Id,
                        job.JobNumber,
                        null,
                        null,
                        null,
                        job.UpdatedAtUtc));

                continue;
            }

            if (job.Status != JobStatus.Scheduled ||
                !job.ScheduledStartUtc.HasValue)
            {
                continue;
            }

            var scheduledStart =
                job.ScheduledStartUtc.Value
                    .ToUniversalTime();

            if (scheduledStart < normalizedAsOfUtc)
            {
                alerts.Add(
                    new OperationalAlertData(
                        OperationalAlertType
                            .ScheduledJobOverdueStart,
                        OperationalAlertAudience
                            .Operations,
                        OperationalAlertSeverity
                            .Critical,
                        job.Id,
                        job.JobNumber,
                        null,
                        null,
                        null,
                        scheduledStart));

                continue;
            }

            alerts.Add(
                new OperationalAlertData(
                    OperationalAlertType
                        .ScheduledJobStartingSoon,
                    OperationalAlertAudience
                        .Operations,
                    OperationalAlertSeverity
                        .Info,
                    job.Id,
                    job.JobNumber,
                    null,
                    null,
                    null,
                    scheduledStart));
        }


        // =================================================
        // Financial invoice alerts
        // =================================================

        var invoiceRows =
            await _database.Invoices
                .AsNoTracking()
                .Where(invoice =>
                    invoice.TenantId == tenantId &&
                    invoice.Total > 0 &&
                    invoice.DueAtUtc <=
                        normalizedInvoiceWindowEnd &&
                    (
                        invoice.Status ==
                            InvoiceStatus.Issued ||
                        invoice.Status ==
                            InvoiceStatus.PartiallyPaid
                    ))
                .Select(invoice => new
                {
                    invoice.Id,
                    invoice.InvoiceNumber,
                    invoice.Total,
                    invoice.DueAtUtc
                })
                .ToListAsync(
                    cancellationToken);

        if (invoiceRows.Count > 0)
        {
            var invoiceIds =
                invoiceRows
                    .Select(invoice => invoice.Id)
                    .ToList();

            var paymentRows =
                await _database.Payments
                    .AsNoTracking()
                    .Where(payment =>
                        payment.TenantId == tenantId &&
                        invoiceIds.Contains(
                            payment.InvoiceId))
                    .GroupBy(payment =>
                        payment.InvoiceId)
                    .Select(group => new
                    {
                        InvoiceId = group.Key,
                        AmountPaid =
                            group.Sum(payment =>
                                payment.Amount)
                    })
                    .ToListAsync(
                        cancellationToken);

            var paidByInvoice =
                paymentRows.ToDictionary(
                    payment => payment.InvoiceId,
                    payment => payment.AmountPaid);

            foreach (var invoice in invoiceRows)
            {
                var amountPaid =
                    paidByInvoice.TryGetValue(
                        invoice.Id,
                        out var paid)
                            ? paid
                            : 0m;

                var balanceDue =
                    RoundMoney(
                        invoice.Total -
                        amountPaid);

                if (balanceDue <= 0)
                {
                    continue;
                }

                var dueAtUtc =
                    invoice.DueAtUtc
                        .ToUniversalTime();

                var isOverdue =
                    dueAtUtc < normalizedAsOfUtc;

                alerts.Add(
                    new OperationalAlertData(
                        isOverdue
                            ? OperationalAlertType
                                .InvoiceOverdue
                            : OperationalAlertType
                                .InvoiceDueSoon,
                        OperationalAlertAudience
                            .Finance,
                        isOverdue
                            ? OperationalAlertSeverity
                                .Critical
                            : OperationalAlertSeverity
                                .Warning,
                        null,
                        null,
                        invoice.Id,
                        invoice.InvoiceNumber,
                        balanceDue,
                        dueAtUtc));
            }
        }

        return alerts;
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