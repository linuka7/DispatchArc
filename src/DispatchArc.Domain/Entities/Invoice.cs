using DispatchArc.Domain.Enums;

namespace DispatchArc.Domain.Entities;

public sealed class Invoice
{
    private Invoice()
    {
    }

    public Invoice(
        Guid tenantId,
        Guid serviceJobId,
        Guid customerId,
        string invoiceNumber,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset dueAtUtc,
        decimal subtotal,
        decimal total)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException(
                "Tenant ID is required.",
                nameof(tenantId));

        if (serviceJobId == Guid.Empty)
            throw new ArgumentException(
                "Service job ID is required.",
                nameof(serviceJobId));

        if (customerId == Guid.Empty)
            throw new ArgumentException(
                "Customer ID is required.",
                nameof(customerId));

        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceNumber);

        if (invoiceNumber.Trim().Length > 50)
            throw new ArgumentException(
                "Invoice number cannot exceed 50 characters.",
                nameof(invoiceNumber));

        if (dueAtUtc < issuedAtUtc)
            throw new ArgumentException(
                "Invoice due date cannot be before the issue date.",
                nameof(dueAtUtc));

        if (subtotal < 0)
            throw new ArgumentOutOfRangeException(
                nameof(subtotal),
                "Invoice subtotal cannot be negative.");

        if (total < 0)
            throw new ArgumentOutOfRangeException(
                nameof(total),
                "Invoice total cannot be negative.");

        if (total < subtotal)
            throw new ArgumentException(
                "Invoice total cannot be less than the subtotal.",
                nameof(total));

        Id = Guid.NewGuid();
        TenantId = tenantId;
        ServiceJobId = serviceJobId;
        CustomerId = customerId;
        InvoiceNumber =
            invoiceNumber.Trim().ToUpperInvariant();

        Status = InvoiceStatus.Issued;

        IssuedAtUtc =
            issuedAtUtc.ToUniversalTime();

        DueAtUtc =
            dueAtUtc.ToUniversalTime();

        Subtotal = decimal.Round(
            subtotal,
            2,
            MidpointRounding.AwayFromZero);

        Total = decimal.Round(
            total,
            2,
            MidpointRounding.AwayFromZero);

        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid ServiceJobId { get; private set; }

    public Guid CustomerId { get; private set; }

    public string InvoiceNumber { get; private set; } =
        string.Empty;

    public InvoiceStatus Status { get; private set; }

    public DateTimeOffset IssuedAtUtc { get; private set; }

    public DateTimeOffset DueAtUtc { get; private set; }

    public decimal Subtotal { get; private set; }

    public decimal Total { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void UpdatePaymentStatus(
        decimal amountPaid)
    {
        if (Status == InvoiceStatus.Void)
        {
            throw new InvalidOperationException(
                "Payments cannot be applied to a void invoice.");
        }

        var roundedAmountPaid = decimal.Round(
            amountPaid,
            2,
            MidpointRounding.AwayFromZero);

        if (roundedAmountPaid < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amountPaid),
                "Amount paid cannot be negative.");
        }

        if (roundedAmountPaid > Total)
        {
            throw new InvalidOperationException(
                "The payment would exceed the invoice total.");
        }

        Status =
            roundedAmountPaid == 0
                ? InvoiceStatus.Issued
                : roundedAmountPaid < Total
                    ? InvoiceStatus.PartiallyPaid
                    : InvoiceStatus.Paid;

        UpdatedAtUtc =
            DateTimeOffset.UtcNow;
    }
}