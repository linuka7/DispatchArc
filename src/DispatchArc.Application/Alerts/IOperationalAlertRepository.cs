namespace DispatchArc.Application.Alerts;

public interface IOperationalAlertRepository
{
    Task<IReadOnlyList<OperationalAlertData>> GetAlertsAsync(
        Guid tenantId,
        DateTimeOffset asOfUtc,
        DateTimeOffset jobStartingSoonUntilUtc,
        DateTimeOffset invoiceDueSoonUntilUtc,
        CancellationToken cancellationToken);
}