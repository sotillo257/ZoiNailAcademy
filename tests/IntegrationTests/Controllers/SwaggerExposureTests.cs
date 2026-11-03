using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;

using IntegrationTests.TestSupport;

namespace IntegrationTests.Controllers;

public class SwaggerExposureTests : IntegrationTestBase
{
    public SwaggerExposureTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Swagger_WhenProductionAndFlagUnset_ReturnsUnauthorized()
    {
        using var client = Factory
            .WithWebHostBuilder(HostSettings.UseProduction)
            .CreateClient();

        var openApiResponse = await client.GetAsync("/openapi/v1.json");
        var swaggerResponse = await client.GetAsync("/swagger/index.html");

        openApiResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        swaggerResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Swagger_WhenProductionAndFlagEnabled_ReturnsOkWithoutAuth()
    {
        using var client = Factory
            .WithWebHostBuilder(builder =>
            {
                HostSettings.UseProduction(builder);
                builder.UseSetting("Swagger:EnableInProduction", "true");
            })
            .CreateClient();

        var openApiResponse = await client.GetAsync("/openapi/v1.json");
        var swaggerResponse = await client.GetAsync("/swagger/index.html");

        openApiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        swaggerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OpenApiDocument_IncludesBearerSecurityScheme()
    {
        using var client = Factory
            .WithWebHostBuilder(builder =>
            {
                HostSettings.UseProduction(builder);
                builder.UseSetting("Swagger:EnableInProduction", "true");
            })
            .CreateClient();

        var document = (await client.GetFromJsonAsync<JsonNode>("/openapi/v1.json"))!;

        var bearerScheme = document["components"]?["securitySchemes"]?["Bearer"];
        bearerScheme.Should().NotBeNull("the OpenAPI document should declare a Bearer security scheme");
        bearerScheme!["type"]!.GetValue<string>().Should().Be("http");
        bearerScheme["scheme"]!.GetValue<string>().Should().Be("bearer");

        var operations = document["paths"]!.AsObject()
            .SelectMany(path => path.Value!.AsObject())
            .Select(operation => operation.Value);
        var anyOperationRequiresBearer = operations.Any(operation =>
        {
            var security = operation!["security"];
            return security is not null && security.AsArray().Any(requirement => requirement!.AsObject().ContainsKey("Bearer"));
        });
        anyOperationRequiresBearer.Should().BeTrue("at least one operation should require the Bearer security scheme");
    }
}
