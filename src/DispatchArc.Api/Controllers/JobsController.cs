using DispatchArc.Api.Contracts.Jobs;
using DispatchArc.Application.Jobs;
using DispatchArc.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace DispatchArc.Api.Controllers;

[ApiController]
[Route("api/tenants/{tenantId:guid}/jobs")]
public sealed class JobsController : ControllerBase
{
    private readonly ServiceJobService _jobService;

    public JobsController(ServiceJobService jobService)
    {
        _jobService = jobService;
    }

    [HttpPost]
    public async Task<ActionResult<ServiceJobResponse>> Create(
        Guid tenantId,
        CreateServiceJobRequest request,
        CancellationToken cancellationToken)
    {
        if (request.CustomerId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Customer ID is required",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var job = await _jobService.CreateAsync(
            tenantId,
            request.CustomerId,
            request.Title,
            request.Description,
            request.Priority,
            cancellationToken);

        if (job is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Customer not found",
                Detail = "The customer does not belong to this tenant.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                tenantId,
                jobId = job.Id
            },
            job);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceJobResponse>>> GetAll(
        Guid tenantId,
        [FromQuery] JobStatus? status,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var jobs = await _jobService.GetAllAsync(
            tenantId,
            status,
            search,
            cancellationToken);

        return Ok(jobs);
    }

    [HttpGet("{jobId:guid}")]
    public async Task<ActionResult<ServiceJobResponse>> GetById(
        Guid tenantId,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var job = await _jobService.GetByIdAsync(
            tenantId,
            jobId,
            cancellationToken);

        return job is null ? NotFound() : Ok(job);
    }

    [HttpPost("{jobId:guid}/quote")]
    public Task<ActionResult<ServiceJobResponse>> Quote(
        Guid tenantId,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        return ExecuteWorkflowActionAsync(() =>
            _jobService.MarkQuotedAsync(
                tenantId,
                jobId,
                cancellationToken));
    }

    [HttpPost("{jobId:guid}/approve")]
    public Task<ActionResult<ServiceJobResponse>> Approve(
        Guid tenantId,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        return ExecuteWorkflowActionAsync(() =>
            _jobService.ApproveAsync(
                tenantId,
                jobId,
                cancellationToken));
    }

    [HttpPost("{jobId:guid}/schedule")]
    public Task<ActionResult<ServiceJobResponse>> Schedule(
        Guid tenantId,
        Guid jobId,
        ScheduleServiceJobRequest request,
        CancellationToken cancellationToken)
    {
        return ExecuteWorkflowActionAsync(() =>
            _jobService.ScheduleAsync(
                tenantId,
                jobId,
                request.StartUtc,
                request.EndUtc,
                cancellationToken));
    }

    [HttpPost("{jobId:guid}/start")]
    public Task<ActionResult<ServiceJobResponse>> Start(
        Guid tenantId,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        return ExecuteWorkflowActionAsync(() =>
            _jobService.StartAsync(
                tenantId,
                jobId,
                cancellationToken));
    }

    [HttpPost("{jobId:guid}/complete")]
    public Task<ActionResult<ServiceJobResponse>> Complete(
        Guid tenantId,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        return ExecuteWorkflowActionAsync(() =>
            _jobService.CompleteAsync(
                tenantId,
                jobId,
                cancellationToken));
    }

    [HttpPost("{jobId:guid}/invoice")]
    public Task<ActionResult<ServiceJobResponse>> Invoice(
        Guid tenantId,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        return ExecuteWorkflowActionAsync(() =>
            _jobService.MarkInvoicedAsync(
                tenantId,
                jobId,
                cancellationToken));
    }

    [HttpPost("{jobId:guid}/cancel")]
    public Task<ActionResult<ServiceJobResponse>> Cancel(
        Guid tenantId,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        return ExecuteWorkflowActionAsync(() =>
            _jobService.CancelAsync(
                tenantId,
                jobId,
                cancellationToken));
    }

    private async Task<ActionResult<ServiceJobResponse>>
        ExecuteWorkflowActionAsync(
            Func<Task<ServiceJobResponse?>> action)
    {
        try
        {
            var job = await action();

            return job is null ? NotFound() : Ok(job);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Invalid workflow transition",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid workflow request",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }
}
