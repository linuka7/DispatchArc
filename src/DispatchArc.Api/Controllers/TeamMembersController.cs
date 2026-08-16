using System.Net.Mail;
using DispatchArc.Api.Contracts.TeamMembers;
using DispatchArc.Application.Auth;
using DispatchArc.Application.TeamMembers;
using DispatchArc.Domain.Entities;
using DispatchArc.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DispatchArc.Api.Controllers;

[ApiController]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[Authorize(Policy = "TenantAccess")]
[Authorize(Policy = "DispatchManagement")]
[Route("api/tenants/{tenantId:guid}/team-members")]
public sealed class TeamMembersController(
    TeamMemberService teamMemberService,
    IAppUserRepository users,
    IPasswordHasher<AppUser> passwordHasher) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(TeamMemberResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TeamMemberResponse>> Create(
        Guid tenantId,
        CreateTeamMemberRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(CreateProblem("Invalid team member request", "Full name, email and password are required.", StatusCodes.Status400BadRequest));
        }

        if (!MailAddress.TryCreate(request.Email, out _))
        {
            return BadRequest(CreateProblem("Invalid team member request", "Enter a valid email address.", StatusCodes.Status400BadRequest));
        }

        if (request.Password.Length < 8)
        {
            return BadRequest(CreateProblem("Invalid team member request", "Password must contain at least 8 characters.", StatusCodes.Status400BadRequest));
        }

        if (request.Role == UserRole.Owner)
        {
            return BadRequest(CreateProblem("Invalid team member request", "Additional owners cannot be created through this endpoint.", StatusCodes.Status400BadRequest));
        }

        var existingUser = await users.GetByEmailAsync(
            tenantId,
            request.Email,
            cancellationToken);

        if (existingUser is not null)
        {
            return Conflict(CreateProblem("Team member already exists", "A team member with this email already exists.", StatusCodes.Status409Conflict));
        }

        var user = new AppUser(
            tenantId,
            request.FullName,
            request.Email,
            request.Role);

        var passwordHash = passwordHasher.HashPassword(
            user,
            request.Password);

        user.SetPasswordHash(passwordHash);

        await users.AddAsync(user, cancellationToken);
        await users.SaveChangesAsync(cancellationToken);

        var response = new TeamMemberResponse(
            user.Id,
            user.TenantId,
            user.FullName,
            user.Email,
            user.Role,
            user.IsActive,
            user.CreatedAtUtc,
            user.UpdatedAtUtc);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                tenantId,
                userId = user.Id
            },
            response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TeamMemberResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TeamMemberResponse>>> GetAll(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var teamMembers = await teamMemberService.ListAsync(
            tenantId,
            cancellationToken);

        return Ok(teamMembers);
    }

    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(TeamMemberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamMemberResponse>> GetById(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var teamMember = await teamMemberService.GetByIdAsync(
            tenantId,
            userId,
            cancellationToken);

        return teamMember is null
            ? NotFound()
            : Ok(teamMember);
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
    }}