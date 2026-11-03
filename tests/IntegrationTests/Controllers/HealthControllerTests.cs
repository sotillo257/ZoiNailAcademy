using System.Net;
using FluentAssertions;

namespace IntegrationTests.Controllers;

public class HealthControllerTests : IntegrationTestBase
{
    public HealthControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Get_Health_ShouldReturnOk()
    {
        var response = await Client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
