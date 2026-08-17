using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using DispatchArc.IntegrationTests.Infrastructure;
using Xunit;

namespace DispatchArc.IntegrationTests.Documentation;

public sealed class ApiContractDocumentationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiContractDocumentationTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SwaggerContainsStableOperationIdsAndResponseCodes()
    {
        var response =
            await _client.GetAsync(
                "/swagger/v1/swagger.json");

        response.EnsureSuccessStatusCode();

        var json =
            await response.Content
                .ReadAsStringAsync();

        var document =
            JsonNode.Parse(json)!
                .AsObject();

        var register =
            GetOperation(
                document,
                "/api/auth/register",
                "post");

        Assert.Equal(
            "Auth_Register",
            register["operationId"]!
                .GetValue<string>());

        AssertResponses(
            register,
            "201",
            "400",
            "409");

        var createJob =
            GetOperation(
                document,
                "/api/tenants/{tenantId}/jobs",
                "post");

        Assert.Equal(
            "Jobs_Create",
            createJob["operationId"]!
                .GetValue<string>());

        AssertResponses(
            createJob,
            "201",
            "400",
            "401",
            "403",
            "404");

        var recordPayment =
            GetOperation(
                document,
                "/api/tenants/{tenantId}/invoices/{invoiceId}/payments",
                "post");

        Assert.Equal(
            "Payments_Record",
            recordPayment["operationId"]!
                .GetValue<string>());

        AssertResponses(
            recordPayment,
            "200",
            "400",
            "401",
            "403",
            "404",
            "409");
    }

    [Fact]
    public async Task AuthenticationValidationReturnsProblemDetails()
    {
        var response =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                new
                {
                    tenantId =
                        Guid.Empty,
                    fullName =
                        "API Contract Owner",
                    email =
                        "api-contract@example.com",
                    password =
                        "ApiContract#2026"
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var json =
            await response.Content
                .ReadAsStringAsync();

        var problem =
            JsonNode.Parse(json)!
                .AsObject();

        Assert.Equal(
            "Invalid registration request",
            problem["title"]!
                .GetValue<string>());

        Assert.Equal(
            400,
            problem["status"]!
                .GetValue<int>());

        Assert.Equal(
            "Tenant ID is required.",
            problem["detail"]!
                .GetValue<string>());
    }

    private static JsonObject GetOperation(
        JsonObject document,
        string path,
        string method)
    {
        return document["paths"]![
                path]![
                method]!
            .AsObject();
    }

    private static void AssertResponses(
        JsonObject operation,
        params string[] statusCodes)
    {
        var responses =
            operation["responses"]!
                .AsObject();

        foreach (var statusCode in statusCodes)
        {
            Assert.True(
                responses.ContainsKey(
                    statusCode),
                $"Expected response code {statusCode}.");
        }
    }
}