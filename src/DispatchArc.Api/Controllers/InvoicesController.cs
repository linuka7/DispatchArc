using DispatchArc.Api.Contracts.Invoices;
using DispatchArc.Application.Invoices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DispatchArc.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantAccess")]
[Route("api/tenants/{tenantId:guid}")]
public sealed class InvoicesController : ControllerBase
{
    private readonly InvoiceService _invoiceService;

    public InvoicesController(
        InvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpPost("jobs/{jobId:guid}/invoice")]
    [Authorize(Policy = "FinanceAccess")]
    public async Task<ActionResult<InvoiceResponse>> Create(
        Guid tenantId,
        Guid jobId,
        CreateInvoiceRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dueAtUtc =
                request?.DueAtUtc ??
                DateTimeOffset.UtcNow.AddDays(30);

            var invoice =
                await _invoiceService.CreateAsync(
                    tenantId,
                    jobId,
                    dueAtUtc,
                    cancellationToken);

            if (invoice is null)
                return NotFound();

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    tenantId,
                    invoiceId = invoice.Id
                },
                invoice);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(CreateProblem(
                "Invoice cannot be created",
                exception.Message,
                StatusCodes.Status409Conflict));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(CreateProblem(
                "Invalid invoice request",
                exception.Message,
                StatusCodes.Status400BadRequest));
        }
    }

    [HttpGet("jobs/{jobId:guid}/invoice")]
    [Authorize(Policy = "FinanceAccess")]
    public async Task<ActionResult<InvoiceResponse>> GetByJob(
        Guid tenantId,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var invoice =
            await _invoiceService.GetByJobAsync(
                tenantId,
                jobId,
                cancellationToken);

        return invoice is null
            ? NotFound()
            : Ok(invoice);
    }

    [HttpGet("invoices/{invoiceId:guid}")]
    [Authorize(Policy = "FinanceAccess")]
    public async Task<ActionResult<InvoiceResponse>> GetById(
        Guid tenantId,
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        var invoice =
            await _invoiceService.GetByIdAsync(
                tenantId,
                invoiceId,
                cancellationToken);

        return invoice is null
            ? NotFound()
            : Ok(invoice);
    }

    private static ProblemDetails CreateProblem(
        string title,
        string detail,
        int status)
    {
        return new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = status
        };
    }
}