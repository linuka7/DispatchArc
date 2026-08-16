using DispatchArc.Domain.Enums;

namespace DispatchArc.Domain.Entities;

public sealed class Payment
{
    private Payment()
    {
    }

    public Payment(
        Guid tenantId,
        Guid invoiceId,
        string paymentNumber,
        decimal amount,
        PaymentMethod method,
        string? reference,
        DateTimeOffset paidAtUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Tenant ID is required.",
                nameof(tenantId));
        }

        if (invoiceId == Guid.Empty)
        {
            throw new ArgumentException(
                "Invoice ID is required.",
                nameof(invoiceId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            paymentNumber);

        if (paymentNumber.Trim().Length > 50)
        {
            throw new ArgumentException(
                "Payment number cannot exceed 50 characters.",
                nameof(paymentNumber));
        }

        var roundedAmount =
            decimal.Round(
                amount,
                2,
                MidpointRounding.AwayFromZero);

        if (roundedAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Payment amount must be greater than zero.");
        }

        if (!Enum.IsDefined(method))
        {
            throw new ArgumentOutOfRangeException(
                nameof(method),
                "Payment method is invalid.");
        }

        var cleanedReference =
            string.IsNullOrWhiteSpace(reference)
                ? string.Empty
                : reference.Trim();

        if (cleanedReference.Length > 150)
        {
            throw new ArgumentException(
                "Payment reference cannot exceed 150 characters.",
                nameof(reference));
        }

        var normalizedReference =
            cleanedReference.ToUpperInvariant();

        if (normalizedReference.Length > 150)
        {
            throw new ArgumentException(
                "Normalized payment reference cannot exceed 150 characters.",
                nameof(reference));
        }

        if (paidAtUtc == default)
        {
            throw new ArgumentException(
                "Payment date is required.",
                nameof(paidAtUtc));
        }

        Id = Guid.NewGuid();
        TenantId = tenantId;
        InvoiceId = invoiceId;

        PaymentNumber =
            paymentNumber
                .Trim()
                .ToUpperInvariant();

        Amount = roundedAmount;
        Method = method;

        Reference =
            cleanedReference;

        NormalizedReference =
            normalizedReference;

        PaidAtUtc =
            paidAtUtc.ToUniversalTime();

        CreatedAtUtc =
            DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid InvoiceId { get; private set; }

    public string PaymentNumber { get; private set; } =
        string.Empty;

    public decimal Amount { get; private set; }

    public PaymentMethod Method { get; private set; }

    public string Reference { get; private set; } =
        string.Empty;

    public string NormalizedReference { get; private set; } =
        string.Empty;

    public DateTimeOffset PaidAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
}