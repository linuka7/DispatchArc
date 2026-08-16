using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using DispatchArc.IntegrationTests.Infrastructure;
using Xunit;
using System.Net.Http.Headers;

namespace DispatchArc.IntegrationTests.Jobs;

public sealed class ServiceJobWorkflowApiTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ServiceJobWorkflowApiTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Job_CanCompleteFullWorkflow()
    {
        var context = await CreateNewJobAsync();

        var quoted = await PostActionAsync(
            context.TenantId,
            context.JobId,
            "quote");

        Assert.Equal("Quoted", GetStatus(quoted));

        var approved = await PostActionAsync(
            context.TenantId,
            context.JobId,
            "approve");

        Assert.Equal("Approved", GetStatus(approved));

        var technicianResponse = await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/team-members",
            new
            {
                fullName = "Integration Technician",
                email = $"tech-{Guid.NewGuid():N}@example.com",
                password = "Technician#2026Secure",
                role = "Technician"
            });

        var technician = await ReadObjectAsync(technicianResponse);
        var technicianId = GetId(technician);

        var assignResponse = await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/jobs/" +
            $"{context.JobId}/assign-technician",
            new
            {
                technicianId
            });

        await ReadObjectAsync(assignResponse);

        var startUtc = DateTimeOffset.UtcNow.AddDays(1);
        var endUtc = startUtc.AddHours(2);

        var scheduleResponse = await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/jobs/" +
            $"{context.JobId}/schedule",
            new
            {
                startUtc,
                endUtc
            });

        var scheduled = await ReadObjectAsync(scheduleResponse);

        Assert.Equal("Scheduled", GetStatus(scheduled));

        var started = await PostActionAsync(
            context.TenantId,
            context.JobId,
            "start");

        Assert.Equal("InProgress", GetStatus(started));

        var completed = await PostActionAsync(
            context.TenantId,
            context.JobId,
            "complete");

        Assert.Equal("Completed", GetStatus(completed));

        var invoiced = await PostActionAsync(
            context.TenantId,
            context.JobId,
            "invoice");

        Assert.Equal("Invoiced", GetStatus(invoiced));

        var finalResponse = await _client.GetAsync(
            $"/api/tenants/{context.TenantId}/jobs/{context.JobId}");

        var finalJob = await ReadObjectAsync(finalResponse);

        Assert.Equal("Invoiced", GetStatus(finalJob));
    }

    [Fact]
    public async Task Job_CannotBeCompletedFromNewStatus()
    {
        var context = await CreateNewJobAsync();

        var response = await _client.PostAsync(
            $"/api/tenants/{context.TenantId}/jobs/" +
            $"{context.JobId}/complete",
            content: null);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    private async Task<(Guid TenantId, Guid JobId)>
        CreateNewJobAsync()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..10];

        var tenantResponse = await _client.PostAsJsonAsync(
            "/api/tenants",
            new
            {
                name = $"Integration Test {uniqueId}",
                slug = $"integration-{uniqueId}"
            });

        var tenant = await ReadObjectAsync(tenantResponse);
        var tenantId = GetId(tenant);

        var registerResponse = await _client.PostAsJsonAsync(
    "/api/auth/register",
    new
    {
        tenantId,
        fullName = "Integration Owner",
        email = $"owner-{uniqueId}@example.com",
        password = "Integration#2026Secure"
    });

var authentication = await ReadObjectAsync(registerResponse);

_client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue(
        "Bearer",
        authentication["accessToken"]!.GetValue<string>());

        var customerResponse = await _client.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/customers",
            new
            {
                name = "Integration Customer",
                phone = "+94 77 000 0000",
                email = $"test-{uniqueId}@example.com",
                city = "Colombo"
            });

        var customer = await ReadObjectAsync(customerResponse);
        var customerId = GetId(customer);

        var jobResponse = await _client.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/jobs",
            new
            {
                customerId,
                title = "Integration test repair",
                description = "Created by an automated API test.",
                priority = "Urgent"
            });

        var job = await ReadObjectAsync(jobResponse);

        Assert.Equal("New", GetStatus(job));

        return (tenantId, GetId(job));
    }

    private async Task<JsonObject> PostActionAsync(
        Guid tenantId,
        Guid jobId,
        string action)
    {
        var response = await _client.PostAsync(
            $"/api/tenants/{tenantId}/jobs/{jobId}/{action}",
            content: null);

        return await ReadObjectAsync(response);
    }

    private static async Task<JsonObject> ReadObjectAsync(
        HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        return JsonNode.Parse(json)!.AsObject();
    }

    private static Guid GetId(JsonObject value)
    {
        return Guid.Parse(
            value["id"]!.GetValue<string>());
    }

    private static string GetStatus(JsonObject value)
    {
        return value["status"]!.GetValue<string>();
    }
}
