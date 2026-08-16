using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using DispatchArc.Infrastructure.Persistence;
using DispatchArc.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DispatchArc.IntegrationTests.Alerts;

public sealed class OperationalAlertsApiTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OperationalAlertsApiTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task OwnerReceivesAllOperationalAlertTypes()
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
                "Alert Technician",
                "Technician");

        var now =
            DateTimeOffset.UtcNow;


        // ---------------------------------------------
        // Approved job needs scheduling
        // ---------------------------------------------

        var approvedJobId =
            await CreateJobAsync(
                context.TenantId,
                customerId,
                "Approved unscheduled alert");

        await PostJobActionAsync(
            context.TenantId,
            approvedJobId,
            "approve");


        // ---------------------------------------------
        // Scheduled job starts within 24 hours
        // ---------------------------------------------

        var startingSoonJobId =
            await CreateJobAsync(
                context.TenantId,
                customerId,
                "Starting soon alert");

        await PostJobActionAsync(
            context.TenantId,
            startingSoonJobId,
            "approve");

        await AssignTechnicianAsync(
            context.TenantId,
            startingSoonJobId,
            technicianId);

        var startingSoonAt =
            now.AddHours(2);

        await ScheduleJobAsync(
            context.TenantId,
            startingSoonJobId,
            startingSoonAt,
            startingSoonAt.AddHours(1));


        // ---------------------------------------------
        // Scheduled start already passed
        // ---------------------------------------------

        var overdueStartJobId =
            await CreateJobAsync(
                context.TenantId,
                customerId,
                "Missed start alert");

        await PostJobActionAsync(
            context.TenantId,
            overdueStartJobId,
            "approve");

        await AssignTechnicianAsync(
            context.TenantId,
            overdueStartJobId,
            technicianId);

        var missedStartAt =
            now.AddHours(-4);

        await ScheduleJobAsync(
            context.TenantId,
            overdueStartJobId,
            missedStartAt,
            missedStartAt.AddHours(1));


        // ---------------------------------------------
        // Completed job needs invoicing
        // ---------------------------------------------

        var completedJobId =
            await CreateJobAsync(
                context.TenantId,
                customerId,
                "Completed uninvoiced alert");

        await PostJobActionAsync(
            context.TenantId,
            completedJobId,
            "approve");

        await AssignTechnicianAsync(
            context.TenantId,
            completedJobId,
            technicianId);

        var completedSchedule =
            now.AddDays(1);

        await ScheduleJobAsync(
            context.TenantId,
            completedJobId,
            completedSchedule,
            completedSchedule.AddHours(1));

        await PostJobActionAsync(
            context.TenantId,
            completedJobId,
            "start");

        await PostJobActionAsync(
            context.TenantId,
            completedJobId,
            "complete");


        // ---------------------------------------------
        // Invoice due soon
        // ---------------------------------------------

        var dueSoonInvoiceId =
            await CreateInvoiceAsync(
                context.TenantId,
                customerId,
                technicianId,
                "Due soon invoice alert",
                10000m,
                now.AddDays(2),
                now.AddDays(4));

        await RecordPaymentAsync(
            context.TenantId,
            dueSoonInvoiceId,
            2500m,
            $"DUE-SOON-{context.UniqueId}");


        // ---------------------------------------------
        // Overdue invoice
        // ---------------------------------------------

        var overdueInvoiceId =
            await CreateInvoiceAsync(
                context.TenantId,
                customerId,
                technicianId,
                "Overdue invoice alert",
                8000m,
                now.AddDays(10),
                now.AddDays(5));

        await RecordPaymentAsync(
            context.TenantId,
            overdueInvoiceId,
            3000m,
            $"OVERDUE-{context.UniqueId}");

        await SetInvoiceDueDateAsync(
            context.TenantId,
            overdueInvoiceId,
            now.AddDays(-1));


        // ---------------------------------------------
        // Owner feed
        // ---------------------------------------------

        var response =
            await _client.GetAsync(
                $"/api/tenants/{context.TenantId}/alerts");

        var feed =
            await ReadObjectAsync(
                response);

        Assert.Equal(
            6,
            GetInt(
                feed,
                "totalCount"));

        Assert.Equal(
            2,
            GetInt(
                feed,
                "criticalCount"));

        Assert.Equal(
            3,
            GetInt(
                feed,
                "warningCount"));

        Assert.Equal(
            1,
            GetInt(
                feed,
                "infoCount"));

        var alerts =
            GetAlerts(feed);

        AssertAlert(
            alerts,
            "ApprovedJobNeedsScheduling",
            "Operations",
            "Warning");

        AssertAlert(
            alerts,
            "ScheduledJobStartingSoon",
            "Operations",
            "Info");

        AssertAlert(
            alerts,
            "ScheduledJobOverdueStart",
            "Operations",
            "Critical");

        AssertAlert(
            alerts,
            "CompletedJobNeedsInvoice",
            "Finance",
            "Warning");

        var dueSoon =
            AssertAlert(
                alerts,
                "InvoiceDueSoon",
                "Finance",
                "Warning");

        Assert.Equal(
            7500m,
            GetDecimal(
                dueSoon,
                "balanceDue"));

        var overdue =
            AssertAlert(
                alerts,
                "InvoiceOverdue",
                "Finance",
                "Critical");

        Assert.Equal(
            5000m,
            GetDecimal(
                overdue,
                "balanceDue"));

        Assert.Equal(
            "Critical",
            alerts[0]!["severity"]!
                .GetValue<string>());

        Assert.Equal(
            "Critical",
            alerts[1]!["severity"]!
                .GetValue<string>());
    }

    [Fact]
    public async Task DispatcherAndFinanceReceiveOnlyTheirAudience()
    {
        var context =
            await CreateTenantOwnerAsync();

        var dispatcherCredentials =
            await CreateUserCredentialsAsync(
                context.TenantId,
                context.UniqueId,
                "Alert Dispatcher",
                "Dispatcher");

        var financeCredentials =
            await CreateUserCredentialsAsync(
                context.TenantId,
                context.UniqueId,
                "Alert Finance",
                "Finance");

        var technicianId =
            await CreateTeamMemberAsync(
                context.TenantId,
                "Filtering Technician",
                "Technician");

        var customerId =
            await CreateCustomerAsync(
                context.TenantId,
                context.UniqueId);


        // Operations alert.
        var approvedJobId =
            await CreateJobAsync(
                context.TenantId,
                customerId,
                "Operations filtering alert");

        await PostJobActionAsync(
            context.TenantId,
            approvedJobId,
            "approve");


        // Finance alert.
        var completedJobId =
            await CreateJobAsync(
                context.TenantId,
                customerId,
                "Finance filtering alert");

        await PostJobActionAsync(
            context.TenantId,
            completedJobId,
            "approve");

        await AssignTechnicianAsync(
            context.TenantId,
            completedJobId,
            technicianId);

        var schedule =
            DateTimeOffset.UtcNow.AddDays(2);

        await ScheduleJobAsync(
            context.TenantId,
            completedJobId,
            schedule,
            schedule.AddHours(1));

        await PostJobActionAsync(
            context.TenantId,
            completedJobId,
            "start");

        await PostJobActionAsync(
            context.TenantId,
            completedJobId,
            "complete");


        // Dispatcher.
        await LoginAsync(
            context.TenantId,
            dispatcherCredentials.Email,
            dispatcherCredentials.Password);

        var dispatcherResponse =
            await _client.GetAsync(
                $"/api/tenants/{context.TenantId}/alerts");

        var dispatcherFeed =
            await ReadObjectAsync(
                dispatcherResponse);

        var dispatcherAlerts =
            GetAlerts(
                dispatcherFeed);

        Assert.NotEmpty(
            dispatcherAlerts);

        Assert.All(
            dispatcherAlerts,
            alert =>
                Assert.Equal(
                    "Operations",
                    alert!["audience"]!
                        .GetValue<string>()));

        Assert.DoesNotContain(
            dispatcherAlerts,
            alert =>
                alert!["type"]!
                    .GetValue<string>() ==
                "CompletedJobNeedsInvoice");


        // Finance.
        await LoginAsync(
            context.TenantId,
            financeCredentials.Email,
            financeCredentials.Password);

        var financeResponse =
            await _client.GetAsync(
                $"/api/tenants/{context.TenantId}/alerts");

        var financeFeed =
            await ReadObjectAsync(
                financeResponse);

        var financeAlerts =
            GetAlerts(
                financeFeed);

        Assert.NotEmpty(
            financeAlerts);

        Assert.All(
            financeAlerts,
            alert =>
                Assert.Equal(
                    "Finance",
                    alert!["audience"]!
                        .GetValue<string>()));

        Assert.Contains(
            financeAlerts,
            alert =>
                alert!["type"]!
                    .GetValue<string>() ==
                "CompletedJobNeedsInvoice");

        Assert.DoesNotContain(
            financeAlerts,
            alert =>
                alert!["type"]!
                    .GetValue<string>() ==
                "ApprovedJobNeedsScheduling");
    }

    [Fact]
    public async Task TechnicianCannotAccessOperationalAlerts()
    {
        var context =
            await CreateTenantOwnerAsync();

        var technicianCredentials =
            await CreateUserCredentialsAsync(
                context.TenantId,
                context.UniqueId,
                "Forbidden Alert Technician",
                "Technician");

        await LoginAsync(
            context.TenantId,
            technicianCredentials.Email,
            technicianCredentials.Password);

        var response =
            await _client.GetAsync(
                $"/api/tenants/{context.TenantId}/alerts");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task OperationalAlertsAreTenantScoped()
    {
        var first =
            await CreateTenantOwnerAsync();

        var second =
            await CreateTenantOwnerAsync();

        var response =
            await _client.GetAsync(
                $"/api/tenants/{first.TenantId}/alerts");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }


    // =====================================================
    // Setup helpers
    // =====================================================

    private async Task<(Guid TenantId, string UniqueId)>
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
                        $"Alert Test {uniqueId}",
                    slug =
                        $"alerts-{uniqueId}"
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
                        "Alert Owner",
                    email =
                        $"alert-owner-{uniqueId}@example.com",
                    password =
                        "AlertOwner#2026"
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
                        "Operational Alert Customer",
                    phone =
                        "+94 77 800 8000",
                    email =
                        $"alert-customer-{uniqueId}@example.com",
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
        string fullName,
        string role)
    {
        var email =
            $"alert-{role.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";

        var response =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{tenantId}/team-members",
                new
                {
                    fullName,
                    email,
                    password =
                        "AlertTeam#2026",
                    role
                });

        var member =
            await ReadObjectAsync(
                response);

        return GetId(member);
    }

    private async Task<(string Email, string Password)>
        CreateUserCredentialsAsync(
            Guid tenantId,
            string uniqueId,
            string fullName,
            string role)
    {
        var email =
            $"alert-{role.ToLowerInvariant()}-{uniqueId}-{Guid.NewGuid():N}@example.com";

        var password =
            $"Alert{role}#2026";

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

        response
            .EnsureSuccessStatusCode();

        return (
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
                        "Operational alert integration test.",
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


    // =====================================================
    // Invoice helpers
    // =====================================================

    private async Task<Guid> CreateInvoiceAsync(
        Guid tenantId,
        Guid customerId,
        Guid technicianId,
        string title,
        decimal amount,
        DateTimeOffset dueAtUtc,
        DateTimeOffset scheduledStartUtc)
    {
        var jobId =
            await CreateJobAsync(
                tenantId,
                customerId,
                title);

        var lineItemResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{tenantId}/jobs/{jobId}/quote/line-items",
                new
                {
                    description =
                        title,
                    quantity =
                        1m,
                    unitPrice =
                        amount
                });

        lineItemResponse
            .EnsureSuccessStatusCode();

        await PostJobActionAsync(
            tenantId,
            jobId,
            "quote");

        await PostJobActionAsync(
            tenantId,
            jobId,
            "approve");

        await AssignTechnicianAsync(
            tenantId,
            jobId,
            technicianId);

        await ScheduleJobAsync(
            tenantId,
            jobId,
            scheduledStartUtc,
            scheduledStartUtc.AddMinutes(30));

        await PostJobActionAsync(
            tenantId,
            jobId,
            "start");

        await PostJobActionAsync(
            tenantId,
            jobId,
            "complete");

        var invoiceResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{tenantId}/jobs/{jobId}/invoice",
                new
                {
                    dueAtUtc
                });

        var invoice =
            await ReadObjectAsync(
                invoiceResponse);

        Assert.Equal(
            amount,
            GetDecimal(
                invoice,
                "total"));

        return GetId(invoice);
    }

    private async Task RecordPaymentAsync(
        Guid tenantId,
        Guid invoiceId,
        decimal amount,
        string reference)
    {
        var response =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{tenantId}/invoices/{invoiceId}/payments",
                new
                {
                    amount,
                    method =
                        "BankTransfer",
                    reference,
                    paidAtUtc =
                        DateTimeOffset.UtcNow
                });

        response
            .EnsureSuccessStatusCode();
    }

    private async Task SetInvoiceDueDateAsync(
        Guid tenantId,
        Guid invoiceId,
        DateTimeOffset dueAtUtc)
    {
        using var scope =
            _factory.Services
                .CreateScope();

        var database =
            scope.ServiceProvider
                .GetRequiredService<DispatchArcDbContext>();

        var updated =
            await database.Invoices
                .Where(invoice =>
                    invoice.TenantId == tenantId &&
                    invoice.Id == invoiceId)
                .ExecuteUpdateAsync(
                    setters =>
                        setters.SetProperty(
                            invoice =>
                                invoice.DueAtUtc,
                            dueAtUtc.ToUniversalTime()));

        Assert.Equal(
            1,
            updated);
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

    private static JsonArray GetAlerts(
        JsonObject feed)
    {
        return feed["alerts"]!
            .AsArray();
    }

    private static JsonObject AssertAlert(
        JsonArray alerts,
        string type,
        string audience,
        string severity)
    {
        var alert =
            alerts
                .Select(node =>
                    node!.AsObject())
                .Single(value =>
                    value["type"]!
                        .GetValue<string>() ==
                    type);

        Assert.Equal(
            audience,
            alert["audience"]!
                .GetValue<string>());

        Assert.Equal(
            severity,
            alert["severity"]!
                .GetValue<string>());

        Assert.False(
            string.IsNullOrWhiteSpace(
                alert["key"]!
                    .GetValue<string>()));

        Assert.False(
            string.IsNullOrWhiteSpace(
                alert["title"]!
                    .GetValue<string>()));

        Assert.False(
            string.IsNullOrWhiteSpace(
                alert["message"]!
                    .GetValue<string>()));

        return alert;
    }
}