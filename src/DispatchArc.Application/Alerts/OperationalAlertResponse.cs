namespace DispatchArc.Application.Alerts;

public sealed record OperationalAlertResponse(
    string Key,
    OperationalAlertType Type,
    OperationalAlertAudience Audience,
    OperationalAlertSeverity Severity,
    string Title,
    string Message,
    Guid? JobId,
    string? JobNumber,
    Guid? InvoiceId,
    string? InvoiceNumber,
    decimal? BalanceDue,
    DateTimeOffset RelevantAtUtc);

public sealed record OperationalAlertFeedResponse(
    DateTimeOffset AsOfUtc,
    int TotalCount,
    int CriticalCount,
    int WarningCount,
    int InfoCount,
    IReadOnlyList<OperationalAlertResponse> Alerts);