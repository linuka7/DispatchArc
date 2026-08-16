namespace DispatchArc.Domain.Entities;

public sealed class InvoiceLineItem
{
    private InvoiceLineItem()
    {
    }

    public InvoiceLineItem(
        Guid tenantId,
        Guid invoiceId,
        string description,
        decimal quantity,
        decimal unitPrice)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException(
                "Tenant ID is required.",
                nameof(tenantId));

        if (invoiceId == Guid.Empty)
            throw new ArgumentException(
                "Invoice ID is required.",
                nameof(invoiceId));

        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        if (description.Trim().Length > 300)
            throw new ArgumentException(
                "Invoice line item description cannot exceed 300 characters.",
                nameof(description));

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");

        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice),
                "Unit price cannot be negative.");

        Id = Guid.NewGuid();
        TenantId = tenantId;
        InvoiceId = invoiceId;
        Description = description.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid InvoiceId { get; private set; }

    public string Description { get; private set; } =
        string.Empty;

    public decimal Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal LineTotal =>
        decimal.Round(
            Quantity * UnitPrice,
            2,
            MidpointRounding.AwayFromZero);

    public DateTimeOffset CreatedAtUtc { get; private set; }
}