using DispatchArc.Application.Jobs;
using DispatchArc.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DispatchArc.Infrastructure.Persistence.Repositories;

public sealed class JobLineItemRepository
    : IJobLineItemRepository
{
    private readonly DispatchArcDbContext _database;

    public JobLineItemRepository(
        DispatchArcDbContext database)
    {
        _database = database;
    }

    public async Task AddAsync(
        JobLineItem item,
        CancellationToken cancellationToken)
    {
        await _database.JobLineItems.AddAsync(
            item,
            cancellationToken);
    }

    public async Task<JobLineItem?> GetByIdAsync(
        Guid tenantId,
        Guid serviceJobId,
        Guid lineItemId,
        CancellationToken cancellationToken)
    {
        return await _database.JobLineItems
            .FirstOrDefaultAsync(
                item =>
                    item.Id == lineItemId &&
                    item.TenantId == tenantId &&
                    item.ServiceJobId == serviceJobId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<JobLineItem>> GetByJobAsync(
        Guid tenantId,
        Guid serviceJobId,
        CancellationToken cancellationToken)
    {
        return await _database.JobLineItems
            .AsNoTracking()
            .Where(item =>
                item.TenantId == tenantId &&
                item.ServiceJobId == serviceJobId)
            .OrderBy(item => item.CreatedAtUtc)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
    }

    public void Remove(JobLineItem item)
    {
        _database.JobLineItems.Remove(item);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return _database.SaveChangesAsync(cancellationToken);
    }
}