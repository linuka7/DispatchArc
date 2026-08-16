using DispatchArc.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DispatchArc.Infrastructure.Persistence.Configurations;

public sealed class JobNoteConfiguration
    : IEntityTypeConfiguration<JobNote>
{
    public void Configure(EntityTypeBuilder<JobNote> builder)
    {
        builder.ToTable("job_notes");

        builder.HasKey(note => note.Id);

        builder.Property(note => note.Type)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(note => note.Content)
            .HasMaxLength(4000)
            .IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(note => note.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ServiceJob>()
            .WithMany()
            .HasForeignKey(note => note.ServiceJobId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(note => note.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(note => new
        {
            note.TenantId,
            note.ServiceJobId,
            note.CreatedAtUtc
        });
    }
}
