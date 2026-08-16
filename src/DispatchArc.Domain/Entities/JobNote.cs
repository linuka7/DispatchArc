using DispatchArc.Domain.Enums;

namespace DispatchArc.Domain.Entities;

public sealed class JobNote
{
    private JobNote()
    {
    }

    public JobNote(
        Guid tenantId,
        Guid serviceJobId,
        Guid authorUserId,
        JobNoteType type,
        string content)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));

        if (serviceJobId == Guid.Empty)
            throw new ArgumentException("Service job ID is required.", nameof(serviceJobId));

        if (authorUserId == Guid.Empty)
            throw new ArgumentException("Author user ID is required.", nameof(authorUserId));

        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        Id = Guid.NewGuid();
        TenantId = tenantId;
        ServiceJobId = serviceJobId;
        AuthorUserId = authorUserId;
        Type = type;
        Content = content.Trim();
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid ServiceJobId { get; private set; }

    public Guid AuthorUserId { get; private set; }

    public JobNoteType Type { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
