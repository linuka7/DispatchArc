using DispatchArc.Application.Jobs;
using DispatchArc.Domain.Entities;
using DispatchArc.Domain.Enums;

namespace DispatchArc.Application.Invoices;

public sealed class InvoiceService
{
    private readonly IInvoiceRepository _invoices;
    private readonly IInvoiceLineItemRepository _invoiceLineItems;
    private readonly IServiceJobRepository _jobs;
    private readonly IJobLineItemRepository _jobLineItems;

    public InvoiceService(
        IInvoiceRepository invoices,
        IInvoiceLineItemRepository invoiceLineItems,
        IServiceJobRepository jobs,
        IJobLineItemRepository jobLineItems)
    {
        _invoices = invoices;
        _invoiceLineItems = invoiceLineItems;
        _jobs = jobs;
        _jobLineItems = jobLineItems;
    }

    public async Task<InvoiceResponse?> CreateAsync(
        Guid tenantId,
        Guid serviceJobId,
        DateTimeOffset dueAtUtc,
        CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(
            tenantId,
            serviceJobId,
            cancellationToken);

        if (job is null)
            return null;

        if (job.Status != JobStatus.Completed)
        {
            throw new InvalidOperationException(
                "Only completed jobs can be invoiced.");
        }

        var existingInvoice =
            await _invoices.GetByJobAsync(
                tenantId,
                serviceJobId,
                cancellationToken);

        if (existingInvoice is not null)
        {
            throw new InvalidOperationException(
                "An invoice already exists for this job.");
        }

        var quoteItems =
            await _jobLineItems.GetByJobAsync(
                tenantId,
                serviceJobId,
                cancellationToken);

        if (quoteItems.Count == 0)
        {
            throw new InvalidOperationException(
                "The job has no quote line items to invoice.");
        }

        var issuedAtUtc = DateTimeOffset.UtcNow;

        if (dueAtUtc < issuedAtUtc)
        {
            throw new ArgumentException(
                "Invoice due date cannot be before the issue date.",
                nameof(dueAtUtc));
        }

        var subtotal = decimal.Round(
            quoteItems.Sum(item => item.LineTotal),
            2,
            MidpointRounding.AwayFromZero);

        var invoiceNumber =
            $"INV-{issuedAtUtc:yyyyMMdd}-" +
            Guid.NewGuid()
                .ToString("N")[..8]
                .ToUpperInvariant();

        var invoice = new Invoice(
            tenantId,
            job.Id,
            job.CustomerId,
            invoiceNumber,
            issuedAtUtc,
            dueAtUtc,
            subtotal,
            subtotal);

        var invoiceItems = quoteItems
            .Select(item => new InvoiceLineItem(
                tenantId,
                invoice.Id,
                item.Description,
                item.Quantity,
                item.UnitPrice))
            .ToList();

        await _invoices.AddAsync(
            invoice,
            cancellationToken);

        await _invoiceLineItems.AddRangeAsync(
            invoiceItems,
            cancellationToken);

        job.MarkInvoiced();

        // One SaveChanges call commits:
        // invoice + snapshot line items + job status transition.
        await _invoices.SaveChangesAsync(
            cancellationToken);

        return Map(
            invoice,
            invoiceItems);
    }

    public async Task<InvoiceResponse?> GetByIdAsync(
        Guid tenantId,
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        var invoice =
            await _invoices.GetByIdAsync(
                tenantId,
                invoiceId,
                cancellationToken);

        if (invoice is null)
            return null;

        var items =
            await _invoiceLineItems.GetByInvoiceAsync(
                tenantId,
                invoiceId,
                cancellationToken);

        return Map(
            invoice,
            items);
    }

    public async Task<InvoiceResponse?> GetByJobAsync(
        Guid tenantId,
        Guid serviceJobId,
        CancellationToken cancellationToken)
    {
        var invoice =
            await _invoices.GetByJobAsync(
                tenantId,
                serviceJobId,
                cancellationToken);

        if (invoice is null)
            return null;

        var items =
            await _invoiceLineItems.GetByInvoiceAsync(
                tenantId,
                invoice.Id,
                cancellationToken);

        return Map(
            invoice,
            items);
    }

    private static InvoiceResponse Map(
        Invoice invoice,
        IReadOnlyCollection<InvoiceLineItem> items)
    {
        var lineItems = items
            .Select(item =>
                new InvoiceLineItemResponse(
                    item.Id,
                    item.Description,
                    item.Quantity,
                    item.UnitPrice,
                    item.LineTotal))
            .ToList();

        return new InvoiceResponse(
            invoice.Id,
            invoice.TenantId,
            invoice.ServiceJobId,
            invoice.CustomerId,
            invoice.InvoiceNumber,
            invoice.Status,
            invoice.IssuedAtUtc,
            invoice.DueAtUtc,
            invoice.Subtotal,
            invoice.Total,
            lineItems,
            invoice.CreatedAtUtc,
            invoice.UpdatedAtUtc);
    }
}