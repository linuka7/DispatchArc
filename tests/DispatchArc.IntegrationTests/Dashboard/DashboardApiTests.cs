using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using DispatchArc.IntegrationTests.Infrastructure;
using Xunit;

namespace DispatchArc.IntegrationTests.Dashboard;

public sealed class DashboardApiTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DashboardApiTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DashboardReturnsTenantBusinessMetrics()
    {
        var context =
            await CreateTenantOwnerAsync();

        var customerId =
            await CreateCustomerAsync(
                context.TenantId,
                context.UniqueId);

        var technicianId =
            await CreateTeamMemberAsync(
                context.TenantId,
                context.UniqueId,
                "Dashboard Technician",
                "Technician");

        // ---------------------------------------------
        // Job 1:
        // Invoiced for 10,000 and partially paid 4,000.
        // ---------------------------------------------

        var invoiceJobId =
            await CreateJobAsync(
                context.TenantId,
                customerId,
                "Invoice dashboard job");

        var lineItemResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/jobs/{invoiceJobId}/quote/line-items",
                new
                {
                    description =
                        "Dashboard service",
                    quantity = 1m,
                    unitPrice = 10000m
                });

        lineItemResponse
            .EnsureSuccessStatusCode();

        await PostJobActionAsync(
            context.TenantId,
            invoiceJobId,
            "quote");

        await PostJobActionAsync(
            context.TenantId,
            invoiceJobId,
            "approve");

        await AssignTechnicianAsync(
            context.TenantId,
            invoiceJobId,
            technicianId);

        var tomorrowStart =
            DateTimeOffset.UtcNow
                .Date
                .AddDays(1)
                .AddHours(8);

        await ScheduleJobAsync(
            context.TenantId,
            invoiceJobId,
            tomorrowStart,
            tomorrowStart.AddHours(2));

        await PostJobActionAsync(
            context.TenantId,
            invoiceJobId,
            "start");

        await PostJobActionAsync(
            context.TenantId,
            invoiceJobId,
            "complete");

        var invoiceResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/jobs/{invoiceJobId}/invoice",
                new
                {
                    dueAtUtc =
                        DateTimeOffset.UtcNow
                            .AddDays(30)
                });

        var invoice =
            await ReadObjectAsync(
                invoiceResponse);

        var invoiceId =
            GetId(invoice);

        var paymentResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/invoices/{invoiceId}/payments",
                new
                {
                    amount = 4000m,
                    method = "BankTransfer",
                    reference =
                        $"DASH-{context.UniqueId}",
                    paidAtUtc =
                        DateTimeOffset.UtcNow
                });

        paymentResponse
            .EnsureSuccessStatusCode();


        // ---------------------------------------------
        // Job 2:
        // Scheduled during the current UTC day.
        // ---------------------------------------------

        var scheduledJobId =
            await CreateJobAsync(
                context.TenantId,
                customerId,
                "Scheduled today dashboard job");

        await PostJobActionAsync(
            context.TenantId,
            scheduledJobId,
            "approve");

        await AssignTechnicianAsync(
            context.TenantId,
            scheduledJobId,
            technicianId);

        var todayStart =
            new DateTimeOffset(
                DateTime.UtcNow.Date,
                TimeSpan.Zero)
                .AddHours(2);

        await ScheduleJobAsync(
            context.TenantId,
            scheduledJobId,
            todayStart,
            todayStart.AddHours(1));


        // ---------------------------------------------
        // Job 3:
        // Remains New.
        // ---------------------------------------------

        await CreateJobAsync(
            context.TenantId,
            customerId,
            "New dashboard job");


        // ---------------------------------------------
        // Dashboard
        // ---------------------------------------------

        var response =
            await _client.GetAsync(
                $"/api/tenants/{context.TenantId}/dashboard");

        var dashboard =
            await ReadObjectAsync(
                response);

        Assert.Equal(
            1,
            GetInt(
                dashboard,
                "totalCustomers"));

        Assert.Equal(
            1,
            GetInt(
                dashboard,
                "activeTechnicians"));

        Assert.Equal(
            3,
            GetInt(
                dashboard,
                "totalJobs"));

        Assert.Equal(
            2,
            GetInt(
                dashboard,
                "openJobs"));

        Assert.Equal(
            1,
            GetInt(
                dashboard,
                "scheduledToday"));

        Assert.Equal(
            1,
            GetJobStatusCount(
                dashboard,
                "New"));

        Assert.Equal(
            1,
            GetJobStatusCount(
                dashboard,
                "Scheduled"));

        Assert.Equal(
            1,
            GetJobStatusCount(
                dashboard,
                "Invoiced"));

        Assert.Equal(
            0,
            GetJobStatusCount(
                dashboard,
                "Cancelled"));

        Assert.Equal(
            10000m,
            GetDecimal(
                dashboard,
                "totalInvoiced"));

        Assert.Equal(
            4000m,
            GetDecimal(
                dashboard,
                "totalCollected"));

        Assert.Equal(
            4000m,
            GetDecimal(
                dashboard,
                "collectedThisMonth"));

        Assert.Equal(
            1,
            GetInt(
                dashboard,
                "outstandingInvoiceCount"));

        Assert.Equal(
            6000m,
            GetDecimal(
                dashboard,
                "outstandingBalance"));

        Assert.Equal(
            0,
            GetInt(
                dashboard,
                "overdueInvoiceCount"));

        Assert.Equal(
            0m,
            GetDecimal(
                dashboard,
                "overdueBalance"));
    }

    [Fact]
    public async Task DashboardIsTenantScoped()
    {
        var first =
            await CreateTenantOwnerAsync();

        var second =
            await CreateTenantOwnerAsync();

        // CreateTenantOwnerAsync leaves the second
        // tenant owner's token active.

        var response =
            await _client.GetAsync(
                $"/api/tenants/{first.TenantId}/dashboard");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task DispatcherCannotAccessOwnerDashboard()
    {
        var context =
            await CreateTenantOwnerAsync();

        var dispatcherEmail =
            $"dashboard-dispatcher-{context.UniqueId}@example.com";

        var dispatcherPassword =
            "DashboardDispatcher#2026";

        var createResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/team-members",
                new
                {
                    fullName =
                        "Dashboard Dispatcher",
                    email =
                        dispatcherEmail,
                    password =
                        dispatcherPassword,
                    role =
                        "Dispatcher"
                });

        createResponse
            .EnsureSuccessStatusCode();

        var loginResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    tenantId =
                        context.TenantId,
                    email =
                        dispatcherEmail,
                    password =
                        dispatcherPassword
                });

        var login =
            await ReadObjectAsync(
                loginResponse);

        SetBearerToken(
            login["accessToken"]!
                .GetValue<string>());

        var response =
            await _client.GetAsync(
                $"/api/tenants/{context.TenantId}/dashboard");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    private async Task<(
        Guid TenantId,
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
                        $"Dashboard Test {uniqueId}",
                    slug =
                        $"dashboard-{uniqueId}"
                });

        var tenant =
            await ReadObjectAsync(
                tenantResponse);

        var tenantId =
            GetId(tenant);

        var registerResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                new
                {
                    tenantId,
                    fullName =
                        "Dashboard Owner",
                    email =
                        $"dashboard-owner-{uniqueId}@example.com",
                    password =
                        "DashboardOwner#2026"
                });

        var authentication =
            await ReadObjectAsync(
                registerResponse);

        SetBearerToken(
            authentication["accessToken"]!
                .GetValue<string>());

        return (
            tenantId,
            uniqueId);
    }

    private async Task<Guid> CreateCustomerAsync(
        Guid tenantId,
        string uniqueId)
    {
        var response =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{tenantId}/customers",
                new
                {
                    name =
                        "Dashboard Customer",
                    phone =
                        "+94 77 700 7000",
                    email =
                        $"dashboard-customer-{uniqueId}@example.com",
                    city =
                        "Colombo"
                });

        var customer =
            await ReadObjectAsync(
                response);

        return GetId(customer);
    }

    private async Task<Guid> CreateTeamMemberAsync(
        Guid tenantId,
        string uniqueId,
        string fullName,
        string role)
    {
        var response =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{tenantId}/team-members",
                new
                {
                    fullName,
                    email =
                        $"dashboard-{role.ToLowerInvariant()}-{uniqueId}@example.com",
                    password =
                        "DashboardTeam#2026",
                    role
                });

        var member =
            await ReadObjectAsync(
                response);

        return GetId(member);
    }

    private async Task<Guid> CreateJobAsync(
        Guid tenantId,
        Guid customerId,
        string title)
    {
        var response =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{tenantId}/jobs",
                new
                {
                    customerId,
                    title,
                    description =
                        "Dashboard integration test.",
                    priority =
                        "Normal"
                });

        var job =
            await ReadObjectAsync(
                response);

        return GetId(job);
    }

    private async Task AssignTechnicianAsync(
        Guid tenantId,
        Guid jobId,
        Guid technicianId)
    {
        var response =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{tenantId}/jobs/{jobId}/assign-technician",
                new
                {
                    technicianId
                });

        response
            .EnsureSuccessStatusCode();
    }

    private async Task ScheduleJobAsync(
        Guid tenantId,
        Guid jobId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc)
    {
        var response =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{tenantId}/jobs/{jobId}/schedule",
                new
                {
                    startUtc,
                    endUtc
                });

        response
            .EnsureSuccessStatusCode();
    }

    private async Task PostJobActionAsync(
        Guid tenantId,
        Guid jobId,
        string action)
    {
        var response =
            await _client.PostAsync(
                $"/api/tenants/{tenantId}/jobs/{jobId}/{action}",
                content: null);

        response
            .EnsureSuccessStatusCode();
    }

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

    private static int GetInt(
        JsonObject value,
        string property)
    {
        return value[property]!
            .GetValue<int>();
    }

    private static decimal GetDecimal(
        JsonObject value,
        string property)
    {
        return value[property]!
            .GetValue<decimal>();
    }

    private static int GetJobStatusCount(
        JsonObject dashboard,
        string status)
    {
        var statuses =
            dashboard["jobsByStatus"]!
                .AsArray();

        var item =
            statuses
                .Select(node =>
                    node!.AsObject())
                .Single(value =>
                    value["status"]!
                        .GetValue<string>() ==
                    status);

        return item["count"]!
            .GetValue<int>();
    }
}