using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using DispatchArc.IntegrationTests.Infrastructure;
using Xunit;

namespace DispatchArc.IntegrationTests.Jobs;

public sealed class JobQuoteApiTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public JobQuoteApiTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task JobQuote_LineItemsCalculateSubtotalAndSupportUpdateDelete()
    {
        var context = await CreateNewJobAsync();

        var labourResponse = await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/quote/line-items",
            new
            {
                description = "Labour",
                quantity = 2m,
                unitPrice = 2500m
            });

        var labour = await ReadObjectAsync(labourResponse);
        var labourId = GetId(labour);

        Assert.Equal(
            5000m,
            labour["lineTotal"]!.GetValue<decimal>());

        var partResponse = await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/quote/line-items",
            new
            {
                description = "Replacement capacitor",
                quantity = 1m,
                unitPrice = 4750m
            });

        var part = await ReadObjectAsync(partResponse);
        var partId = GetId(part);

        var callOutResponse = await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/quote/line-items",
            new
            {
                description = "Call-out fee",
                quantity = 1m,
                unitPrice = 1500m
            });

        await ReadObjectAsync(callOutResponse);

        var quoteResponse = await _client.GetAsync(
            $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/quote");

        var quote = await ReadObjectAsync(quoteResponse);

        Assert.Equal(
            11250m,
            quote["subtotal"]!.GetValue<decimal>());

        Assert.Equal(
            3,
            quote["lineItems"]!.AsArray().Count);

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/quote/line-items/{labourId}",
            new
            {
                description = "Labour",
                quantity = 3m,
                unitPrice = 2500m
            });

        var updatedLabour =
            await ReadObjectAsync(updateResponse);

        Assert.Equal(
            7500m,
            updatedLabour["lineTotal"]!.GetValue<decimal>());

        var deleteResponse = await _client.DeleteAsync(
            $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/quote/line-items/{partId}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var finalQuoteResponse = await _client.GetAsync(
            $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/quote");

        var finalQuote =
            await ReadObjectAsync(finalQuoteResponse);

        Assert.Equal(
            9000m,
            finalQuote["subtotal"]!.GetValue<decimal>());

        Assert.Equal(
            2,
            finalQuote["lineItems"]!.AsArray().Count);
    }

    [Fact]
    public async Task ApprovedJob_PricingChangesAreLocked()
    {
        var context = await CreateNewJobAsync();

        var lineItemResponse = await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/quote/line-items",
            new
            {
                description = "Inspection",
                quantity = 1m,
                unitPrice = 3000m
            });

        var lineItem =
            await ReadObjectAsync(lineItemResponse);

        var lineItemId = GetId(lineItem);

        await PostActionAsync(
            context.TenantId,
            context.JobId,
            "quote");

        await PostActionAsync(
            context.TenantId,
            context.JobId,
            "approve");

        var addResponse = await _client.PostAsJsonAsync(
            $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/quote/line-items",
            new
            {
                description = "Late addition",
                quantity = 1m,
                unitPrice = 500m
            });

        Assert.Equal(
            HttpStatusCode.Conflict,
            addResponse.StatusCode);

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/quote/line-items/{lineItemId}",
            new
            {
                description = "Changed inspection",
                quantity = 2m,
                unitPrice = 3000m
            });

        Assert.Equal(
            HttpStatusCode.Conflict,
            updateResponse.StatusCode);

        var deleteResponse = await _client.DeleteAsync(
            $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/quote/line-items/{lineItemId}");

        Assert.Equal(
            HttpStatusCode.Conflict,
            deleteResponse.StatusCode);

        var quoteResponse = await _client.GetAsync(
            $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/quote");

        var quote =
            await ReadObjectAsync(quoteResponse);

        Assert.Equal(
            3000m,
            quote["subtotal"]!.GetValue<decimal>());

        Assert.Single(
            quote["lineItems"]!.AsArray());
    }

    [Fact]
    public async Task JobLineItem_InvalidValuesReturnBadRequest()
    {
        var context = await CreateNewJobAsync();

        var zeroQuantityResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/quote/line-items",
                new
                {
                    description = "Invalid quantity",
                    quantity = 0m,
                    unitPrice = 1000m
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            zeroQuantityResponse.StatusCode);

        var negativePriceResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/quote/line-items",
                new
                {
                    description = "Invalid price",
                    quantity = 1m,
                    unitPrice = -1m
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            negativePriceResponse.StatusCode);

        var blankDescriptionResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/quote/line-items",
                new
                {
                    description = "   ",
                    quantity = 1m,
                    unitPrice = 1000m
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            blankDescriptionResponse.StatusCode);
    }

    [Fact]
    public async Task JobLineItem_UnsupportedPrecisionReturnsBadRequest()
    {
        var context =
            await CreateNewJobAsync();

        var quantityResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/quote/line-items",
                new
                {
                    description =
                        "Too precise quantity",
                    quantity =
                        1.0001m,
                    unitPrice =
                        1000m
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            quantityResponse.StatusCode);

        var priceResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/quote/line-items",
                new
                {
                    description =
                        "Too precise price",
                    quantity =
                        1m,
                    unitPrice =
                        1000.001m
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            priceResponse.StatusCode);
    }

    [Fact]
    public async Task JobLineItem_UnsupportedMagnitudeReturnsBadRequest()
    {
        var context =
            await CreateNewJobAsync();

        var quantityResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/quote/line-items",
                new
                {
                    description =
                        "Quantity beyond database range",
                    quantity =
                        1000000000000000m,
                    unitPrice =
                        1m
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            quantityResponse.StatusCode);

        var priceResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{context.TenantId}/jobs/{context.JobId}/quote/line-items",
                new
                {
                    description =
                        "Price beyond database range",
                    quantity =
                        1m,
                    unitPrice =
                        10000000000000000m
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            priceResponse.StatusCode);
    }
    private async Task<(Guid TenantId, Guid JobId)>
        CreateNewJobAsync()
    {
        var uniqueId =
            Guid.NewGuid().ToString("N")[..10];

        var tenantResponse =
            await _client.PostAsJsonAsync(
                "/api/tenants",
                new
                {
                    name = $"Quote Test {uniqueId}",
                    slug = $"quote-{uniqueId}"
                });

        var tenant =
            await ReadObjectAsync(tenantResponse);

        var tenantId = GetId(tenant);

        var registerResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                new
                {
                    tenantId,
                    fullName = "Quote Test Owner",
                    email = $"quote-owner-{uniqueId}@example.com",
                    password = "QuoteTest#2026Secure"
                });

        var authentication =
            await ReadObjectAsync(registerResponse);

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
                    name = "Quote Test Customer",
                    phone = "+94 77 333 3333",
                    email = $"quote-customer-{uniqueId}@example.com",
                    city = "Colombo"
                });

        var customer =
            await ReadObjectAsync(customerResponse);

        var customerId = GetId(customer);

        var jobResponse =
            await _client.PostAsJsonAsync(
                $"/api/tenants/{tenantId}/jobs",
                new
                {
                    customerId,
                    title = "Quote pricing test job",
                    description = "Integration test quote.",
                    priority = "Normal"
                });

        var job =
            await ReadObjectAsync(jobResponse);

        return (
            tenantId,
            GetId(job));
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

        var json =
            await response.Content.ReadAsStringAsync();

        return JsonNode.Parse(json)!.AsObject();
    }

    private static Guid GetId(
        JsonObject value)
    {
        return Guid.Parse(
            value["id"]!.GetValue<string>());
    }
}