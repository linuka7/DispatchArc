using DispatchArc.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DispatchArc.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration
    : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(customer => customer.Id);

        builder.Property(customer => customer.Name)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(customer => customer.Phone)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(customer => customer.Email)
            .HasMaxLength(254);

        builder.Property(customer => customer.AddressLine)
            .HasMaxLength(250);

        builder.Property(customer => customer.City)
            .HasMaxLength(100);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(customer => customer.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(customer => new
        {
            customer.TenantId,
            customer.Name
        });

        builder.HasIndex(customer => new
        {
            customer.TenantId,
            customer.Email
        });
    }
}