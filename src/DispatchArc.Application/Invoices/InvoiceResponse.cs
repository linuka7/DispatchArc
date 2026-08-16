using DispatchArc.Domain.Enums;

namespace DispatchArc.Application.Invoices;

public sealed record InvoiceResponse(
    Guid Id,
    Guid TenantId,
    Guid ServiceJobId,
    Guid CustomerId,
    string InvoiceNumber,
    InvoiceStatus Status,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset DueAtUtc,
    decimal Subtotal,
    decimal Total,
    IReadOnlyList<InvoiceLineItemResponse> LineItems,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);