using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using DispatchArc.IntegrationTests.Infrastructure;
using Xunit;

namespace DispatchArc.IntegrationTests.Security;

public sealed class TenantAuthorizationApiTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TenantAuthorizationApiTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task TenantCustomers_WithoutToken_ReturnsUnauthorized()
    {
        var tenantId = Guid.NewGuid();

        var response = await _client.GetAsync(
            $"/api/tenants/{tenantId}/customers");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task TenantA_CannotAccessTenantB_Customers()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..10];

        var tenantAId = await CreateTenantAsync(
            $"Security Tenant A {uniqueId}",
            $"security-a-{uniqueId}");

        var tenantBId = await CreateTenantAsync(
            $"Security Tenant B {uniqueId}",
            $"security-b-{uniqueId}");

        var registerResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                tenantId = tenantAId,
                fullName = "Tenant A Owner",
                email = $"owner-{uniqueId}@example.com",
                password = "Security#2026Secure"
            });

        registerResponse.EnsureSuccessStatusCode();

        var authentication = JsonNode.Parse(
            await registerResponse.Content.ReadAsStringAsync())!
            .AsObject();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                authentication["accessToken"]!.GetValue<string>());

        var response = await _client.GetAsync(
            $"/api/tenants/{tenantBId}/customers");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    private async Task<Guid> CreateTenantAsync(
        string name,
        string slug)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/tenants",
            new
            {
                name,
                slug
            });

        response.EnsureSuccessStatusCode();

        var tenant = JsonNode.Parse(
            await response.Content.ReadAsStringAsync())!
            .AsObject();

        return Guid.Parse(
            tenant["id"]!.GetValue<string>());
    }
}