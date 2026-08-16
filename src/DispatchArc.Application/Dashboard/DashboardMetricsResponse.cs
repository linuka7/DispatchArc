using DispatchArc.Domain.Enums;

namespace DispatchArc.Application.Dashboard;

public sealed record DashboardJobStatusResponse(
    JobStatus Status,
    int Count);

public sealed record DashboardMetricsResponse(
    DateTimeOffset AsOfUtc,
    int TotalCustomers,
    int ActiveTechnicians,
    int TotalJobs,
    int OpenJobs,
    int ScheduledToday,
    IReadOnlyList<DashboardJobStatusResponse> JobsByStatus,
    decimal TotalInvoiced,
    decimal TotalCollected,
    decimal CollectedThisMonth,
    int OutstandingInvoiceCount,
    decimal OutstandingBalance,
    int OverdueInvoiceCount,
    decimal OverdueBalance);