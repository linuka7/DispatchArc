using DispatchArc.Domain.Enums;

namespace DispatchArc.Api.Contracts.Jobs;

public sealed class AddJobNoteRequest
{
    public JobNoteType Type { get; init; }

    public string Content { get; init; } = string.Empty;
}
