using DispatchArc.Domain.Entities;
using DispatchArc.Domain.Enums;

namespace DispatchArc.Application.Jobs;

public interface IServiceJobRepository
{
    Task AddAsync(
        ServiceJob job,
        CancellationToken cancellationToken);

    Task<ServiceJob?> GetByIdAsync(
        Guid tenantId,
        Guid jobId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ServiceJob>> GetAllAsync(
        Guid tenantId,
        JobStatus? status,
        string? search,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}
