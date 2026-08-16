namespace DispatchArc.Application.Alerts;

public sealed record OperationalAlertData(
    OperationalAlertType Type,
    OperationalAlertAudience Audience,
    OperationalAlertSeverity Severity,
    Guid? JobId,
    string? JobNumber,
    Guid? InvoiceId,
    string? InvoiceNumber,
    decimal? BalanceDue,
    DateTimeOffset RelevantAtUtc);