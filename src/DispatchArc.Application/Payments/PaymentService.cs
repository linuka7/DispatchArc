using DispatchArc.Application.Invoices;
using DispatchArc.Domain.Entities;
using DispatchArc.Domain.Enums;

namespace DispatchArc.Application.Payments;

public sealed class PaymentService
{
    private readonly IInvoiceRepository _invoices;
    private readonly IPaymentRepository _payments;

    public PaymentService(
        IInvoiceRepository invoices,
        IPaymentRepository payments)
    {
        _invoices = invoices;
        _payments = payments;
    }

    public async Task<InvoicePaymentSummaryResponse?>
        GetSummaryAsync(
            Guid tenantId,
            Guid invoiceId,
            CancellationToken cancellationToken)
    {
        var invoice =
            await _invoices.GetByIdAsync(
                tenantId,
                invoiceId,
                cancellationToken);

        if (invoice is null)
            return null;

        var payments =
            await _payments.GetByInvoiceAsync(
                tenantId,
                invoiceId,
                cancellationToken);

        return MapSummary(
            invoice,
            payments);
    }

    public Task<InvoicePaymentSummaryResponse?>
        RecordAsync(
            Guid tenantId,
            Guid invoiceId,
            decimal amount,
            PaymentMethod method,
            string? reference,
            DateTimeOffset? paidAtUtc,
            CancellationToken cancellationToken)
    {
        return _payments.ExecuteInTransactionAsync(
            transactionCancellationToken =>
                RecordWithinTransactionAsync(
                    tenantId,
                    invoiceId,
                    amount,
                    method,
                    reference,
                    paidAtUtc,
                    transactionCancellationToken),
            cancellationToken);
    }

    private async Task<InvoicePaymentSummaryResponse?>
        RecordWithinTransactionAsync(
            Guid tenantId,
            Guid invoiceId,
            decimal amount,
            PaymentMethod method,
            string? reference,
            DateTimeOffset? paidAtUtc,
            CancellationToken cancellationToken)
    {
        // This query acquires a PostgreSQL FOR UPDATE
        // row lock until this payment transaction commits.
        var invoice =
            await _invoices.GetForUpdateAsync(
                tenantId,
                invoiceId,
                cancellationToken);

        if (invoice is null)
            return null;

        if (invoice.Status == InvoiceStatus.Void)
        {
            throw new InvalidOperationException(
                "Payments cannot be recorded against a void invoice.");
        }

        if (invoice.Status == InvoiceStatus.Paid)
        {
            throw new InvalidOperationException(
                "This invoice is already fully paid.");
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

        var paymentDate =
            (paidAtUtc ??
                DateTimeOffset.UtcNow)
            .ToUniversalTime();

        if (paymentDate < invoice.IssuedAtUtc)
        {
            throw new ArgumentException(
                "Payment date cannot be before the invoice issue date.",
                nameof(paidAtUtc));
        }
        if (paymentDate >
            DateTimeOffset.UtcNow.AddMinutes(5))
        {
            throw new ArgumentException(
                "Payment date cannot be in the future.",
                nameof(paidAtUtc));
        }

        var existingPayments =
            await _payments.GetByInvoiceAsync(
                tenantId,
                invoiceId,
                cancellationToken);

        var normalizedReference =
            string.IsNullOrWhiteSpace(reference)
                ? string.Empty
                : reference.Trim();

        var normalizedReferenceKey =
            normalizedReference
                .ToUpperInvariant();

        if (normalizedReferenceKey.Length > 0 &&
            existingPayments.Any(payment =>
                payment.NormalizedReference ==
                    normalizedReferenceKey))
        {
            throw new InvalidOperationException(
                "A payment with this reference already exists for the invoice.");
        }

        var amountPaid =
            decimal.Round(
                existingPayments.Sum(
                    payment =>
                        payment.Amount),
                2,
                MidpointRounding.AwayFromZero);

        var newAmountPaid =
            decimal.Round(
                amountPaid +
                roundedAmount,
                2,
                MidpointRounding.AwayFromZero);

        if (newAmountPaid > invoice.Total)
        {
            var balanceDue =
                decimal.Round(
                    invoice.Total -
                    amountPaid,
                    2,
                    MidpointRounding.AwayFromZero);

            throw new InvalidOperationException(
                $"Payment exceeds the remaining balance of {balanceDue:0.00}.");
        }

        var paymentNumber =
            $"PAY-{paymentDate:yyyyMMdd}-" +
            Guid.NewGuid()
                .ToString("N")[..8]
                .ToUpperInvariant();

        var payment =
            new Payment(
                tenantId,
                invoice.Id,
                paymentNumber,
                roundedAmount,
                method,
                normalizedReference,
                paymentDate);

        await _payments.AddAsync(
            payment,
            cancellationToken);

        invoice.UpdatePaymentStatus(
            newAmountPaid);

        // The payment and invoice state change are saved
        // while the invoice row lock is still held.
        await _payments.SaveChangesAsync(
            cancellationToken);

        var allPayments =
            existingPayments
                .Append(payment)
                .OrderBy(item =>
                    item.PaidAtUtc)
                .ThenBy(item =>
                    item.CreatedAtUtc)
                .ThenBy(item =>
                    item.Id)
                .ToList();

        return MapSummary(
            invoice,
            allPayments);
    }

    private static InvoicePaymentSummaryResponse
        MapSummary(
            Invoice invoice,
            IReadOnlyCollection<Payment> payments)
    {
        var responses =
            payments
                .OrderBy(payment =>
                    payment.PaidAtUtc)
                .ThenBy(payment =>
                    payment.CreatedAtUtc)
                .ThenBy(payment =>
                    payment.Id)
                .Select(MapPayment)
                .ToList();

        var amountPaid =
            decimal.Round(
                responses.Sum(
                    payment =>
                        payment.Amount),
                2,
                MidpointRounding.AwayFromZero);

        var balanceDue =
            decimal.Round(
                invoice.Total -
                amountPaid,
                2,
                MidpointRounding.AwayFromZero);

        if (balanceDue < 0)
            balanceDue = 0;

        return new InvoicePaymentSummaryResponse(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.Status,
            invoice.Total,
            amountPaid,
            balanceDue,
            responses);
    }

    private static PaymentResponse MapPayment(
        Payment payment)
    {
        return new PaymentResponse(
            payment.Id,
            payment.TenantId,
            payment.InvoiceId,
            payment.PaymentNumber,
            payment.Amount,
            payment.Method,
            payment.Reference,
            payment.PaidAtUtc,
            payment.CreatedAtUtc);
    }
}