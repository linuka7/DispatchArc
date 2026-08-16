using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using DispatchArc.IntegrationTests.Infrastructure;
using Xunit;

namespace DispatchArc.IntegrationTests.Jobs;

public sealed class InvoiceApiTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public InvoiceApiTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CompletedJob_CreatesInvoiceSnapshot()
    {
        var context =
            await CreateJobAsync(
                complete: true,
                withQuoteItems: true);

        var response =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/invoice",
                new
                {
                    dueAtUtc =
                        DateTimeOffset.UtcNow.AddDays(30)
                });

        var invoice =
            await ReadObjectAsync(response);

        Assert.Equal(
            "Issued",
            invoice["status"]!
                .GetValue<string>());

        Assert.Equal(
            9750m,
            invoice["subtotal"]!
                .GetValue<decimal>());

        Assert.Equal(
            9750m,
            invoice["total"]!
                .GetValue<decimal>());

        Assert.Equal(
            2,
            invoice["lineItems"]!
                .AsArray()
                .Count);

        var invoiceId =
            GetId(invoice);

        var byIdResponse =
            await _client.GetAsync(
                $"/api/tenants/{context.TenantId}/invoices/{invoiceId}");

        var byId =
            await ReadObjectAsync(
                byIdResponse);

        Assert.Equal(
            invoiceId,
            GetId(byId));

        var byJobResponse =
            await _client.GetAsync(
                $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/invoice");

        var byJob =
            await ReadObjectAsync(
                byJobResponse);

        Assert.Equal(
            invoiceId,
            GetId(byJob));

        var jobResponse =
            await _client.GetAsync(
                $"/api/tenants/{context.TenantId}/jobs/{context.JobId}");

        var job =
            await ReadObjectAsync(
                jobResponse);

        Assert.Equal(
            "Invoiced",
            job["status"]!
                .GetValue<string>());
    }

    [Fact]
    public async Task JobCannotBeInvoicedBeforeCompletion()
    {
        var context =
            await CreateJobAsync(
                complete: false,
                withQuoteItems: true);

        var response =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/invoice",
                new
                {
                    dueAtUtc =
                        DateTimeOffset.UtcNow.AddDays(30)
                });

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task CompletedJobWithoutQuoteItems_CannotBeInvoiced()
    {
        var context =
            await CreateJobAsync(
                complete: true,
                withQuoteItems: false);

        var response =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/invoice",
                new
                {
                    dueAtUtc =
                        DateTimeOffset.UtcNow.AddDays(30)
                });

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var jobResponse =
            await _client.GetAsync(
                $"/api/tenants/{context.TenantId}/jobs/{context.JobId}");

        var job =
            await ReadObjectAsync(
                jobResponse);

        Assert.Equal(
            "Completed",
            job["status"]!
                .GetValue<string>());
    }

    private async Task<(Guid TenantId, Guid JobId)>
        CreateJobAsync(
            bool complete,
            bool withQuoteItems)
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
                        $"Invoice Test {uniqueId}",
                    slug =
                        $"invoice-{uniqueId}"
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
                        "Invoice Test Owner",
                    email =
                        $"invoice-owner-{uniqueId}@example.com",
                    password =
                        "InvoiceOwner#2026Secure"
                });

        var authentication =
            await ReadObjectAsync(
                registerResponse);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                authentication["accessToken"]!
                    .GetValue<string>());

        var customerResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{tenantId}/customers",
                new
                {
                    name = "Invoice Customer",
                    phone = "+94 77 444 4444",
                    email =
                        $"invoice-customer-{uniqueId}@example.com",
                    city = "Colombo"
                });

        var customer =
            await ReadObjectAsync(
                customerResponse);

        var jobResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{tenantId}/jobs",
                new
                {
                    customerId =
                        GetId(customer),
                    title =
                        "Invoice test job",
                    description =
                        "Invoice integration test.",
                    priority =
                        "Normal"
                });

        var job =
            await ReadObjectAsync(
                jobResponse);

        var jobId =
            GetId(job);

        if (withQuoteItems)
        {
            await AddLineItemAsync(
                tenantId,
                jobId,
                "Labour",
                2m,
                2500m);

            await AddLineItemAsync(
                tenantId,
                jobId,
                "Replacement capacitor",
                1m,
                4750m);
        }

        if (complete)
        {
            await PostActionAsync(
                tenantId,
                jobId,
                "quote");

            await PostActionAsync(
                tenantId,
                jobId,
                "approve");

            var technicianResponse =
                await _client.PostAsJsonAsync(
                    $"/api/tenants/{tenantId}/team-members",
                    new
                    {
                        fullName =
                            "Invoice Technician",
                        email =
                            $"invoice-tech-{uniqueId}@example.com",
                        password =
                            "InvoiceTech#2026Secure",
                        role =
                            "Technician"
                    });

            var technician =
                await ReadObjectAsync(
                    technicianResponse);

            var assignResponse =
                await _client.PostAsJsonAsync(
                    $"/api/tenants/{tenantId}/jobs/{jobId}/assign-technician",
                    new
                    {
                        technicianId =
                            GetId(technician)
                    });

            assignResponse
                .EnsureSuccessStatusCode();

            var startUtc =
                DateTimeOffset.UtcNow
                    .AddDays(1);

            var scheduleResponse =
                await _client.PostAsJsonAsync(
                    $"/api/tenants/{tenantId}/jobs/{jobId}/schedule",
                    new
                    {
                        startUtc,
                        endUtc =
                            startUtc.AddHours(2)
                    });

            scheduleResponse
                .EnsureSuccessStatusCode();

            await PostActionAsync(
                tenantId,
                jobId,
                "start");

            await PostActionAsync(
                tenantId,
                jobId,
                "complete");
        }

        return (
            tenantId,
            jobId);
    }

    private async Task AddLineItemAsync(
        Guid tenantId,
        Guid jobId,
        string description,
        decimal quantity,
        decimal unitPrice)
    {
        var response =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{tenantId}/jobs/{jobId}/quote/line-items",
                new
                {
                    description,
                    quantity,
                    unitPrice
                });

        response.EnsureSuccessStatusCode();
    }

    private async Task<JsonObject> PostActionAsync(
        Guid tenantId,
        Guid jobId,
        string action)
    {
        var response =
            await _client.PostAsync(
                $"/api/tenants/{tenantId}/jobs/{jobId}/{action}",
                content: null);

        return await ReadObjectAsync(
            response);
    }

    private static async Task<JsonObject>
        ReadObjectAsync(
            HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();

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