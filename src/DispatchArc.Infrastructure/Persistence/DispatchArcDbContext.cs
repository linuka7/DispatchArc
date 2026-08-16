using DispatchArc.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DispatchArc.Infrastructure.Persistence;

public sealed class DispatchArcDbContext : DbContext
{
    public DispatchArcDbContext(
        DbContextOptions<DispatchArcDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ServiceJob> ServiceJobs => Set<ServiceJob>();
    public DbSet<JobLineItem> JobLineItems => Set<JobLineItem>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<JobNote> JobNotes => Set<JobNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(DispatchArcDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
