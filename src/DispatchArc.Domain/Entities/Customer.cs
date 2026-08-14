namespace DispatchArc.Domain.Entities;

public sealed class Customer
{
    private Customer()
    {
    }

    public Customer(
        Guid tenantId,
        string name,
        string phone,
        string? email = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Tenant ID is required.",
                nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);

        Id = Guid.NewGuid();
        TenantId = tenantId;
        Name = name.Trim();
        Phone = phone.Trim();
        Email = string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim().ToLowerInvariant();

        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public string? AddressLine { get; private set; }

    public string? City { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void UpdateAddress(string? addressLine, string? city)
    {
        AddressLine = string.IsNullOrWhiteSpace(addressLine)
            ? null
            : addressLine.Trim();

        City = string.IsNullOrWhiteSpace(city)
            ? null
            : city.Trim();

        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}