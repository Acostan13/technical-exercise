using System.Net;
using StargateAPI.Tests.Infrastructure;

namespace StargateAPI.Tests.Api;

public class SwaggerTests : IDisposable
{
    private readonly StargateApiFactory _factory = new("Development");

    [Fact]
    public async Task OpenApiDocument_IsServedInDevelopment_WithEndpointDescriptions()
    {
        var response = await _factory.CreateClient().GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await response.Content.ReadAsStringAsync();
        Assert.Contains("/AstronautDuty/{name}", document);
        Assert.Contains("/ProcessLog", document);
        // XML doc comments are included, so the API is self-describing.
        Assert.Contains("Retrieves a person's current astronaut information and duty history", document);
    }

    [Fact]
    public async Task OpenApiDocument_IsNotServedOutsideDevelopment()
    {
        using var factory = new StargateApiFactory("Testing");

        var response = await factory.CreateClient().GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public void Dispose() => _factory.Dispose();
}
