using System.Net;
using System.Text.Json.Nodes;
using DispatchArc.IntegrationTests.Infrastructure;

namespace DispatchArc.IntegrationTests.Health;

public sealed class HealthEndpointTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LivenessEndpointReturnsHealthy()
    {
        var response =
            await _client.GetAsync(
                "/api/health/live");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            JsonNode.Parse(
                await response.Content
                    .ReadAsStringAsync())!
                .AsObject();

        Assert.Equal(
            "healthy",
            body["status"]!
                .GetValue<string>());
    }

    [Fact]
    public async Task ReadinessEndpointConfirmsDatabaseConnectivity()
    {
        var response =
            await _client.GetAsync(
                "/api/health/ready");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            JsonNode.Parse(
                await response.Content
                    .ReadAsStringAsync())!
                .AsObject();

        Assert.Equal(
            "ready",
            body["status"]!
                .GetValue<string>());

        Assert.Equal(
            "PostgreSQL",
            body["service"]!
                .GetValue<string>());
    }
}