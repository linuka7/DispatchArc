using DispatchArc.Application.Auth;
using DispatchArc.Application.Customers;
using DispatchArc.Domain.Entities;
using DispatchArc.Domain.Enums;

namespace DispatchArc.Application.Jobs;

public sealed class ServiceJobService
{
    private readonly IServiceJobRepository _jobs;
    private readonly ICustomerRepository _customers;
    private readonly IAppUserRepository _users;

    public ServiceJobService(
        IServiceJobRepository jobs,
        ICustomerRepository customers,
        IAppUserRepository users)
    {
        _jobs = jobs;
        _customers = customers;
        _users = users;
    }

    public async Task<ServiceJobResponse?> CreateAsync(
        Guid tenantId,
        Guid customerId,
        string title,
        string? description,
        JobPriority priority,
        CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(
            tenantId,
            customerId,
            cancellationToken);

        if (customer is null)
        {
            return null;
        }

        var jobNumber =
            $"JOB-{DateTime.UtcNow:yyyyMMdd}-" +
            Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

        var job = new ServiceJob(
            tenantId,
            customerId,
            jobNumber,
            title,
            description,
            priority);

        await _jobs.AddAsync(job, cancellationToken);

        return Map(job);
    }

    public async Task<ServiceJobResponse?> GetByIdAsync(
        Guid tenantId,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(
            tenantId,
            jobId,
            cancellationToken);

        return job is null ? null : Map(job);
    }

    public async Task<IReadOnlyList<ServiceJobResponse>> GetAllAsync(
        Guid tenantId,
        JobStatus? status,
        string? search,
        CancellationToken cancellationToken)
    {
        var jobs = await _jobs.GetAllAsync(
            tenantId,
            status,
            search,
            cancellationToken);

        return jobs.Select(Map).ToList();
    }

    public Task<ServiceJobResponse?> MarkQuotedAsync(
        Guid tenantId,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        return ApplyWorkflowActionAsync(
            tenantId,
            jobId,
            job => job.MarkQuoted(),
            cancellationToken);
    }

    public Task<ServiceJobResponse?> ApproveAsync(
        Guid tenantId,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        return ApplyWorkflowActionAsync(
            tenantId,
            jobId,
            job => job.Approve(),
            cancellationToken);
    }

    public async Task<ServiceJobResponse?> AssignTechnicianAsync(
        Guid tenantId,
        Guid jobId,
        Guid technicianId,
        CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(
            tenantId,
            jobId,
            cancellationToken);

        if (job is null)
        {
            return null;
        }

        var technician = await _users.GetByIdAsync(
            tenantId,
            technicianId,
            cancellationToken);

        if (technician is null)
        {
            throw new ArgumentException(
                "The selected technician does not exist in this tenant.");
        }

        if (technician.Role != UserRole.Technician)
        {
            throw new ArgumentException(
                "The selected user is not a technician.");
        }

        if (!technician.IsActive)
        {
            throw new ArgumentException(
                "The selected technician is inactive.");
        }

        job.AssignTechnician(technicianId);

        await _jobs.SaveChangesAsync(cancellationToken);

        return Map(job);
    }

    public async Task<ServiceJobResponse?> ScheduleAsync(
        Guid tenantId,
        Guid jobId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(
            tenantId,
            jobId,
            cancellationToken);

        if (job is null)
        {
            return null;
        }

        if (endUtc <= startUtc)
        {
            throw new ArgumentException(
                "The scheduled end must be after the start.");
        }

        if (!job.AssignedTechnicianId.HasValue)
        {
            throw new InvalidOperationException(
                "A technician must be assigned before scheduling the job.");
        }

        var hasConflict = await _jobs.HasSchedulingConflictAsync(
            tenantId,
            job.AssignedTechnicianId.Value,
            job.Id,
            startUtc.ToUniversalTime(),
            endUtc.ToUniversalTime(),
            cancellationToken);

        if (hasConflict)
        {
            throw new InvalidOperationException(
                "The assigned technician already has another job scheduled during this time.");
        }

        job.Schedule(startUtc, endUtc);

        await _jobs.SaveChangesAsync(cancellationToken);

        return Map(job);
    }

    public Task<ServiceJobResponse?> StartAsync(
        Guid tenantId,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        return ApplyWorkflowActionAsync(
            tenantId,
            jobId,
            job => job.Start(),
            cancellationToken);
    }

    public Task<ServiceJobResponse?> CompleteAsync(
        Guid tenantId,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        return ApplyWorkflowActionAsync(
            tenantId,
            jobId,
            job => job.Complete(),
            cancellationToken);
    }

    public Task<ServiceJobResponse?> CancelAsync(
        Guid tenantId,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        return ApplyWorkflowActionAsync(
            tenantId,
            jobId,
            job => job.Cancel(),
            cancellationToken);
    }

    private async Task<ServiceJobResponse?> ApplyWorkflowActionAsync(
        Guid tenantId,
        Guid jobId,
        Action<ServiceJob> action,
        CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(
            tenantId,
            jobId,
            cancellationToken);

        if (job is null)
        {
            return null;
        }

        action(job);

        await _jobs.SaveChangesAsync(cancellationToken);

        return Map(job);
    }

    private static ServiceJobResponse Map(ServiceJob job)
    {
        return new ServiceJobResponse(
            job.Id,
            job.TenantId,
            job.CustomerId,
            job.AssignedTechnicianId,
            job.JobNumber,
            job.Title,
            job.Description,
            job.Priority,
            job.Status,
            job.ScheduledStartUtc,
            job.ScheduledEndUtc,
            job.CreatedAtUtc,
            job.UpdatedAtUtc);
    }
}