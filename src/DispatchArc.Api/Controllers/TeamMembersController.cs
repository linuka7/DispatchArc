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
[Authorize(Policy = "TenantAccess")]
[Authorize(Policy = "DispatchManagement")]
[Route("api/tenants/{tenantId:guid}/team-members")]
public sealed class TeamMembersController(
    TeamMemberService teamMemberService,
    IAppUserRepository users,
    IPasswordHasher<AppUser> passwordHasher) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TeamMemberResponse>> Create(
        Guid tenantId,
        CreateTeamMemberRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new
            {
                message = "Full name, email and password are required."
            });
        }

        if (!MailAddress.TryCreate(request.Email, out _))
        {
            return BadRequest(new
            {
                message = "Enter a valid email address."
            });
        }

        if (request.Password.Length < 8)
        {
            return BadRequest(new
            {
                message = "Password must contain at least 8 characters."
            });
        }

        if (request.Role == UserRole.Owner)
        {
            return BadRequest(new
            {
                message = "Additional owners cannot be created through this endpoint."
            });
        }

        var existingUser = await users.GetByEmailAsync(
            tenantId,
            request.Email,
            cancellationToken);

        if (existingUser is not null)
        {
            return Conflict(new
            {
                message = "A team member with this email already exists."
            });
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
}