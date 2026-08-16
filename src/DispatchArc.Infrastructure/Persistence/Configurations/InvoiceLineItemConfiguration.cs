using DispatchArc.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DispatchArc.Infrastructure.Persistence.Configurations;

public sealed class InvoiceLineItemConfiguration
    : IEntityTypeConfiguration<InvoiceLineItem>
{
    public void Configure(
        EntityTypeBuilder<InvoiceLineItem> builder)
    {
        builder.ToTable("invoice_line_items");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Description)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(item => item.Quantity)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(item => item.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Ignore(item => item.LineTotal);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(item => item.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Invoice>()
            .WithMany()
            .HasForeignKey(item => item.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => new
        {
            item.TenantId,
            item.InvoiceId
        });
    }
}