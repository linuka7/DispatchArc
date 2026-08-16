using System.Security.Claims;
using DispatchArc.Api.Contracts.Jobs;
using DispatchArc.Application.Jobs;
using DispatchArc.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DispatchArc.Api.Controllers;

[ApiController]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[Authorize(Policy = "TenantAccess")]
[Route("api/tenants/{tenantId:guid}/jobs/{jobId:guid}/notes")]
public sealed class JobNotesController : ControllerBase
{
    private readonly JobNoteService _jobNoteService;

    public JobNotesController(JobNoteService jobNoteService)
    {
        _jobNoteService = jobNoteService;
    }

    [HttpGet]
    [Authorize(Policy = "TechnicianAccess")]
    public async Task<ActionResult<IReadOnlyList<JobNoteResponse>>> GetAll(
        Guid tenantId,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        try
        {
            var notes = await _jobNoteService.GetByJobAsync(
                tenantId,
                jobId,
                userId,
                cancellationToken);

            if (notes is null)
                return NotFound();

            if (User.IsInRole(nameof(UserRole.Technician)))
            {
                notes = notes
                    .Where(note =>
                        note.Type == JobNoteType.TechnicianUpdate)
                    .ToList();
            }

            return Ok(notes);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid job note request",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpPost]
    [Authorize(Policy = "TechnicianAccess")]
    public async Task<ActionResult<JobNoteResponse>> Create(
        Guid tenantId,
        Guid jobId,
        AddJobNoteRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Note content is required",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (request.Content.Trim().Length > 4000)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Note content is too long",
                Detail = "Job notes cannot exceed 4000 characters.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (!Enum.IsDefined(request.Type))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid note type",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (User.IsInRole(nameof(UserRole.Technician)) &&
            request.Type != JobNoteType.TechnicianUpdate)
        {
            return Forbid();
        }

        if (!TryGetCurrentUserId(out var authorUserId))
            return Unauthorized();

        try
        {
            var note = await _jobNoteService.AddAsync(
                tenantId,
                jobId,
                authorUserId,
                request.Type,
                request.Content,
                cancellationToken);

            if (note is null)
                return NotFound();

            return CreatedAtAction(
                nameof(GetAll),
                new
                {
                    tenantId,
                    jobId
                },
                note);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid job note",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var value = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out userId);
    }
}
