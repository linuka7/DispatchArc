using System.Net.Mail;
using DispatchArc.Api.Auth;
using DispatchArc.Application.Auth;
using DispatchArc.Domain.Entities;
using DispatchArc.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DispatchArc.Api.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/auth")]
public sealed class AuthController(
    IAppUserRepository users,
    IPasswordHasher<AppUser> passwordHasher,
    JwtTokenService jwtTokenService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (request.TenantId == Guid.Empty)
        {
            return BadRequest(CreateProblem("Invalid registration request", "Tenant ID is required.", StatusCodes.Status400BadRequest));
        }

        if (string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(CreateProblem("Invalid registration request", "Full name, email and password are required.", StatusCodes.Status400BadRequest));
        }

        if (!MailAddress.TryCreate(request.Email, out _))
        {
            return BadRequest(CreateProblem("Invalid registration request", "Enter a valid email address.", StatusCodes.Status400BadRequest));
        }

        if (request.Password.Length < 8)
        {
            return BadRequest(CreateProblem("Invalid registration request", "Password must contain at least 8 characters.", StatusCodes.Status400BadRequest));
        }

        var existingUser = await users.GetByEmailAsync(
            request.TenantId,
            request.Email,
            cancellationToken);

        if (existingUser is not null)
        {
            return Conflict(CreateProblem("User already exists", "A user with this email already exists.", StatusCodes.Status409Conflict));
        }

        var user = new AppUser(
            request.TenantId,
            request.FullName,
            request.Email,
            UserRole.Owner);

        var passwordHash = passwordHasher.HashPassword(
            user,
            request.Password);

        user.SetPasswordHash(passwordHash);

        await users.AddAsync(user, cancellationToken);
        await users.SaveChangesAsync(cancellationToken);

        var generatedToken = jwtTokenService.Generate(user);

        return StatusCode(
            StatusCodes.Status201Created,
            CreateResponse(user, generatedToken));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByEmailAsync(
            request.TenantId,
            request.Email,
            cancellationToken);

        if (user is null ||
            !user.IsActive ||
            string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return Unauthorized(CreateProblem("Authentication failed", "Invalid email or password.", StatusCodes.Status401Unauthorized));
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized(CreateProblem("Authentication failed", "Invalid email or password.", StatusCodes.Status401Unauthorized));
        }

        var generatedToken = jwtTokenService.Generate(user);

        return Ok(CreateResponse(user, generatedToken));
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
    private static AuthResponse CreateResponse(
        AppUser user,
        GeneratedToken generatedToken)
    {
        return new AuthResponse(
            generatedToken.AccessToken,
            generatedToken.ExpiresAtUtc,
            user.Id,
            user.TenantId,
            user.FullName,
            user.Email,
            user.Role);
    }
}