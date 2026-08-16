namespace DispatchArc.Application.Jobs;

public sealed record JobLineItemResponse(
    Guid Id,
    Guid TenantId,
    Guid ServiceJobId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);