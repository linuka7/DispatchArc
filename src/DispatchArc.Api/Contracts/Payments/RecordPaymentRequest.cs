using DispatchArc.Domain.Enums;

namespace DispatchArc.Api.Contracts.Payments;

public sealed class RecordPaymentRequest
{
    public decimal Amount { get; init; }

    public PaymentMethod Method { get; init; }

    public string? Reference { get; init; }

    public DateTimeOffset? PaidAtUtc { get; init; }
}