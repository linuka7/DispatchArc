using System.Globalization;

namespace DispatchArc.Application.Alerts;

public sealed class OperationalAlertService
{
    private static readonly TimeSpan JobStartingSoonWindow =
        TimeSpan.FromHours(24);

    private static readonly TimeSpan InvoiceDueSoonWindow =
        TimeSpan.FromDays(3);

    private readonly IOperationalAlertRepository _alerts;

    public OperationalAlertService(
        IOperationalAlertRepository alerts)
    {
        _alerts = alerts;
    }

    public async Task<OperationalAlertFeedResponse> GetAsync(
        Guid tenantId,
        OperationalAlertAudience audience,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Tenant ID is required.",
                nameof(tenantId));
        }

        if (!Enum.IsDefined(audience))
        {
            throw new ArgumentOutOfRangeException(
                nameof(audience),
                "Operational alert audience is invalid.");
        }

        var asOfUtc =
            DateTimeOffset.UtcNow;

        var alerts =
            await _alerts.GetAlertsAsync(
                tenantId,
                asOfUtc,
                asOfUtc.Add(JobStartingSoonWindow),
                asOfUtc.Add(InvoiceDueSoonWindow),
                cancellationToken);

        var visibleAlerts =
            alerts
                .Where(alert =>
                    audience ==
                        OperationalAlertAudience.All ||
                    alert.Audience == audience)
                .Select(Map)
                .OrderByDescending(alert =>
                    alert.Severity)
                .ThenBy(alert =>
                    alert.RelevantAtUtc)
                .ThenBy(alert =>
                    alert.Key,
                    StringComparer.Ordinal)
                .ToList();

        return new OperationalAlertFeedResponse(
            asOfUtc,
            visibleAlerts.Count,
            visibleAlerts.Count(alert =>
                alert.Severity ==
                    OperationalAlertSeverity.Critical),
            visibleAlerts.Count(alert =>
                alert.Severity ==
                    OperationalAlertSeverity.Warning),
            visibleAlerts.Count(alert =>
                alert.Severity ==
                    OperationalAlertSeverity.Info),
            visibleAlerts);
    }

    private static OperationalAlertResponse Map(
        OperationalAlertData alert)
    {
        var key =
            CreateKey(alert);

        var title =
            CreateTitle(alert.Type);

        var message =
            CreateMessage(alert);

        return new OperationalAlertResponse(
            key,
            alert.Type,
            alert.Audience,
            alert.Severity,
            title,
            message,
            alert.JobId,
            alert.JobNumber,
            alert.InvoiceId,
            alert.InvoiceNumber,
            alert.BalanceDue,
            alert.RelevantAtUtc);
    }

    private static string CreateKey(
        OperationalAlertData alert)
    {
        if (alert.JobId.HasValue)
        {
            return
                $"job:{alert.JobId.Value:N}:{alert.Type}";
        }

        if (alert.InvoiceId.HasValue)
        {
            return
                $"invoice:{alert.InvoiceId.Value:N}:{alert.Type}";
        }

        throw new InvalidOperationException(
            "Operational alert has no related entity.");
    }

    private static string CreateTitle(
        OperationalAlertType type)
    {
        return type switch
        {
            OperationalAlertType.ApprovedJobNeedsScheduling =>
                "Approved job needs scheduling",

            OperationalAlertType.ScheduledJobStartingSoon =>
                "Job starts soon",

            OperationalAlertType.ScheduledJobOverdueStart =>
                "Scheduled job has not started",

            OperationalAlertType.CompletedJobNeedsInvoice =>
                "Completed job needs an invoice",

            OperationalAlertType.InvoiceDueSoon =>
                "Invoice due soon",

            OperationalAlertType.InvoiceOverdue =>
                "Invoice is overdue",

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(type))
        };
    }

    private static string CreateMessage(
        OperationalAlertData alert)
    {
        var relevantTime =
            alert.RelevantAtUtc
                .ToUniversalTime()
                .ToString(
                    "yyyy-MM-dd HH:mm 'UTC'",
                    CultureInfo.InvariantCulture);

        return alert.Type switch
        {
            OperationalAlertType.ApprovedJobNeedsScheduling =>
                $"{alert.JobNumber} is approved but has not been scheduled.",

            OperationalAlertType.ScheduledJobStartingSoon =>
                $"{alert.JobNumber} is scheduled to start at {relevantTime}.",

            OperationalAlertType.ScheduledJobOverdueStart =>
                $"{alert.JobNumber} was scheduled to start at {relevantTime} and is still waiting to start.",

            OperationalAlertType.CompletedJobNeedsInvoice =>
                $"{alert.JobNumber} is completed and ready for invoicing.",

            OperationalAlertType.InvoiceDueSoon =>
                $"{alert.InvoiceNumber} has {FormatMoney(alert.BalanceDue)} outstanding and is due at {relevantTime}.",

            OperationalAlertType.InvoiceOverdue =>
                $"{alert.InvoiceNumber} has {FormatMoney(alert.BalanceDue)} outstanding and was due at {relevantTime}.",

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(alert.Type))
        };
    }

    private static string FormatMoney(
        decimal? amount)
    {
        return (amount ?? 0m)
            .ToString(
                "0.00",
                CultureInfo.InvariantCulture);
    }
}