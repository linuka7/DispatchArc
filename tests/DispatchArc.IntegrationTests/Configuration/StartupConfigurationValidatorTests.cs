using DispatchArc.Api.Configuration;
using Microsoft.Extensions.Configuration;

namespace DispatchArc.IntegrationTests.Configuration;

public sealed class StartupConfigurationValidatorTests
{
    [Fact]
    public void DevelopmentConfigurationAcceptsValidSettings()
    {
        var configuration =
            CreateConfiguration(
                database:
                    "Host=localhost;Port=5432;Database=dispatcharc;Username=dispatcharc_dev;Password=local_only",
                jwtKey:
                    "DispatchArc_Development_Test_Key_1234567890",
                expirationMinutes:
                    "60");

        var result =
            StartupConfigurationValidator.ValidateAndGet(
                configuration,
                isProduction: false);

        Assert.Equal(
            60,
            result.Jwt.ExpirationMinutes);

        Assert.Contains(
            "Database=dispatcharc",
            result.DatabaseConnectionString);
    }

    [Fact]
    public void MissingDatabaseConnectionStringIsRejected()
    {
        var configuration =
            CreateConfiguration(
                database: null,
                jwtKey:
                    "DispatchArc_Development_Test_Key_1234567890",
                expirationMinutes:
                    "60");

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    StartupConfigurationValidator.ValidateAndGet(
                        configuration,
                        isProduction: false));

        Assert.Equal(
            "Database connection string is missing.",
            exception.Message);
    }

    [Fact]
    public void ProductionRequiresStrongerJwtKey()
    {
        var configuration =
            CreateConfiguration(
                database:
                    "Host=db.internal;Database=dispatcharc;Username=dispatcharc_app;Password=StrongPassword123",
                jwtKey:
                    "12345678901234567890123456789012",
                expirationMinutes:
                    "60");

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    StartupConfigurationValidator.ValidateAndGet(
                        configuration,
                        isProduction: true));

        Assert.Contains(
            "at least 48 characters",
            exception.Message);
    }

    [Fact]
    public void ProductionRejectsPlaceholderSecrets()
    {
        var configuration =
            CreateConfiguration(
                database:
                    "Host=db.internal;Database=dispatcharc;Username=dispatcharc_app;Password=change_me",
                jwtKey:
                    "DispatchArc_Production_Signing_Key_12345678901234567890",
                expirationMinutes:
                    "60");

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    StartupConfigurationValidator.ValidateAndGet(
                        configuration,
                        isProduction: true));

        Assert.Contains(
            "placeholder value",
            exception.Message);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("1441")]
    public void InvalidJwtExpirationIsRejected(
        string expirationMinutes)
    {
        var configuration =
            CreateConfiguration(
                database:
                    "Host=localhost;Database=dispatcharc",
                jwtKey:
                    "DispatchArc_Development_Test_Key_1234567890",
                expirationMinutes:
                    expirationMinutes);

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    StartupConfigurationValidator.ValidateAndGet(
                        configuration,
                        isProduction: false));

        Assert.Contains(
            "JWT expiration must be between",
            exception.Message);
    }

    private static IConfiguration CreateConfiguration(
        string? database,
        string jwtKey,
        string expirationMinutes)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Database"] = database,
                    ["Jwt:Issuer"] = "DispatchArc.Api",
                    ["Jwt:Audience"] = "DispatchArc.Client",
                    ["Jwt:Key"] = jwtKey,
                    ["Jwt:ExpirationMinutes"] = expirationMinutes
                })
            .Build();
    }
}