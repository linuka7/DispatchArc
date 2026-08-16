using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using DispatchArc.IntegrationTests.Infrastructure;
using Xunit;

namespace DispatchArc.IntegrationTests.Jobs;

public sealed class PaymentApiTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PaymentApiTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task InvoiceSupportsPartialThenFullPayment()
    {
        var context =
            await CreateInvoiceAsync(
                invoiceTotal: 10000m);

        var initialResponse =
            await _client.GetAsync(
                $"/api/tenants/{context.TenantId}/invoices/{context.InvoiceId}/payments");

        var initial =
            await ReadObjectAsync(
                initialResponse);

        Assert.Equal(
            "Issued",
            GetString(
                initial,
                "status"));

        Assert.Equal(
            0m,
            GetDecimal(
                initial,
                "amountPaid"));

        Assert.Equal(
            10000m,
            GetDecimal(
                initial,
                "balanceDue"));

        var firstPaymentResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/invoices/{context.InvoiceId}/payments",
                new
                {
                    amount = 4000m,
                    method = "BankTransfer",
                    reference = "BANK-001",
                    paidAtUtc =
                        DateTimeOffset.UtcNow
                });

        var partial =
            await ReadObjectAsync(
                firstPaymentResponse);

        Assert.Equal(
            "PartiallyPaid",
            GetString(
                partial,
                "status"));

        Assert.Equal(
            4000m,
            GetDecimal(
                partial,
                "amountPaid"));

        Assert.Equal(
            6000m,
            GetDecimal(
                partial,
                "balanceDue"));

        Assert.Single(
            partial["payments"]!
                .AsArray());

        var secondPaymentResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/invoices/{context.InvoiceId}/payments",
                new
                {
                    amount = 6000m,
                    method = "Card",
                    reference = "CARD-001",
                    paidAtUtc =
                        DateTimeOffset.UtcNow
                });

        var paid =
            await ReadObjectAsync(
                secondPaymentResponse);

        Assert.Equal(
            "Paid",
            GetString(
                paid,
                "status"));

        Assert.Equal(
            10000m,
            GetDecimal(
                paid,
                "amountPaid"));

        Assert.Equal(
            0m,
            GetDecimal(
                paid,
                "balanceDue"));

        Assert.Equal(
            2,
            paid["payments"]!
                .AsArray()
                .Count);

        var finalResponse =
            await _client.GetAsync(
                $"/api/tenants/{context.TenantId}/invoices/{context.InvoiceId}/payments");

        var final =
            await ReadObjectAsync(
                finalResponse);

        Assert.Equal(
            "Paid",
            GetString(
                final,
                "status"));

        Assert.Equal(
            10000m,
            GetDecimal(
                final,
                "amountPaid"));

        Assert.Equal(
            0m,
            GetDecimal(
                final,
                "balanceDue"));
    }

    [Fact]
    public async Task PaymentCannotExceedRemainingBalance()
    {
        var context =
            await CreateInvoiceAsync(
                invoiceTotal: 10000m);

        var firstResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/invoices/{context.InvoiceId}/payments",
                new
                {
                    amount = 8000m,
                    method = "Cash",
                    reference = "CASH-001"
                });

        firstResponse
            .EnsureSuccessStatusCode();

        var overpaymentResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/invoices/{context.InvoiceId}/payments",
                new
                {
                    amount = 2500m,
                    method = "Cash",
                    reference = "CASH-002"
                });

        Assert.Equal(
            HttpStatusCode.Conflict,
            overpaymentResponse.StatusCode);

        var summaryResponse =
            await _client.GetAsync(
                $"/api/tenants/{context.TenantId}/invoices/{context.InvoiceId}/payments");

        var summary =
            await ReadObjectAsync(
                summaryResponse);

        Assert.Equal(
            8000m,
            GetDecimal(
                summary,
                "amountPaid"));

        Assert.Equal(
            2000m,
            GetDecimal(
                summary,
                "balanceDue"));

        Assert.Equal(
            "PartiallyPaid",
            GetString(
                summary,
                "status"));
    }

    [Fact]
    public async Task DuplicatePaymentReferenceIsRejected()
    {
        var context =
            await CreateInvoiceAsync(
                invoiceTotal: 10000m);

        var firstResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/invoices/{context.InvoiceId}/payments",
                new
                {
                    amount = 2000m,
                    method = "BankTransfer",
                    reference = "TRANSFER-ABC"
                });

        firstResponse
            .EnsureSuccessStatusCode();

        var duplicateResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/invoices/{context.InvoiceId}/payments",
                new
                {
                    amount = 1000m,
                    method = "BankTransfer",
                    reference = "transfer-abc"
                });

        Assert.Equal(
            HttpStatusCode.Conflict,
            duplicateResponse.StatusCode);

        var summaryResponse =
            await _client.GetAsync(
                $"/api/tenants/{context.TenantId}/invoices/{context.InvoiceId}/payments");

        var summary =
            await ReadObjectAsync(
                summaryResponse);

        Assert.Equal(
            2000m,
            GetDecimal(
                summary,
                "amountPaid"));

        Assert.Single(
            summary["payments"]!
                .AsArray());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task InvalidPaymentAmountReturnsBadRequest(
        decimal amount)
    {
        var context =
            await CreateInvoiceAsync(
                invoiceTotal: 10000m);

        var response =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/invoices/{context.InvoiceId}/payments",
                new
                {
                    amount,
                    method = "Cash"
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task PaidInvoiceRejectsAdditionalPayment()
    {
        var context =
            await CreateInvoiceAsync(
                invoiceTotal: 5000m);

        var paidResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/invoices/{context.InvoiceId}/payments",
                new
                {
                    amount = 5000m,
                    method = "Card",
                    reference = "PAID-001"
                });

        paidResponse
            .EnsureSuccessStatusCode();

        var extraResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/invoices/{context.InvoiceId}/payments",
                new
                {
                    amount = 1m,
                    method = "Cash",
                    reference = "PAID-002"
                });

        Assert.Equal(
            HttpStatusCode.Conflict,
            extraResponse.StatusCode);
    }

    [Fact]
    public async Task PaymentLookupIsTenantScoped()
    {
        var first =
            await CreateInvoiceAsync(
                invoiceTotal: 5000m);

        var second =
            await CreateInvoiceAsync(
                invoiceTotal: 3000m);

        var response =
            await _client.GetAsync(
                $"/api/tenants/{second.TenantId}/invoices/{first.InvoiceId}/payments");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    private async Task<(
        Guid TenantId,
        Guid InvoiceId)> CreateInvoiceAsync(
            decimal invoiceTotal)
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
                        $"Payment Test {uniqueId}",
                    slug =
                        $"payment-{uniqueId}"
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
                        "Payment Test Owner",
                    email =
                        $"payment-owner-{uniqueId}@example.com",
                    password =
                        "PaymentOwner#2026Secure"
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
                    name =
                        "Payment Customer",
                    phone =
                        "+94 77 555 5555",
                    email =
                        $"payment-customer-{uniqueId}@example.com",
                    city =
                        "Colombo"
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
                        "Payment integration test job",
                    description =
                        "Payment tracking integration test.",
                    priority =
                        "Normal"
                });

        var job =
            await ReadObjectAsync(
                jobResponse);

        var jobId =
            GetId(job);

        var lineItemResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{tenantId}/jobs/{jobId}/quote/line-items",
                new
                {
                    description =
                        "Service total",
                    quantity =
                        1m,
                    unitPrice =
                        invoiceTotal
                });

        lineItemResponse
            .EnsureSuccessStatusCode();

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
                        "Payment Technician",
                    email =
                        $"payment-tech-{uniqueId}@example.com",
                    password =
                        "PaymentTech#2026Secure",
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

        var invoiceResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{tenantId}/jobs/{jobId}/invoice",
                new
                {
                    dueAtUtc =
                        DateTimeOffset.UtcNow
                            .AddDays(30)
                });

        var invoice =
            await ReadObjectAsync(
                invoiceResponse);

        Assert.Equal(
            invoiceTotal,
            invoice["total"]!
                .GetValue<decimal>());

        return (
            tenantId,
            GetId(invoice));
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

    private static string GetString(
        JsonObject value,
        string property)
    {
        return value[property]!
            .GetValue<string>();
    }

    private static decimal GetDecimal(
        JsonObject value,
        string property)
    {
        return value[property]!
            .GetValue<decimal>();
    }
}