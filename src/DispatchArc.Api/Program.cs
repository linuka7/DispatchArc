using Microsoft.AspNetCore.Mvc;
using DispatchArc.Api.OpenApi;
using DispatchArc.Api.Configuration;
using DispatchArc.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json.Serialization;
using System.Text;
using DispatchArc.Api.Auth;
using DispatchArc.Application.Alerts;
using DispatchArc.Application.Customers;
using DispatchArc.Application.Dashboard;
using DispatchArc.Application.Jobs;
using DispatchArc.Application.Invoices;
using DispatchArc.Application.Payments;
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
builder.Services.AddDispatchArcSwagger();
builder.Services.AddProblemDetails();
builder.Services.AddDispatchArcProductionHosting(
    builder.Configuration);
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory =
        context =>
        {
            var problem =
                new ValidationProblemDetails(
                    context.ModelState)
                {
                    Title =
                        "Request validation failed",
                    Detail =
                        "One or more request values are invalid.",
                    Status =
                        StatusCodes.Status400BadRequest,
                    Instance =
                        context.HttpContext
                            .Request.Path
                };

            return new BadRequestObjectResult(
                problem);
        };
});

builder.Services.AddScoped<TenantService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<DispatchArc.Application.TeamMembers.TeamMemberService>();
builder.Services.AddScoped<ServiceJobService>();
builder.Services.AddScoped<JobNoteService>();
builder.Services.AddScoped<JobLineItemService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<OperationalAlertService>();

var startupConfiguration =
    StartupConfigurationValidator.ValidateAndGet(
        builder.Configuration,
        builder.Environment.IsProduction());

var jwtOptions =
    startupConfiguration.Jwt;

builder.Services.AddInfrastructure(
    startupConfiguration.DatabaseConnectionString,
    enableRetryOnFailure:
        builder.Environment.IsProduction());
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<CurrentUserTokenValidator>();

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

        options.Events =
            new JwtBearerEvents
            {
                OnTokenValidated =
                    async context =>
                    {
                        var validator =
                            context.HttpContext
                                .RequestServices
                                .GetRequiredService<
                                    CurrentUserTokenValidator>();

                        var failureReason =
                            await validator
                                .GetFailureReasonAsync(
                                    context.Principal,
                                    context.HttpContext
                                        .RequestAborted);

                        if (failureReason is not null)
                        {
                            context.Fail(
                                failureReason);
                        }
                    }
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
    options.AddPolicy(
        "OperationalAlertsAccess",
        policy => policy.RequireRole(
            nameof(UserRole.Owner),
            nameof(UserRole.Dispatcher),
            nameof(UserRole.Finance)));
});

var app = builder.Build();

app.UseDispatchArcSwagger();

app.UseDispatchArcProductionHosting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet(
    "/api/health/live",
    () =>
        Results.Ok(new
        {
            status = "healthy",
            application = "DispatchArc"
        }));

app.MapGet(
    "/api/health/ready",
    async (
        DispatchArcDbContext database,
        CancellationToken cancellationToken) =>
    {
        var canConnect =
            await database.Database
                .CanConnectAsync(
                    cancellationToken);

        return canConnect
            ? Results.Ok(new
            {
                status = "ready",
                service = "PostgreSQL",
                application = "DispatchArc"
            })
            : Results.Problem(
                title:
                    "Application is not ready.",
                detail:
                    "PostgreSQL connectivity is unavailable.",
                statusCode:
                    StatusCodes
                        .Status503ServiceUnavailable);
    });

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
