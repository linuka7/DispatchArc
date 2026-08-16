using DispatchArc.Domain.Enums;

namespace DispatchArc.Application.Dashboard;

public sealed record DashboardMetricsData(
    int TotalCustomers,
    int ActiveTechnicians,
    IReadOnlyDictionary<JobStatus, int> JobsByStatus,
    int ScheduledToday,
    decimal TotalInvoiced,
    decimal TotalCollected,
    decimal CollectedThisMonth,
    int OutstandingInvoiceCount,
    decimal OutstandingBalance,
    int OverdueInvoiceCount,
    decimal OverdueBalance);