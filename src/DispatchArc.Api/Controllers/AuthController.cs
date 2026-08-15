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
[Route("api/auth")]
public sealed class AuthController(
    IAppUserRepository users,
    IPasswordHasher<AppUser> passwordHasher,
    JwtTokenService jwtTokenService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (request.TenantId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "Tenant ID is required."
            });
        }

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

        var existingUser = await users.GetByEmailAsync(
            request.TenantId,
            request.Email,
            cancellationToken);

        if (existingUser is not null)
        {
            return Conflict(new
            {
                message = "A user with this email already exists."
            });
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
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        var generatedToken = jwtTokenService.Generate(user);

        return Ok(CreateResponse(user, generatedToken));
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