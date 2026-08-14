using DispatchArc.Domain.Enums;

namespace DispatchArc.Application.Jobs;

public sealed record ServiceJobResponse(
    Guid Id,
    Guid TenantId,
    Guid CustomerId,
    Guid? AssignedTechnicianId,
    string JobNumber,
    string Title,
    string Description,
    JobPriority Priority,
    JobStatus Status,
    DateTimeOffset? ScheduledStartUtc,
    DateTimeOffset? ScheduledEndUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
