using DispatchArc.Domain.Enums;

namespace DispatchArc.Application.Dashboard;

public sealed class DashboardService
{
    private readonly IDashboardRepository _dashboard;

    public DashboardService(
        IDashboardRepository dashboard)
    {
        _dashboard = dashboard;
    }

    public async Task<DashboardMetricsResponse> GetAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Tenant ID is required.",
                nameof(tenantId));
        }

        var asOfUtc =
            DateTimeOffset.UtcNow;

        var metrics =
            await _dashboard.GetMetricsAsync(
                tenantId,
                asOfUtc,
                cancellationToken);

        var jobStatuses =
            Enum.GetValues<JobStatus>()
                .Select(status =>
                    new DashboardJobStatusResponse(
                        status,
                        metrics.JobsByStatus.TryGetValue(
                            status,
                            out var count)
                                ? count
                                : 0))
                .ToList();

        var totalJobs =
            jobStatuses.Sum(item => item.Count);

        var terminalStatuses =
            new HashSet<JobStatus>
            {
                JobStatus.Completed,
                JobStatus.Invoiced,
                JobStatus.Cancelled
            };

        var openJobs =
            jobStatuses
                .Where(item =>
                    !terminalStatuses.Contains(
                        item.Status))
                .Sum(item => item.Count);

        return new DashboardMetricsResponse(
            asOfUtc,
            metrics.TotalCustomers,
            metrics.ActiveTechnicians,
            totalJobs,
            openJobs,
            metrics.ScheduledToday,
            jobStatuses,
            metrics.TotalInvoiced,
            metrics.TotalCollected,
            metrics.CollectedThisMonth,
            metrics.OutstandingInvoiceCount,
            metrics.OutstandingBalance,
            metrics.OverdueInvoiceCount,
            metrics.OverdueBalance);
    }
}