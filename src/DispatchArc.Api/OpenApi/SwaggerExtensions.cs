using Microsoft.OpenApi;

namespace DispatchArc.Api.OpenApi;

public static class SwaggerExtensions
{
    public static IServiceCollection AddDispatchArcSwagger(
        this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Title = "DispatchArc API",
                    Version = "v1",
                    Description =
                        "Multi-tenant field-service operations API for customers, service jobs, scheduling, quotes, invoices, payments, dashboards and operational alerts."
                });

            options.CustomOperationIds(
                apiDescription =>
                {
                    var routeValues =
                        apiDescription
                            .ActionDescriptor
                            .RouteValues;

                    if (routeValues is null ||
                        !routeValues.TryGetValue(
                            "controller",
                            out var controller) ||
                        !routeValues.TryGetValue(
                            "action",
                            out var action) ||
                        string.IsNullOrWhiteSpace(controller) ||
                        string.IsNullOrWhiteSpace(action))
                    {
                        // Minimal API endpoints such as
                        // /api/health/database do not have
                        // MVC controller/action route values.
                        return null;
                    }

                    return $"{controller}_{action}";
                });
            options.AddSecurityDefinition(
                "Bearer",
                new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description =
                        "Enter the JWT access token returned by the DispatchArc authentication endpoints."
                });

            options.AddSecurityRequirement(
                document =>
                    new OpenApiSecurityRequirement
                    {
                        [
                            new OpenApiSecuritySchemeReference(
                                "Bearer",
                                document)
                        ] = []
                    });
        });

        return services;
    }

    public static WebApplication UseDispatchArcSwagger(
        this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        app.UseSwagger();

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint(
                "/swagger/v1/swagger.json",
                "DispatchArc API v1");

            options.RoutePrefix =
                "swagger";

            options.DocumentTitle =
                "DispatchArc API Documentation";

            options.DisplayRequestDuration();

            options.EnableTryItOutByDefault();
        });

        return app;
    }
}