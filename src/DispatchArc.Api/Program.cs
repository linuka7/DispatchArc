using DispatchArc.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json.Serialization;
using System.Text;
using DispatchArc.Api.Auth;
using DispatchArc.Application.Customers;
using DispatchArc.Application.Jobs;
using DispatchArc.Application.Tenants;
using DispatchArc.Domain.Entities;
using DispatchArc.Domain.Enums;
using DispatchArc.Infrastructure;
using DispatchArc.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

builder.Services.AddScoped<TenantService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<DispatchArc.Application.TeamMembers.TeamMemberService>();
builder.Services.AddScoped<ServiceJobService>();
builder.Services.AddScoped<JobNoteService>();
builder.Services.AddScoped<JobLineItemService>();

var connectionString =
    builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException(
        "Database connection string is missing.");

builder.Services.AddInfrastructure(connectionString);

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "JWT configuration is missing.");

if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
    string.IsNullOrWhiteSpace(jwtOptions.Audience) ||
    string.IsNullOrWhiteSpace(jwtOptions.Key) ||
    jwtOptions.Key.Length < 32)
{
    throw new InvalidOperationException(
        "JWT issuer, audience and a key of at least 32 characters are required.");
}

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddScoped<
    IPasswordHasher<AppUser>,
    PasswordHasher<AppUser>>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions.Key)),
                NameClaimType = System.Security.Claims.ClaimTypes.Name,
                RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IAuthorizationHandler, TenantAccessHandler>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("TenantAccess", policy =>
        policy.AddRequirements(new TenantAccessRequirement()));
    options.AddPolicy(
        "OwnerOnly",
        policy => policy.RequireRole(
            nameof(UserRole.Owner)));

    options.AddPolicy(
        "DispatchManagement",
        policy => policy.RequireRole(
            nameof(UserRole.Owner),
            nameof(UserRole.Dispatcher)));

    options.AddPolicy(
        "TechnicianAccess",
        policy => policy.RequireRole(
            nameof(UserRole.Owner),
            nameof(UserRole.Dispatcher),
            nameof(UserRole.Technician)));

    options.AddPolicy(
        "FinanceAccess",
        policy => policy.RequireRole(
            nameof(UserRole.Owner),
            nameof(UserRole.Finance)));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet(
    "/api/health/database",
    async (
        DispatchArcDbContext database,
        CancellationToken cancellationToken) =>
    {
        var canConnect = await database.Database
            .CanConnectAsync(cancellationToken);

        return canConnect
            ? Results.Ok(new
            {
                status = "healthy",
                service = "PostgreSQL",
                application = "DispatchArc"
            })
            : Results.Problem(
                title: "Database connection failed.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
    });

app.Run();

public partial class Program
{
}
