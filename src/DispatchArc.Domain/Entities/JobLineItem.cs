namespace DispatchArc.Domain.Entities;

public sealed class JobLineItem
{
    private const decimal MaxQuantity =
        999999999999999.999m;

    private const decimal MaxUnitPrice =
        9999999999999999.99m;

    private const decimal MaxLineTotal =
        9999999999999999.99m;

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
        {
            throw new ArgumentException(
                "Tenant ID is required.",
                nameof(tenantId));
        }

        if (serviceJobId == Guid.Empty)
        {
            throw new ArgumentException(
                "Service job ID is required.",
                nameof(serviceJobId));
        }

        ValidateDescription(
            description);

        ValidatePricing(
            quantity,
            unitPrice);

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

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Update(
        string description,
        decimal quantity,
        decimal unitPrice)
    {
        ValidateDescription(
            description);

        ValidatePricing(
            quantity,
            unitPrice);

        Description =
            description.Trim();

        Quantity =
            quantity;

        UnitPrice =
            unitPrice;

        UpdatedAtUtc =
            DateTimeOffset.UtcNow;
    }

    private static void ValidateDescription(
        string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            description);

        if (description.Trim().Length > 300)
        {
            throw new ArgumentException(
                "Line item description cannot exceed 300 characters.",
                nameof(description));
        }
    }

    private static void ValidatePricing(
        decimal quantity,
        decimal unitPrice)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }

        if (quantity > MaxQuantity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity exceeds the supported database range.");
        }

        if (decimal.Round(
                quantity,
                3,
                MidpointRounding.AwayFromZero) != quantity)
        {
            throw new ArgumentException(
                "Quantity cannot contain more than 3 decimal places.",
                nameof(quantity));
        }

        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice),
                "Unit price cannot be negative.");
        }

        if (unitPrice > MaxUnitPrice)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice),
                "Unit price exceeds the supported database range.");
        }

        if (decimal.Round(
                unitPrice,
                2,
                MidpointRounding.AwayFromZero) != unitPrice)
        {
            throw new ArgumentException(
                "Unit price cannot contain more than 2 decimal places.",
                nameof(unitPrice));
        }

        decimal lineTotal;

        try
        {
            lineTotal =
                decimal.Round(
                    quantity * unitPrice,
                    2,
                    MidpointRounding.AwayFromZero);
        }
        catch (OverflowException)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice),
                "Line item total is too large.");
        }

        if (lineTotal > MaxLineTotal)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice),
                "Line item total exceeds the supported money range.");
        }
    }
}