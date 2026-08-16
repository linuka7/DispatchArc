using DispatchArc.Domain.Enums;

namespace DispatchArc.Application.Jobs;

public sealed record JobNoteResponse(
    Guid Id,
    Guid TenantId,
    Guid ServiceJobId,
    Guid AuthorUserId,
    string AuthorFullName,
    JobNoteType Type,
    string Content,
    DateTimeOffset CreatedAtUtc);
