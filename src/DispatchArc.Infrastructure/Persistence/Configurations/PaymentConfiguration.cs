using DispatchArc.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DispatchArc.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration
    : IEntityTypeConfiguration<Payment>
{
    public void Configure(
        EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.PaymentNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(payment => payment.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(payment => payment.Method)
            .IsRequired();

        builder.Property(payment => payment.Reference)
            .HasMaxLength(150)
            .IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(payment => payment.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Invoice>()
            .WithMany()
            .HasForeignKey(payment => payment.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(payment => new
        {
            payment.TenantId,
            payment.PaymentNumber
        })
        .IsUnique();

        builder.HasIndex(payment => new
        {
            payment.TenantId,
            payment.InvoiceId,
            payment.PaidAtUtc
        });
    }
}