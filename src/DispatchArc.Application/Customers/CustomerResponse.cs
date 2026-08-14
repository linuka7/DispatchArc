namespace DispatchArc.Application.Customers;

public sealed record CustomerResponse(
    Guid Id,
    Guid TenantId,
    string Name,
    string Phone,
    string? Email,
    string? AddressLine,
    string? City,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
