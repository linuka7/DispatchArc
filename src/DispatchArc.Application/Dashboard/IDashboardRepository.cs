namespace DispatchArc.Application.Dashboard;

public interface IDashboardRepository
{
    Task<DashboardMetricsData> GetMetricsAsync(
        Guid tenantId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken);
}