namespace DispatchArc.Application.Tenants;

public sealed record TenantResponse(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);