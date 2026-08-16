using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using DispatchArc.Domain.Enums;
using DispatchArc.Infrastructure.Persistence;
using DispatchArc.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DispatchArc.IntegrationTests.Security;

public sealed class JwtSecurityHardeningTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public JwtSecurityHardeningTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task InactiveUserCannotContinueUsingExistingToken()
    {
        var context =
            await CreateTenantOwnerAsync();

        // Confirm the token works before deactivation.
        var beforeResponse =
            await _client.GetAsync(
                $"/api/tenants/{context.TenantId}/dashboard");

        Assert.Equal(
            HttpStatusCode.OK,
            beforeResponse.StatusCode);

        await DeactivateUserAsync(
            context.OwnerUserId,
            context.TenantId);

        // Same previously issued token must now fail
        // during JWT authentication.
        var afterResponse =
            await _client.GetAsync(
                $"/api/tenants/{context.TenantId}/dashboard");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            afterResponse.StatusCode);
    }

    [Fact]
    public async Task RoleChangeInvalidatesExistingToken()
    {
        var context =
            await CreateTenantOwnerAsync();

        var dispatcher =
            await CreateTeamMemberAsync(
                context.TenantId,
                "Security Dispatcher",
                "Dispatcher");

        await LoginAsync(
            context.TenantId,
            dispatcher.Email,
            dispatcher.Password);

        // Dispatcher is allowed before the role change.
        var beforeResponse =
            await _client.GetAsync(
                $"/api/tenants/{context.TenantId}/team-members");

        Assert.Equal(
            HttpStatusCode.OK,
            beforeResponse.StatusCode);

        await ChangeUserRoleAsync(
            dispatcher.UserId,
            context.TenantId,
            UserRole.Technician);

        // The old token still contains Dispatcher.
        // It must be rejected rather than retaining
        // the stale privilege.
        var afterResponse =
            await _client.GetAsync(
                $"/api/tenants/{context.TenantId}/team-members");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            afterResponse.StatusCode);
    }

    [Fact]
    public async Task MissingUserCannotContinueUsingExistingToken()
    {
        var context =
            await CreateTenantOwnerAsync();

        var beforeResponse =
            await _client.GetAsync(
                $"/api/tenants/{context.TenantId}/dashboard");

        Assert.Equal(
            HttpStatusCode.OK,
            beforeResponse.StatusCode);

        await DeleteUserAsync(
            context.OwnerUserId,
            context.TenantId);

        var afterResponse =
            await _client.GetAsync(
                $"/api/tenants/{context.TenantId}/dashboard");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            afterResponse.StatusCode);
    }
    [Fact]
    public async Task ActiveUserWithCurrentRoleKeepsUsingValidToken()
    {
        var context =
            await CreateTenantOwnerAsync();

        var firstResponse =
            await _client.GetAsync(
                $"/api/tenants/{context.TenantId}/dashboard");

        Assert.Equal(
            HttpStatusCode.OK,
            firstResponse.StatusCode);

        var secondResponse =
            await _client.GetAsync(
                $"/api/tenants/{context.TenantId}/dashboard");

        Assert.Equal(
            HttpStatusCode.OK,
            secondResponse.StatusCode);
    }


    // =====================================================
    // Setup
    // =====================================================

    private async Task<(
        Guid TenantId,
        Guid OwnerUserId,
        string UniqueId)>
        CreateTenantOwnerAsync()
    {
        var uniqueId =
            Guid.NewGuid()
                .ToString("N")[..10];

        var tenantResponse =
            await _client.PostAsJsonAsync(
                "/api/tenants",
                new
                {
                    name =
                        $"Security Test {uniqueId}",
                    slug =
                        $"security-{uniqueId}"
                });

        var tenant =
            await ReadObjectAsync(
                tenantResponse);

        var tenantId =
            GetId(
                tenant);

        var registerResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                new
                {
                    tenantId,
                    fullName =
                        "Security Owner",
                    email =
                        $"security-owner-{uniqueId}@example.com",
                    password =
                        "SecurityOwner#2026"
                });

        var authentication =
            await ReadObjectAsync(
                registerResponse);

        var ownerUserId =
            Guid.Parse(
                authentication["userId"]!
                    .GetValue<string>());

        SetBearerToken(
            authentication["accessToken"]!
                .GetValue<string>());

        return (
            tenantId,
            ownerUserId,
            uniqueId);
    }

    private async Task<(
        Guid UserId,
        string Email,
        string Password)>
        CreateTeamMemberAsync(
            Guid tenantId,
            string fullName,
            string role)
    {
        var uniqueId =
            Guid.NewGuid()
                .ToString("N");

        var email =
            $"security-{role.ToLowerInvariant()}-{uniqueId}@example.com";

        var password =
            $"Security{role}#2026";

        var response =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{tenantId}/team-members",
                new
                {
                    fullName,
                    email,
                    password,
                    role
                });

        var member =
            await ReadObjectAsync(
                response);

        return (
            GetId(member),
            email,
            password);
    }

    private async Task LoginAsync(
        Guid tenantId,
        string email,
        string password)
    {
        var response =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    tenantId,
                    email,
                    password
                });

        var authentication =
            await ReadObjectAsync(
                response);

        SetBearerToken(
            authentication["accessToken"]!
                .GetValue<string>());
    }


    // =====================================================
    // Direct security-state mutations
    // =====================================================

    private async Task DeactivateUserAsync(
        Guid userId,
        Guid tenantId)
    {
        using var scope =
            _factory.Services
                .CreateScope();

        var database =
            scope.ServiceProvider
                .GetRequiredService<DispatchArcDbContext>();

        var user =
            await database.Users
                .SingleAsync(
                    item =>
                        item.Id == userId &&
                        item.TenantId == tenantId);

        user.Deactivate();

        await database
            .SaveChangesAsync();
    }

    private async Task ChangeUserRoleAsync(
        Guid userId,
        Guid tenantId,
        UserRole role)
    {
        using var scope =
            _factory.Services
                .CreateScope();

        var database =
            scope.ServiceProvider
                .GetRequiredService<DispatchArcDbContext>();

        var user =
            await database.Users
                .SingleAsync(
                    item =>
                        item.Id == userId &&
                        item.TenantId == tenantId);

        user.ChangeRole(
            role);

        await database
            .SaveChangesAsync();
    }


    private async Task DeleteUserAsync(
        Guid userId,
        Guid tenantId)
    {
        using var scope =
            _factory.Services
                .CreateScope();

        var database =
            scope.ServiceProvider
                .GetRequiredService<DispatchArcDbContext>();

        var deleted =
            await database.Users
                .Where(user =>
                    user.Id == userId &&
                    user.TenantId == tenantId)
                .ExecuteDeleteAsync();

        Assert.Equal(
            1,
            deleted);
    }

    // =====================================================
    // JSON / auth helpers
    // =====================================================

    private void SetBearerToken(
        string token)
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);
    }

    private static async Task<JsonObject>
        ReadObjectAsync(
            HttpResponseMessage response)
    {
        response
            .EnsureSuccessStatusCode();

        var json =
            await response.Content
                .ReadAsStringAsync();

        return JsonNode.Parse(json)!
            .AsObject();
    }

    private static Guid GetId(
        JsonObject value)
    {
        return Guid.Parse(
            value["id"]!
                .GetValue<string>());
    }
}