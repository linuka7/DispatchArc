using System.Text.Json.Serialization;
using DispatchArc.Application.Customers;
using DispatchArc.Application.Jobs;
using DispatchArc.Application.Tenants;
using DispatchArc.Infrastructure;
using DispatchArc.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

builder.Services.AddScoped<TenantService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<ServiceJobService>();

var connectionString =
    builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException(
        "The database connection string is missing.");

builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.MapGet(
    "/api/health/database",
    async (
        DispatchArcDbContext database,
        CancellationToken cancellationToken) =>
    {
        try
        {
            var canConnect =
                await database.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? Results.Ok(new
                {
                    status = "healthy",
                    service = "PostgreSQL",
                    application = "DispatchArc"
                })
                : Results.Problem(
                    title: "Database unavailable",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch
        {
            return Results.Problem(
                title: "Database unavailable",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    });

app.Run();

public partial class Program
{
}

