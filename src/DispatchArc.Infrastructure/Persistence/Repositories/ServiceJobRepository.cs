using DispatchArc.Application.Jobs;
using DispatchArc.Domain.Entities;
using DispatchArc.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DispatchArc.Infrastructure.Persistence.Repositories;

public sealed class ServiceJobRepository : IServiceJobRepository
{
    private readonly DispatchArcDbContext _database;

    public ServiceJobRepository(DispatchArcDbContext database)
    {
        _database = database;
    }

    public async Task AddAsync(
        ServiceJob job,
        CancellationToken cancellationToken)
    {
        await _database.ServiceJobs.AddAsync(
            job,
            cancellationToken);

        await _database.SaveChangesAsync(cancellationToken);
    }

    public Task<ServiceJob?> GetByIdAsync(
        Guid tenantId,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        return _database.ServiceJobs.SingleOrDefaultAsync(
            job =>
                job.TenantId == tenantId &&
                job.Id == jobId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceJob>> GetAllAsync(
        Guid tenantId,
        JobStatus? status,
        string? search,
        CancellationToken cancellationToken)
    {
        var query = _database.ServiceJobs
            .AsNoTracking()
            .Where(job => job.TenantId == tenantId);

        if (status.HasValue)
        {
            query = query.Where(job => job.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";

            query = query.Where(job =>
                EF.Functions.ILike(job.JobNumber, pattern) ||
                EF.Functions.ILike(job.Title, pattern) ||
                EF.Functions.ILike(job.Description, pattern));
        }

        return await query
            .OrderByDescending(job => job.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasSchedulingConflictAsync(
    Guid tenantId,
    Guid technicianId,
    Guid excludedJobId,
    DateTimeOffset startUtc,
    DateTimeOffset endUtc,
    CancellationToken cancellationToken)
{
    return _database.ServiceJobs.AnyAsync(
        job =>
            job.TenantId == tenantId &&
            job.AssignedTechnicianId == technicianId &&
            job.Id != excludedJobId &&
            job.Status != JobStatus.Cancelled &&
            job.ScheduledStartUtc.HasValue &&
            job.ScheduledEndUtc.HasValue &&
            job.ScheduledStartUtc < endUtc &&
            job.ScheduledEndUtc > startUtc,
        cancellationToken);
}

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return _database.SaveChangesAsync(cancellationToken);
    }
}
