using DispatchArc.Domain.Entities;

namespace DispatchArc.Application.Jobs;

public interface IJobLineItemRepository
{
    Task AddAsync(
        JobLineItem item,
        CancellationToken cancellationToken);

    Task<JobLineItem?> GetByIdAsync(
        Guid tenantId,
        Guid serviceJobId,
        Guid lineItemId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<JobLineItem>> GetByJobAsync(
        Guid tenantId,
        Guid serviceJobId,
        CancellationToken cancellationToken);

    void Remove(JobLineItem item);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}