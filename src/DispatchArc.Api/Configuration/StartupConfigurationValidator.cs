using DispatchArc.Api.Auth;
using Microsoft.Extensions.Configuration;

namespace DispatchArc.Api.Configuration;

public sealed record StartupConfigurationResult(
    string DatabaseConnectionString,
    JwtOptions Jwt);

public static class StartupConfigurationValidator
{
    private const int DevelopmentMinimumJwtKeyLength = 32;
    private const int ProductionMinimumJwtKeyLength = 48;

    private const int MinimumExpirationMinutes = 5;
    private const int MaximumExpirationMinutes = 1440;

    public static StartupConfigurationResult ValidateAndGet(
        IConfiguration configuration,
        bool isProduction)
    {
        var connectionString =
            configuration.GetConnectionString(
                "Database");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection string is missing.");
        }

        var jwt =
            configuration
                .GetSection(JwtOptions.SectionName)
                .Get<JwtOptions>()
            ?? throw new InvalidOperationException(
                "JWT configuration is missing.");

        var minimumKeyLength =
            isProduction
                ? ProductionMinimumJwtKeyLength
                : DevelopmentMinimumJwtKeyLength;

        if (string.IsNullOrWhiteSpace(jwt.Issuer) ||
            string.IsNullOrWhiteSpace(jwt.Audience) ||
            string.IsNullOrWhiteSpace(jwt.Key) ||
            jwt.Key.Length < minimumKeyLength)
        {
            throw new InvalidOperationException(
                $"JWT issuer, audience and a key of at least {minimumKeyLength} characters are required.");
        }

        if (jwt.ExpirationMinutes <
                MinimumExpirationMinutes ||
            jwt.ExpirationMinutes >
                MaximumExpirationMinutes)
        {
            throw new InvalidOperationException(
                $"JWT expiration must be between {MinimumExpirationMinutes} and {MaximumExpirationMinutes} minutes.");
        }

        if (isProduction)
        {
            RejectProductionPlaceholder(
                connectionString,
                "database connection string");

            RejectProductionPlaceholder(
                jwt.Key,
                "JWT signing key");
        }

        return new StartupConfigurationResult(
            connectionString,
            jwt);
    }

    private static void RejectProductionPlaceholder(
        string value,
        string settingName)
    {
        var knownUnsafeMarkers =
            new[]
            {
                "change_me",
                "changeme",
                "local_development",
                "not_for_production"
            };

        if (knownUnsafeMarkers.Any(marker =>
                value.Contains(
                    marker,
                    StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"The production {settingName} contains a development or placeholder value.");
        }
    }
}