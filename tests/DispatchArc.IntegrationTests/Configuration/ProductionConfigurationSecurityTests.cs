using Microsoft.Extensions.Configuration;
using System.Text.Json.Nodes;

namespace DispatchArc.IntegrationTests.Configuration;

public sealed class ProductionConfigurationSecurityTests
{
    [Fact]
    public void ProductionAppsettingsContainsNoDatabaseOrJwtSecrets()
    {
        var root =
            FindRepositoryRoot();

        var path =
            Path.Combine(
                root,
                "src",
                "DispatchArc.Api",
                "appsettings.Production.json");

        var json =
            JsonNode.Parse(
                File.ReadAllText(path))!
                .AsObject();

        Assert.False(
            json.ContainsKey(
                "ConnectionStrings"));

        Assert.False(
            json.ContainsKey(
                "Jwt"));
    }

    [Fact]
    public void ProductionEnvironmentTemplateDocumentsRequiredSettings()
    {
        var root =
            FindRepositoryRoot();

        var path =
            Path.Combine(
                root,
                "production.env.example");

        var text =
            File.ReadAllText(path);

        Assert.Contains(
            "ASPNETCORE_ENVIRONMENT=Production",
            text);

        Assert.Contains(
            "ConnectionStrings__Database=",
            text);

        Assert.Contains(
            "Jwt__Issuer=",
            text);

        Assert.Contains(
            "Jwt__Audience=",
            text);

        Assert.Contains(
            "Jwt__Key=",
            text);

        Assert.Contains(
            "Jwt__ExpirationMinutes=",
            text);

        Assert.Contains(
            "ReverseProxy__Enabled=",
            text);
    }

    [Fact]
    public void ProductionEnvironmentTemplateContainsNoRealDatabaseHost()
    {
        var root =
            FindRepositoryRoot();

        var path =
            Path.Combine(
                root,
                "production.env.example");

        var text =
            File.ReadAllText(path);

        Assert.Contains(
            "YOUR_DATABASE_HOST",
            text);

        Assert.Contains(
            "YOUR_DATABASE_PASSWORD",
            text);
    }

    [Fact]
    public void ProductionConfigurationAcceptsStrongNonPlaceholderSettings()
    {
        var configuration =
            new Microsoft.Extensions.Configuration
                .ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Database"] =
                            "Host=db.internal;Port=5432;Database=dispatcharc;Username=dispatcharc_app;Password=StrongProductionPassword987",
                        ["Jwt:Issuer"] =
                            "DispatchArc.Api",
                        ["Jwt:Audience"] =
                            "DispatchArc.Client",
                        ["Jwt:Key"] =
                            "DispatchArc_Strong_Random_Production_Key_9876543210_ABCDEF",
                        ["Jwt:ExpirationMinutes"] =
                            "60"
                    })
                .Build();

        var result =
            DispatchArc.Api.Configuration
                .StartupConfigurationValidator
                .ValidateAndGet(
                    configuration,
                    isProduction: true);

        Assert.Equal(
            60,
            result.Jwt.ExpirationMinutes);

        Assert.Contains(
            "Database=dispatcharc",
            result.DatabaseConnectionString);
    }

    private static string FindRepositoryRoot()
    {
        var directory =
            new DirectoryInfo(
                AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(
                Path.Combine(
                    directory.FullName,
                    "DispatchArc.sln")))
            {
                return directory.FullName;
            }

            directory =
                directory.Parent;
        }

        throw new InvalidOperationException(
            "DispatchArc repository root could not be located.");
    }
}