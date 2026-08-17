using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace DispatchArc.Api.Configuration;

public sealed record ReverseProxyConfiguration(
    bool Enabled,
    IReadOnlyList<IPAddress> KnownProxies);

public static class ReverseProxyConfigurationValidator
{
    public static ReverseProxyConfiguration ValidateAndGet(
        IConfiguration configuration)
    {
        var section =
            configuration.GetSection(
                "ReverseProxy");

        var enabled =
            section.GetValue<bool>(
                "Enabled");

        if (!enabled)
        {
            return new ReverseProxyConfiguration(
                false,
                Array.Empty<IPAddress>());
        }

        var configuredProxies =
            section
                .GetSection("KnownProxies")
                .Get<string[]>()
            ?? Array.Empty<string>();

        if (configuredProxies.Length == 0)
        {
            throw new InvalidOperationException(
                "Reverse proxy forwarding is enabled, but no trusted proxy addresses are configured.");
        }

        var knownProxies =
            new List<IPAddress>();

        foreach (var configuredProxy in
                 configuredProxies)
        {
            if (!IPAddress.TryParse(
                    configuredProxy,
                    out var address))
            {
                throw new InvalidOperationException(
                    $"Reverse proxy address '{configuredProxy}' is not a valid IP address.");
            }

            knownProxies.Add(address);
        }

        return new ReverseProxyConfiguration(
            true,
            knownProxies);
    }
}

public static class ProductionHostingExtensions
{
    public static IServiceCollection
        AddDispatchArcProductionHosting(
            this IServiceCollection services,
            IConfiguration configuration)
    {
        var reverseProxy =
            ReverseProxyConfigurationValidator
                .ValidateAndGet(
                    configuration);

        services.AddSingleton(
            reverseProxy);

        if (reverseProxy.Enabled)
        {
            services.Configure<ForwardedHeadersOptions>(
                options =>
                {
                    options.ForwardedHeaders =
                        ForwardedHeaders
                            .XForwardedFor |
                        ForwardedHeaders
                            .XForwardedProto;

                    options.ForwardLimit = 1;

                    foreach (var proxy in
                             reverseProxy.KnownProxies)
                    {
                        options.KnownProxies.Add(
                            proxy);
                    }
                });
        }

        return services;
    }

    public static WebApplication
        UseDispatchArcProductionHosting(
            this WebApplication app)
    {
        var reverseProxy =
            app.Services
                .GetRequiredService<
                    ReverseProxyConfiguration>();

        if (reverseProxy.Enabled)
        {
            // Must run before HTTPS redirection so the
            // original client scheme is restored safely.
            app.UseForwardedHeaders();
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler();
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        return app;
    }
}