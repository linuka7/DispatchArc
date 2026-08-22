using DispatchArc.Domain.Entities;
using DispatchArc.Domain.Enums;
using DispatchArc.Application.Invoices;

namespace DispatchArc.Application.Jobs;

public sealed class JobLineItemService
{
    private readonly IJobLineItemRepository _lineItems;
    private readonly IServiceJobRepository _jobs;
    private readonly IInvoiceRepository _invoices;

    public JobLineItemService(
        IJobLineItemRepository lineItems,
        IServiceJobRepository jobs,
        IInvoiceRepository invoices)
    {
        _lineItems = lineItems;
        _jobs = jobs;
        _invoices = invoices;
    }

    public async Task<JobQuoteResponse?> GetQuoteAsync(
        Guid tenantId,
        Guid serviceJobId,
        CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(
            tenantId,
            serviceJobId,
            cancellationToken);

        if (job is null)
            return null;

        var items = await _lineItems.GetByJobAsync(
            tenantId,
            serviceJobId,
            cancellationToken);

        var responses = items
            .Select(Map)
            .ToList();

        var subtotal = decimal.Round(
            responses.Sum(item => item.LineTotal),
            2,
            MidpointRounding.AwayFromZero);

        return new JobQuoteResponse(
            tenantId,
            serviceJobId,
            responses,
            subtotal);
    }

    public async Task<JobLineItemResponse?> AddAsync(
        Guid tenantId,
        Guid serviceJobId,
        string description,
        decimal quantity,
        decimal unitPrice,
        CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(
            tenantId,
            serviceJobId,
            cancellationToken);

        if (job is null)
            return null;

        await EnsurePricingEditableAsync(job, tenantId, cancellationToken);

        var item = new JobLineItem(
            tenantId,
            serviceJobId,
            description,
            quantity,
            unitPrice);

        await _lineItems.AddAsync(
            item,
            cancellationToken);

        await _lineItems.SaveChangesAsync(
            cancellationToken);

        return Map(item);
    }

    public async Task<JobLineItemResponse?> UpdateAsync(
        Guid tenantId,
        Guid serviceJobId,
        Guid lineItemId,
        string description,
        decimal quantity,
        decimal unitPrice,
        CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(
            tenantId,
            serviceJobId,
            cancellationToken);

        if (job is null)
            return null;

        await EnsurePricingEditableAsync(job, tenantId, cancellationToken);

        var item = await _lineItems.GetByIdAsync(
            tenantId,
            serviceJobId,
            lineItemId,
            cancellationToken);

        if (item is null)
            return null;

        item.Update(
            description,
            quantity,
            unitPrice);

        await _lineItems.SaveChangesAsync(
            cancellationToken);

        return Map(item);
    }

    public async Task<bool> DeleteAsync(
        Guid tenantId,
        Guid serviceJobId,
        Guid lineItemId,
        CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(
            tenantId,
            serviceJobId,
            cancellationToken);

        if (job is null)
            return false;

        await EnsurePricingEditableAsync(job, tenantId, cancellationToken);

        var item = await _lineItems.GetByIdAsync(
            tenantId,
            serviceJobId,
            lineItemId,
            cancellationToken);

        if (item is null)
            return false;

        _lineItems.Remove(item);

        await _lineItems.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    private async Task EnsurePricingEditableAsync(
        ServiceJob job,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (job.Status is JobStatus.New or JobStatus.Quoted)
        {
            return;
        }

        if (job.Status == JobStatus.Completed &&
            await _invoices.GetByJobAsync(
                tenantId,
                job.Id,
                cancellationToken) is null)
        {
            return;
        }

        if (job.Status is not JobStatus.New
            and not JobStatus.Quoted)
        {
            throw new InvalidOperationException(
                $"Pricing cannot be changed while the job is in {job.Status} status.");
        }
    }

    private static JobLineItemResponse Map(
        JobLineItem item)
    {
        return new JobLineItemResponse(
            item.Id,
            item.TenantId,
            item.ServiceJobId,
            item.Description,
            item.Quantity,
            item.UnitPrice,
            item.LineTotal,
            item.CreatedAtUtc,
            item.UpdatedAtUtc);
    }
}