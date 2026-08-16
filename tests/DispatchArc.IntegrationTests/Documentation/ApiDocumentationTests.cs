using System.Net;
using System.Text.Json.Nodes;
using DispatchArc.IntegrationTests.Infrastructure;
using Xunit;

namespace DispatchArc.IntegrationTests.Documentation;

public sealed class ApiDocumentationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiDocumentationTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SwaggerDocumentIsAvailableAndDescribesDispatchArc()
    {
        var response =
            await _client.GetAsync(
                "/swagger/v1/swagger.json");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var json =
            await response.Content
                .ReadAsStringAsync();

        var document =
            JsonNode.Parse(json)!
                .AsObject();

        Assert.Equal(
            "DispatchArc API",
            document["info"]!["title"]!
                .GetValue<string>());

        Assert.Equal(
            "v1",
            document["info"]!["version"]!
                .GetValue<string>());

        var securitySchemes =
            document["components"]![
                "securitySchemes"]!
                .AsObject();

        Assert.NotNull(
            securitySchemes["Bearer"]);

        var paths =
            document["paths"]!
                .AsObject();

        Assert.NotNull(
            paths["/api/auth/login"]);

        Assert.NotNull(
            paths[
                "/api/tenants/{tenantId}/jobs"]);

        Assert.NotNull(
            paths[
                "/api/tenants/{tenantId}/dashboard"]);

        Assert.NotNull(
            paths[
                "/api/tenants/{tenantId}/alerts"]);
    }

    [Fact]
    public async Task SwaggerUiIsAvailableInDevelopment()
    {
        var response =
            await _client.GetAsync(
                "/swagger/index.html");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var html =
            await response.Content
                .ReadAsStringAsync();

        Assert.Contains(
            "swagger-ui",
            html,
            StringComparison.OrdinalIgnoreCase);
    }
}