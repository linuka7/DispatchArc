namespace DispatchArc.Api.Contracts.Jobs;

public sealed class ScheduleServiceJobRequest
{
    public DateTimeOffset StartUtc { get; init; }
    public DateTimeOffset EndUtc { get; init; }
}
