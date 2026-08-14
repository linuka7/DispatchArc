using System.ComponentModel.DataAnnotations;

namespace DispatchArc.Api.Contracts.Tenants;

public sealed class CreateTenantRequest
{
    [Required]
    [MaxLength(120)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [MaxLength(80)]
    [RegularExpression(
        "^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Slug can contain lowercase letters, numbers and hyphens.")]
    public string Slug { get; init; } = string.Empty;
}
