namespace DispatchArc.Domain.Enums;

public enum JobStatus
{
    New = 1,
    Quoted = 2,
    Approved = 3,
    Scheduled = 4,
    InProgress = 5,
    Completed = 6,
    Invoiced = 7,
    Cancelled = 8
}