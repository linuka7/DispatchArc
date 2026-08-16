using DispatchArc.Application.Jobs;
using DispatchArc.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DispatchArc.Infrastructure.Persistence.Repositories;

public sealed class JobNoteRepository : IJobNoteRepository
{
    private readonly DispatchArcDbContext _database;

    public JobNoteRepository(DispatchArcDbContext database)
    {
        _database = database;
    }

    public async Task AddAsync(
        JobNote note,
        CancellationToken cancellationToken)
    {
        await _database.JobNotes.AddAsync(
            note,
            cancellationToken);
    }

    public async Task<IReadOnlyList<JobNote>> GetByJobAsync(
        Guid tenantId,
        Guid serviceJobId,
        CancellationToken cancellationToken)
    {
        return await _database.JobNotes
            .AsNoTracking()
            .Where(note =>
                note.TenantId == tenantId &&
                note.ServiceJobId == serviceJobId)
            .OrderBy(note => note.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return _database.SaveChangesAsync(cancellationToken);
    }
}
