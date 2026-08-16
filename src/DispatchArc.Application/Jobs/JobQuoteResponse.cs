namespace DispatchArc.Application.Jobs;

public sealed record JobQuoteResponse(
    Guid TenantId,
    Guid ServiceJobId,
    IReadOnlyList<JobLineItemResponse> LineItems,
    decimal Subtotal);