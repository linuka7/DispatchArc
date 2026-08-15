using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using DispatchArc.IntegrationTests.Infrastructure;
using Xunit;

namespace DispatchArc.IntegrationTests.TeamMembers;

public sealed class TeamMembersApiTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TeamMembersApiTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task TeamMembers_WithoutToken_ReturnsUnauthorized()
    {
        var tenantId = Guid.NewGuid();

        var response = await _client.GetAsync(
            $"/api/tenants/{tenantId}/team-members");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Owner_CanListAndGetOwnTeamMember()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..10];
        var email = $"team-owner-{uniqueId}@example.com";

        var tenantId = await CreateTenantAsync(
            $"Team Tenant {uniqueId}",
            $"team-{uniqueId}");

        var authentication = await RegisterOwnerAsync(
            tenantId,
            "Team Owner",
            email);

        var userId = Guid.Parse(
            authentication["userId"]!.GetValue<string>());

        var listResponse = await _client.GetAsync(
            $"/api/tenants/{tenantId}/team-members");

        listResponse.EnsureSuccessStatusCode();

        var teamMembers = JsonNode.Parse(
            await listResponse.Content.ReadAsStringAsync())!
            .AsArray();

        var owner = Assert.Single(teamMembers);

        Assert.Equal(
            userId.ToString(),
            owner!["id"]!.GetValue<string>());

        Assert.Equal(
            email,
            owner["email"]!.GetValue<string>());

        Assert.Equal(
            "Owner",
            owner["role"]!.GetValue<string>());

        var getResponse = await _client.GetAsync(
            $"/api/tenants/{tenantId}/team-members/{userId}");

        getResponse.EnsureSuccessStatusCode();

        var teamMember = await ReadObjectAsync(getResponse);

        Assert.Equal(
            userId.ToString(),
            teamMember["id"]!.GetValue<string>());
    }

    [Fact]
    public async Task Owner_CanCreateTechnician()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..10];

        var tenantId = await CreateTenantAsync(
            $"Technician Tenant {uniqueId}",
            $"technician-{uniqueId}");

        await RegisterOwnerAsync(
            tenantId,
            "Technician Manager",
            $"manager-{uniqueId}@example.com");

        var technicianEmail =
            $"technician-{uniqueId}@example.com";

        var response = await _client.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/team-members",
            new
            {
                fullName = "Nimal Technician",
                email = technicianEmail,
                password = "Technician#2026Secure",
                role = "Technician"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var technician = await ReadObjectAsync(response);

        Assert.Equal(
            tenantId.ToString(),
            technician["tenantId"]!.GetValue<string>());

        Assert.Equal(
            "Nimal Technician",
            technician["fullName"]!.GetValue<string>());

        Assert.Equal(
            technicianEmail,
            technician["email"]!.GetValue<string>());

        Assert.Equal(
            "Technician",
            technician["role"]!.GetValue<string>());

        Assert.True(
            technician["isActive"]!.GetValue<bool>());
    }

    [Fact]
    public async Task DuplicateTeamMemberEmail_ReturnsConflict()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..10];

        var tenantId = await CreateTenantAsync(
            $"Duplicate Tenant {uniqueId}",
            $"duplicate-{uniqueId}");

        await RegisterOwnerAsync(
            tenantId,
            "Duplicate Manager",
            $"duplicate-manager-{uniqueId}@example.com");

        var technicianEmail =
            $"duplicate-technician-{uniqueId}@example.com";

        var request = new
        {
            fullName = "Duplicate Technician",
            email = technicianEmail,
            password = "Technician#2026Secure",
            role = "Technician"
        };

        var firstResponse = await _client.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/team-members",
            request);

        firstResponse.EnsureSuccessStatusCode();

        var duplicateResponse = await _client.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/team-members",
            request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task Owner_CannotCreateAnotherOwner()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..10];

        var tenantId = await CreateTenantAsync(
            $"Owner Restriction {uniqueId}",
            $"owner-restriction-{uniqueId}");

        await RegisterOwnerAsync(
            tenantId,
            "Original Owner",
            $"original-owner-{uniqueId}@example.com");

        var response = await _client.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/team-members",
            new
            {
                fullName = "Second Owner",
                email = $"second-owner-{uniqueId}@example.com",
                password = "SecondOwner#2026Secure",
                role = "Owner"
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task TenantA_CannotListTenantBTeamMembers()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..10];

        var tenantAId = await CreateTenantAsync(
            $"Team Security A {uniqueId}",
            $"team-security-a-{uniqueId}");

        var tenantBId = await CreateTenantAsync(
            $"Team Security B {uniqueId}",
            $"team-security-b-{uniqueId}");

        await RegisterOwnerAsync(
            tenantAId,
            "Tenant A Owner",
            $"tenant-a-{uniqueId}@example.com");

        var response = await _client.GetAsync(
            $"/api/tenants/{tenantBId}/team-members");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    private async Task<JsonObject> RegisterOwnerAsync(
        Guid tenantId,
        string fullName,
        string email)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                tenantId,
                fullName,
                email,
                password = "TeamOwner#2026Secure"
            });

        response.EnsureSuccessStatusCode();

        var authentication = await ReadObjectAsync(response);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                authentication["accessToken"]!.GetValue<string>());

        return authentication;
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

        var tenant = await ReadObjectAsync(response);

        return Guid.Parse(
            tenant["id"]!.GetValue<string>());
    }

    private static async Task<JsonObject> ReadObjectAsync(
        HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        return JsonNode.Parse(json)!.AsObject();
    }
}