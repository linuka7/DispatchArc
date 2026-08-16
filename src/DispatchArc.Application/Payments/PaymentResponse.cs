using DispatchArc.Domain.Enums;

namespace DispatchArc.Application.Payments;

public sealed record PaymentResponse(
    Guid Id,
    Guid TenantId,
    Guid InvoiceId,
    string PaymentNumber,
    decimal Amount,
    PaymentMethod Method,
    string Reference,
    DateTimeOffset PaidAtUtc,
    DateTimeOffset CreatedAtUtc);