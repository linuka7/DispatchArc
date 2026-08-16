using DispatchArc.Application.Auth;
using DispatchArc.Domain.Entities;
using DispatchArc.Domain.Enums;

namespace DispatchArc.Application.Jobs;

public sealed class JobNoteService
{
    private readonly IJobNoteRepository _notes;
    private readonly IServiceJobRepository _jobs;
    private readonly IAppUserRepository _users;

    public JobNoteService(
        IJobNoteRepository notes,
        IServiceJobRepository jobs,
        IAppUserRepository users)
    {
        _notes = notes;
        _jobs = jobs;
        _users = users;
    }

    public async Task<JobNoteResponse?> AddAsync(
        Guid tenantId,
        Guid serviceJobId,
        Guid authorUserId,
        JobNoteType type,
        string content,
        CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(
            tenantId,
            serviceJobId,
            cancellationToken);

        if (job is null)
            return null;

        var author = await _users.GetByIdAsync(
            tenantId,
            authorUserId,
            cancellationToken);

        if (author is null)
        {
            throw new ArgumentException(
                "The note author does not exist in this tenant.");
        }

        if (author.Role == UserRole.Technician)
        {
            if (job.AssignedTechnicianId != authorUserId)
            {
                throw new UnauthorizedAccessException(
                    "Technicians can only update jobs assigned to them.");
            }

            if (type != JobNoteType.TechnicianUpdate)
            {
                throw new UnauthorizedAccessException(
                    "Technicians can only create technician updates.");
            }
        }

        var note = new JobNote(
            tenantId,
            serviceJobId,
            authorUserId,
            type,
            content);

        await _notes.AddAsync(
            note,
            cancellationToken);

        await _notes.SaveChangesAsync(
            cancellationToken);

        return Map(note, author.FullName);
    }

    public async Task<IReadOnlyList<JobNoteResponse>?> GetByJobAsync(
        Guid tenantId,
        Guid serviceJobId,
        Guid viewerUserId,
        CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(
            tenantId,
            serviceJobId,
            cancellationToken);

        if (job is null)
            return null;

        var viewer = await _users.GetByIdAsync(
            tenantId,
            viewerUserId,
            cancellationToken);

        if (viewer is null)
        {
            throw new ArgumentException(
                "The current user does not exist in this tenant.");
        }

        if (viewer.Role == UserRole.Technician &&
            job.AssignedTechnicianId != viewerUserId)
        {
            throw new UnauthorizedAccessException(
                "Technicians can only view updates for jobs assigned to them.");
        }

        var notes = await _notes.GetByJobAsync(
            tenantId,
            serviceJobId,
            cancellationToken);

        var users = await _users.ListByTenantAsync(
            tenantId,
            cancellationToken);

        var authorNames = users.ToDictionary(
            user => user.Id,
            user => user.FullName);

        return notes
            .Select(note => Map(
                note,
                authorNames.GetValueOrDefault(
                    note.AuthorUserId,
                    "Unknown user")))
            .ToList();
    }

    private static JobNoteResponse Map(
        JobNote note,
        string authorFullName)
    {
        return new JobNoteResponse(
            note.Id,
            note.TenantId,
            note.ServiceJobId,
            note.AuthorUserId,
            authorFullName,
            note.Type,
            note.Content,
            note.CreatedAtUtc);
    }
}
