using DispatchArc.Api.Contracts.Jobs;
using DispatchArc.Application.Jobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DispatchArc.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantAccess")]
[Route("api/tenants/{tenantId:guid}/jobs/{jobId:guid}/quote")]
public sealed class JobQuoteController : ControllerBase
{
    private readonly JobLineItemService _lineItemService;

    public JobQuoteController(
        JobLineItemService lineItemService)
    {
        _lineItemService = lineItemService;
    }

    [HttpGet]
    [Authorize(Policy = "DispatchManagement")]
    public async Task<ActionResult<JobQuoteResponse>> GetQuote(
        Guid tenantId,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var quote = await _lineItemService.GetQuoteAsync(
            tenantId,
            jobId,
            cancellationToken);

        if (quote is null)
            return NotFound();

        return Ok(quote);
    }

    [HttpPost("line-items")]
    [Authorize(Policy = "DispatchManagement")]
    public async Task<ActionResult<JobLineItemResponse>> AddLineItem(
        Guid tenantId,
        Guid jobId,
        SaveJobLineItemRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var item = await _lineItemService.AddAsync(
                tenantId,
                jobId,
                request.Description,
                request.Quantity,
                request.UnitPrice,
                cancellationToken);

            if (item is null)
                return NotFound();

            return CreatedAtAction(
                nameof(GetQuote),
                new
                {
                    tenantId,
                    jobId
                },
                item);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(CreateProblem(
                "Invalid line item",
                exception.Message,
                StatusCodes.Status400BadRequest));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(CreateProblem(
                "Pricing is locked",
                exception.Message,
                StatusCodes.Status409Conflict));
        }
    }

    [HttpPut("line-items/{lineItemId:guid}")]
    [Authorize(Policy = "DispatchManagement")]
    public async Task<ActionResult<JobLineItemResponse>> UpdateLineItem(
        Guid tenantId,
        Guid jobId,
        Guid lineItemId,
        SaveJobLineItemRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var item = await _lineItemService.UpdateAsync(
                tenantId,
                jobId,
                lineItemId,
                request.Description,
                request.Quantity,
                request.UnitPrice,
                cancellationToken);

            if (item is null)
                return NotFound();

            return Ok(item);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(CreateProblem(
                "Invalid line item",
                exception.Message,
                StatusCodes.Status400BadRequest));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(CreateProblem(
                "Pricing is locked",
                exception.Message,
                StatusCodes.Status409Conflict));
        }
    }

    [HttpDelete("line-items/{lineItemId:guid}")]
    [Authorize(Policy = "DispatchManagement")]
    public async Task<IActionResult> DeleteLineItem(
        Guid tenantId,
        Guid jobId,
        Guid lineItemId,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _lineItemService.DeleteAsync(
                tenantId,
                jobId,
                lineItemId,
                cancellationToken);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(CreateProblem(
                "Pricing is locked",
                exception.Message,
                StatusCodes.Status409Conflict));
        }
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