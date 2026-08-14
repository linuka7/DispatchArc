using DispatchArc.Domain.Enums;

namespace DispatchArc.Domain.Entities;

public sealed class ServiceJob
{
    private ServiceJob()
    {
    }

    public ServiceJob(
        Guid tenantId,
        Guid customerId,
        string jobNumber,
        string title,
        string? description,
        JobPriority priority = JobPriority.Normal)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Tenant ID is required.",
                nameof(tenantId));
        }

        if (customerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Customer ID is required.",
                nameof(customerId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(jobNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Id = Guid.NewGuid();
        TenantId = tenantId;
        CustomerId = customerId;
        JobNumber = jobNumber.Trim().ToUpperInvariant();
        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        Priority = priority;
        Status = JobStatus.New;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid? AssignedTechnicianId { get; private set; }

    public string JobNumber { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public JobPriority Priority { get; private set; }

    public JobStatus Status { get; private set; }

    public DateTimeOffset? ScheduledStartUtc { get; private set; }

    public DateTimeOffset? ScheduledEndUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void AssignTechnician(Guid technicianId)
    {
        if (technicianId == Guid.Empty)
        {
            throw new ArgumentException(
                "Technician ID is required.",
                nameof(technicianId));
        }

        EnsureJobIsOpen();

        AssignedTechnicianId = technicianId;
        Touch();
    }

    public void MarkQuoted()
    {
        MoveFrom(JobStatus.New, JobStatus.Quoted);
    }

    public void Approve()
    {
        if (Status is not JobStatus.New and not JobStatus.Quoted)
        {
            throw new InvalidOperationException(
                $"A job in {Status} status cannot be approved.");
        }

        Status = JobStatus.Approved;
        Touch();
    }

    public void Schedule(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc)
    {
        if (Status != JobStatus.Approved)
        {
            throw new InvalidOperationException(
                "Only approved jobs can be scheduled.");
        }

        if (endUtc <= startUtc)
        {
            throw new ArgumentException(
                "The scheduled end must be after the start.");
        }

        ScheduledStartUtc = startUtc.ToUniversalTime();
        ScheduledEndUtc = endUtc.ToUniversalTime();
        Status = JobStatus.Scheduled;
        Touch();
    }

    public void Start()
    {
        MoveFrom(JobStatus.Scheduled, JobStatus.InProgress);
    }

    public void Complete()
    {
        MoveFrom(JobStatus.InProgress, JobStatus.Completed);
    }

    public void MarkInvoiced()
    {
        MoveFrom(JobStatus.Completed, JobStatus.Invoiced);
    }

    public void Cancel()
    {
        if (Status is JobStatus.Completed
            or JobStatus.Invoiced
            or JobStatus.Cancelled)
        {
            throw new InvalidOperationException(
                $"A job in {Status} status cannot be cancelled.");
        }

        Status = JobStatus.Cancelled;
        Touch();
    }

    private void MoveFrom(
        JobStatus expectedStatus,
        JobStatus nextStatus)
    {
        if (Status != expectedStatus)
        {
            throw new InvalidOperationException(
                $"A job in {Status} status cannot move to {nextStatus}.");
        }

        Status = nextStatus;
        Touch();
    }

    private void EnsureJobIsOpen()
    {
        if (Status is JobStatus.Completed
            or JobStatus.Invoiced
            or JobStatus.Cancelled)
        {
            throw new InvalidOperationException(
                $"A job in {Status} status can no longer be modified.");
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}