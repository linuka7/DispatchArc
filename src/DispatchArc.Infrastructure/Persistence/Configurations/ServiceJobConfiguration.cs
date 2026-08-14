using DispatchArc.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DispatchArc.Infrastructure.Persistence.Configurations;

public sealed class ServiceJobConfiguration
    : IEntityTypeConfiguration<ServiceJob>
{
    public void Configure(EntityTypeBuilder<ServiceJob> builder)
    {
        builder.ToTable("service_jobs");

        builder.HasKey(job => job.Id);

        builder.Property(job => job.JobNumber)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(job => job.Title)
            .HasMaxLength(180)
            .IsRequired();

        builder.Property(job => job.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(job => job.Priority)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(job => job.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(job => job.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(job => job.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(job => job.AssignedTechnicianId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(job => new
        {
            job.TenantId,
            job.JobNumber
        }).IsUnique();

        builder.HasIndex(job => new
        {
            job.TenantId,
            job.Status
        });

        builder.HasIndex(job => new
        {
            job.AssignedTechnicianId,
            job.ScheduledStartUtc
        });
    }
}