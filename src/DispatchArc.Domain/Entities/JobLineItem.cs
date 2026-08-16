namespace DispatchArc.Domain.Entities;

public sealed class JobLineItem
{
    private JobLineItem()
    {
    }

    public JobLineItem(
        Guid tenantId,
        Guid serviceJobId,
        string description,
        decimal quantity,
        decimal unitPrice)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException(
                "Tenant ID is required.",
                nameof(tenantId));

        if (serviceJobId == Guid.Empty)
            throw new ArgumentException(
                "Service job ID is required.",
                nameof(serviceJobId));

        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        if (description.Trim().Length > 300)
            throw new ArgumentException(
                "Line item description cannot exceed 300 characters.",
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
        ServiceJobId = serviceJobId;
        Description = description.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid ServiceJobId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public decimal Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal LineTotal =>
        decimal.Round(
            Quantity * UnitPrice,
            2,
            MidpointRounding.AwayFromZero);

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Update(
        string description,
        decimal quantity,
        decimal unitPrice)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        if (description.Trim().Length > 300)
            throw new ArgumentException(
                "Line item description cannot exceed 300 characters.",
                nameof(description));

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");

        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice),
                "Unit price cannot be negative.");

        Description = description.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}