using DispatchArc.Domain.Enums;

namespace DispatchArc.Application.Payments;

public sealed record InvoicePaymentSummaryResponse(
    Guid InvoiceId,
    string InvoiceNumber,
    InvoiceStatus Status,
    decimal InvoiceTotal,
    decimal AmountPaid,
    decimal BalanceDue,
    IReadOnlyList<PaymentResponse> Payments);