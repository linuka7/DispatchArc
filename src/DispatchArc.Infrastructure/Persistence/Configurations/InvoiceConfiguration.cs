using DispatchArc.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DispatchArc.Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfiguration
    : IEntityTypeConfiguration<Invoice>
{
    public void Configure(
        EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");

        builder.HasKey(invoice => invoice.Id);

        builder.Property(invoice => invoice.InvoiceNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(invoice => invoice.Status)
            .IsRequired();

        builder.Property(invoice => invoice.Subtotal)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(invoice => invoice.Total)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(invoice => invoice.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ServiceJob>()
            .WithMany()
            .HasForeignKey(invoice => invoice.ServiceJobId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(invoice => invoice.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(invoice => new
        {
            invoice.TenantId,
            invoice.InvoiceNumber
        })
        .IsUnique();

        builder.HasIndex(invoice => new
        {
            invoice.TenantId,
            invoice.ServiceJobId
        })
        .IsUnique();
    }
}