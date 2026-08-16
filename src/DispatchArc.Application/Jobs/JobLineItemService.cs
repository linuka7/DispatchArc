using DispatchArc.Domain.Entities;
using DispatchArc.Domain.Enums;

namespace DispatchArc.Application.Jobs;

public sealed class JobLineItemService
{
    private readonly IJobLineItemRepository _lineItems;
    private readonly IServiceJobRepository _jobs;

    public JobLineItemService(
        IJobLineItemRepository lineItems,
        IServiceJobRepository jobs)
    {
        _lineItems = lineItems;
        _jobs = jobs;
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

        EnsurePricingEditable(job);

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

        EnsurePricingEditable(job);

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

        EnsurePricingEditable(job);

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

    private static void EnsurePricingEditable(
        ServiceJob job)
    {
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