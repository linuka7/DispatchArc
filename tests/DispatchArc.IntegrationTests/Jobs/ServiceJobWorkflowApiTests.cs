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

        var pricingResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/quote/line-items",
                new
                {
                    description = "Full workflow service",
                    quantity = 1m,
                    unitPrice = 5000m
                });

        pricingResponse.EnsureSuccessStatusCode();
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

        var invoiceResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/invoice",
                new
                {
                    dueAtUtc =
                        DateTimeOffset.UtcNow.AddDays(30)
                });

        var invoice =
            await ReadObjectAsync(
                invoiceResponse);

        Assert.Equal(
            "Issued",
            invoice["status"]!
                .GetValue<string>());
        var finalResponse = await _client.GetAsync(
            $"/api/tenants/{context.TenantId}/jobs/{context.JobId}");

        var finalJob = await ReadObjectAsync(finalResponse);

        Assert.Equal("Invoiced", GetStatus(finalJob));
    }

    [Fact]
    public async Task ApprovedJob_WithoutTechnician_CannotBeScheduled()
    {
        var context = await CreateNewJobAsync();

        await PostActionAsync(
            context.TenantId,
            context.JobId,
            "quote");

        await PostActionAsync(
            context.TenantId,
            context.JobId,
            "approve");

        var startUtc = DateTimeOffset.UtcNow.AddDays(1);
        var endUtc = startUtc.AddHours(1);

        var response = await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/jobs/" +
            $"{context.JobId}/schedule",
            new
            {
                startUtc,
                endUtc
            });

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task Schedule_EndBeforeStart_ReturnsBadRequest()
    {
        var context = await CreateNewJobAsync();

        await PostActionAsync(
            context.TenantId,
            context.JobId,
            "quote");

        await PostActionAsync(
            context.TenantId,
            context.JobId,
            "approve");

        var startUtc = DateTimeOffset.UtcNow.AddDays(1);
        var endUtc = startUtc.AddHours(-1);

        var response = await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/jobs/" +
            $"{context.JobId}/schedule",
            new
            {
                startUtc,
                endUtc
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task AssignTechnician_EmptyId_ReturnsBadRequest()
    {
        var context = await CreateNewJobAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/jobs/" +
            $"{context.JobId}/assign-technician",
            new
            {
                technicianId = Guid.Empty
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task AssignTechnician_NonTechnician_ReturnsBadRequest()
    {
        var context = await CreateNewJobAsync();

        var dispatcherResponse = await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/team-members",
            new
            {
                fullName = "Integration Dispatcher",
                email = $"dispatcher-{Guid.NewGuid():N}@example.com",
                password = "Dispatcher#2026Secure",
                role = "Dispatcher"
            });

        var dispatcher = await ReadObjectAsync(dispatcherResponse);
        var dispatcherId = GetId(dispatcher);

        var response = await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/jobs/" +
            $"{context.JobId}/assign-technician",
            new
            {
                technicianId = dispatcherId
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task AssignTechnician_FromAnotherTenant_ReturnsBadRequest()
    {
        var context = await CreateNewJobAsync();

        var tenantAAuthorization =
            _client.DefaultRequestHeaders.Authorization;

        var uniqueId = Guid.NewGuid().ToString("N")[..10];

        var tenantBResponse = await _client.PostAsJsonAsync(
            "/api/tenants",
            new
            {
                name = $"Other Tenant {uniqueId}",
                slug = $"other-{uniqueId}"
            });

        var tenantB = await ReadObjectAsync(tenantBResponse);
        var tenantBId = GetId(tenantB);

        var registerResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                tenantId = tenantBId,
                fullName = "Other Tenant Owner",
                email = $"owner-b-{uniqueId}@example.com",
                password = "OtherTenant#2026Secure"
            });

        var authentication = await ReadObjectAsync(registerResponse);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                authentication["accessToken"]!.GetValue<string>());

        var technicianResponse = await _client.PostAsJsonAsync(
            $"/api/tenants/{tenantBId}/team-members",
            new
            {
                fullName = "Other Tenant Technician",
                email = $"tech-b-{uniqueId}@example.com",
                password = "Technician#2026Secure",
                role = "Technician"
            });

        var technician = await ReadObjectAsync(technicianResponse);
        var technicianId = GetId(technician);

        _client.DefaultRequestHeaders.Authorization =
            tenantAAuthorization;

        var response = await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/jobs/" +
            $"{context.JobId}/assign-technician",
            new
            {
                technicianId
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Schedule_OverlappingJobsForSameTechnician_ReturnsConflict()
    {
        var context = await CreateNewJobAsync();

        var technicianResponse = await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/team-members",
            new
            {
                fullName = "Conflict Test Technician",
                email = $"conflict-tech-{Guid.NewGuid():N}@example.com",
                password = "Technician#2026Secure",
                role = "Technician"
            });

        var technician = await ReadObjectAsync(technicianResponse);
        var technicianId = GetId(technician);

        await PostActionAsync(context.TenantId, context.JobId, "quote");
        await PostActionAsync(context.TenantId, context.JobId, "approve");

        await ReadObjectAsync(await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/assign-technician",
            new { technicianId }));

        var customerResponse = await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/customers",
            new
            {
                name = "Conflict Test Customer",
                phone = "+94 77 111 1111",
                email = $"conflict-{Guid.NewGuid():N}@example.com",
                city = "Colombo"
            });

        var customer = await ReadObjectAsync(customerResponse);
        var customerId = GetId(customer);

        var secondJobResponse = await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/jobs",
            new
            {
                customerId,
                title = "Second conflict test job",
                description = "Tests technician scheduling overlap.",
                priority = "Normal"
            });

        var secondJob = await ReadObjectAsync(secondJobResponse);
        var secondJobId = GetId(secondJob);

        await PostActionAsync(context.TenantId, secondJobId, "quote");
        await PostActionAsync(context.TenantId, secondJobId, "approve");

        await ReadObjectAsync(await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/jobs/{secondJobId}/assign-technician",
            new { technicianId }));

        var firstStart = DateTimeOffset.UtcNow.AddDays(1);
        var firstEnd = firstStart.AddHours(2);

        await ReadObjectAsync(await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/schedule",
            new
            {
                startUtc = firstStart,
                endUtc = firstEnd
            }));

        var response = await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/jobs/{secondJobId}/schedule",
            new
            {
                startUtc = firstStart.AddHours(1),
                endUtc = firstEnd.AddHours(1)
            });

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task Schedule_BackToBackJobsForSameTechnician_IsAllowed()
    {
        var context = await CreateNewJobAsync();

        var technicianResponse = await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/team-members",
            new
            {
                fullName = "Back To Back Technician",
                email = $"backtoback-{Guid.NewGuid():N}@example.com",
                password = "Technician#2026Secure",
                role = "Technician"
            });

        var technician = await ReadObjectAsync(technicianResponse);
        var technicianId = GetId(technician);

        await PostActionAsync(context.TenantId, context.JobId, "quote");
        await PostActionAsync(context.TenantId, context.JobId, "approve");

        await ReadObjectAsync(await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/assign-technician",
            new { technicianId }));

        var customerResponse = await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/customers",
            new
            {
                name = "Back To Back Customer",
                phone = "+94 77 222 2222",
                email = $"backtoback-{Guid.NewGuid():N}@example.com",
                city = "Colombo"
            });

        var customer = await ReadObjectAsync(customerResponse);
        var customerId = GetId(customer);

        var secondJobResponse = await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/jobs",
            new
            {
                customerId,
                title = "Back to back second job",
                description = "Tests boundary scheduling.",
                priority = "Normal"
            });

        var secondJob = await ReadObjectAsync(secondJobResponse);
        var secondJobId = GetId(secondJob);

        await PostActionAsync(context.TenantId, secondJobId, "quote");
        await PostActionAsync(context.TenantId, secondJobId, "approve");

        await ReadObjectAsync(await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/jobs/{secondJobId}/assign-technician",
            new { technicianId }));

        var firstStart = DateTimeOffset.UtcNow.AddDays(1);
        var firstEnd = firstStart.AddHours(2);

        await ReadObjectAsync(await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/schedule",
            new
            {
                startUtc = firstStart,
                endUtc = firstEnd
            }));

        var response = await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/jobs/{secondJobId}/schedule",
            new
            {
                startUtc = firstEnd,
                endUtc = firstEnd.AddHours(2)
            });

        var scheduled = await ReadObjectAsync(response);

        Assert.Equal("Scheduled", GetStatus(scheduled));
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

    [Fact]
    public async Task Technician_CannotCreateInternalNote()
    {
        var context = await CreateNewJobAsync();

        var technicianEmail =
            $"note-tech-{Guid.NewGuid():N}@example.com";

        const string technicianPassword =
            "Technician#2026Secure";

        var createTechnicianResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/team-members",
                new
                {
                    fullName = "Note Technician",
                    email = technicianEmail,
                    password = technicianPassword,
                    role = "Technician"
                });

        createTechnicianResponse.EnsureSuccessStatusCode();

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                tenantId = context.TenantId,
                email = technicianEmail,
                password = technicianPassword
            });

        var authentication =
            await ReadObjectAsync(loginResponse);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                authentication["accessToken"]!
                    .GetValue<string>());

        var response = await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/notes",
            new
            {
                type = "InternalNote",
                content = "Technician should not create this."
            });

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
    [Fact]
    public async Task Technician_CannotUpdateUnassignedJob()
    {
        var context = await CreateNewJobAsync();

        var technicianEmail =
            $"unassigned-tech-{Guid.NewGuid():N}@example.com";

        const string technicianPassword =
            "Technician#2026Secure";

        var createTechnicianResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/team-members",
                new
                {
                    fullName = "Unassigned Technician",
                    email = technicianEmail,
                    password = technicianPassword,
                    role = "Technician"
                });

        createTechnicianResponse.EnsureSuccessStatusCode();

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                tenantId = context.TenantId,
                email = technicianEmail,
                password = technicianPassword
            });

        var authentication =
            await ReadObjectAsync(loginResponse);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                authentication["accessToken"]!
                    .GetValue<string>());

        var response = await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/notes",
            new
            {
                type = "TechnicianUpdate",
                content = "Attempted update on unassigned job."
            });

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
    [Fact]
    public async Task AssignedTechnician_CanCreateAndReadTechnicianUpdate()
    {
        var context = await CreateNewJobAsync();

        var technicianEmail =
            $"assigned-tech-{Guid.NewGuid():N}@example.com";

        const string technicianPassword =
            "Technician#2026Secure";

        var createTechnicianResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/team-members",
                new
                {
                    fullName = "Assigned Technician",
                    email = technicianEmail,
                    password = technicianPassword,
                    role = "Technician"
                });

        var technician =
            await ReadObjectAsync(createTechnicianResponse);

        var technicianId = GetId(technician);

        var assignResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/jobs/" +
                $"{context.JobId}/assign-technician",
                new
                {
                    technicianId
                });

        assignResponse.EnsureSuccessStatusCode();

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                tenantId = context.TenantId,
                email = technicianEmail,
                password = technicianPassword
            });

        var authentication =
            await ReadObjectAsync(loginResponse);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                authentication["accessToken"]!
                    .GetValue<string>());

        var createUpdateResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/notes",
                new
                {
                    type = "TechnicianUpdate",
                    content = "Arrived on site and started inspection."
                });

        var update =
            await ReadObjectAsync(createUpdateResponse);

        Assert.Equal(
            "TechnicianUpdate",
            update["type"]!.GetValue<string>());

        Assert.Equal(
            "Assigned Technician",
            update["authorFullName"]!.GetValue<string>());

        var getResponse = await _client.GetAsync(
            $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/notes");

        getResponse.EnsureSuccessStatusCode();

        var json =
            await getResponse.Content.ReadAsStringAsync();

        var notes =
            JsonNode.Parse(json)!.AsArray();

        Assert.Single(notes);

        Assert.Equal(
            "Arrived on site and started inspection.",
            notes[0]!["content"]!.GetValue<string>());

        Assert.Equal(
            "TechnicianUpdate",
            notes[0]!["type"]!.GetValue<string>());
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
