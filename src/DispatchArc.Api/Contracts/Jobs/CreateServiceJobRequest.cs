using System.ComponentModel.DataAnnotations;
using DispatchArc.Domain.Enums;

namespace DispatchArc.Api.Contracts.Jobs;

public sealed class CreateServiceJobRequest
{
    public Guid CustomerId { get; init; }

    [Required]
    [MaxLength(180)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }

    public JobPriority Priority { get; init; } = JobPriority.Normal;
}
