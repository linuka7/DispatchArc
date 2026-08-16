using DispatchArc.Domain.Entities;

namespace DispatchArc.Application.Jobs;

public interface IJobNoteRepository
{
    Task AddAsync(
        JobNote note,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<JobNote>> GetByJobAsync(
        Guid tenantId,
        Guid serviceJobId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}
