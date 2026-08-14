using System.ComponentModel.DataAnnotations;

namespace DispatchArc.Api.Contracts.Customers;

public sealed class CreateCustomerRequest
{
    [Required]
    [MaxLength(160)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [MaxLength(40)]
    public string Phone { get; init; } = string.Empty;

    [EmailAddress]
    [MaxLength(254)]
    public string? Email { get; init; }

    [MaxLength(250)]
    public string? AddressLine { get; init; }

    [MaxLength(100)]
    public string? City { get; init; }
}
