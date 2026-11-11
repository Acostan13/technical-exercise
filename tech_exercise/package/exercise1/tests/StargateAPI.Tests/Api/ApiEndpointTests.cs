using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using StargateAPI.Tests.Infrastructure;

namespace StargateAPI.Tests.Api;

/// <summary>
/// End-to-end tests over HTTP for every required endpoint, including the error contract.
/// </summary>
public class ApiEndpointTests : IDisposable
{
    private readonly StargateApiFactory _factory = new();
    private readonly HttpClient _client;

    public ApiEndpointTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task RequiredEndpoints_SupportTheFullAstronautLifecycle()
    {
        // Add a person
        var created = await _client.PostAsJsonAsync("/Person", new { name = "John Doe" });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);

        // Update a person by name
        var updated = await _client.PutAsJsonAsync("/Person/John Doe", new { name = "John A. Doe" });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        // Add astronaut duties
        var first = await _client.PostAsJsonAsync("/AstronautDuty",
            new { name = "John A. Doe", rank = "1LT", dutyTitle = "Commander", dutyStartDate = "2020-01-15" });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var retired = await _client.PostAsJsonAsync("/AstronautDuty",
            new { name = "John A. Doe", rank = "CPT", dutyTitle = "RETIRED", dutyStartDate = "2023-03-01" });
        Assert.Equal(HttpStatusCode.OK, retired.StatusCode);

        // Retrieve a person by name
        var person = await GetJsonAsync("/Person/john a. doe");
        Assert.Equal("RETIRED", person.GetProperty("person").GetProperty("currentDutyTitle").GetString());
        Assert.Equal("2023-02-28T00:00:00", person.GetProperty("person").GetProperty("careerEndDate").GetString());

        // Retrieve all people
        var people = await GetJsonAsync("/Person");
        Assert.Equal(1, people.GetProperty("people").GetArrayLength());

        // Retrieve astronaut duties by name
        var duties = await GetJsonAsync("/AstronautDuty/John A. Doe");
        var history = duties.GetProperty("astronautDuties");
        Assert.Equal(2, history.GetArrayLength());
        Assert.Equal("RETIRED", history[0].GetProperty("dutyTitle").GetString());
        Assert.Equal(JsonValueKind.Null, history[0].GetProperty("dutyEndDate").ValueKind);
        Assert.Equal("2023-02-28T00:00:00", history[1].GetProperty("dutyEndDate").GetString());
    }

    [Fact]
    public async Task UnknownPerson_Returns404_InTheStandardEnvelope()
    {
        var response = await _client.GetAsync("/AstronautDuty/Nobody");

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "No person named 'Nobody' was found.");
    }

    [Fact]
    public async Task DuplicatePerson_Returns409_InTheStandardEnvelope()
    {
        await _client.PostAsJsonAsync("/Person", new { name = "John Doe" });

        var response = await _client.PostAsJsonAsync("/Person", new { name = "JOHN DOE" });

        await AssertErrorAsync(response, HttpStatusCode.Conflict, "A person named 'JOHN DOE' already exists.");
    }

    [Fact]
    public async Task InvalidDuty_Returns400_ListingEveryError()
    {
        var response = await _client.PostAsJsonAsync("/AstronautDuty",
            new { name = "John Doe", rank = "", dutyTitle = "" });

        var body = await AssertErrorAsync(response, HttpStatusCode.BadRequest, "One or more validation errors occurred.");
        Assert.Equal(3, body.GetProperty("errors").GetArrayLength());
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    public async Task UnreadableBody_Returns400_InTheStandardEnvelope(string body)
    {
        var response = await _client.PostAsync("/AstronautDuty",
            new StringContent(body, Encoding.UTF8, "application/json"));

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "The request body is missing or is not valid JSON.");
    }

    [Fact]
    public async Task InvalidFieldValue_Returns400_NamingTheField_WithoutTypeNames()
    {
        var response = await _client.PostAsJsonAsync("/AstronautDuty",
            new { name = "John Doe", rank = "1LT", dutyTitle = "Pilot", dutyStartDate = "not-a-date" });

        var body = await AssertErrorAsync(response, HttpStatusCode.BadRequest, "'dutyStartDate' has an invalid value.");
        Assert.DoesNotContain("System.", body.GetRawText());
    }

    [Fact]
    public async Task MissingProperty_Returns400_FromValidation()
    {
        var response = await _client.PostAsJsonAsync("/Person", new { });

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "Name is required.");
    }

    private async Task<JsonElement> GetJsonAsync(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<JsonElement> AssertErrorAsync(
        HttpResponseMessage response, HttpStatusCode expectedStatus, string? expectedMessage = null)
    {
        Assert.Equal(expectedStatus, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.GetProperty("success").GetBoolean());
        Assert.Equal((int)expectedStatus, body.GetProperty("responseCode").GetInt32());

        if (expectedMessage is not null)
        {
            Assert.Equal(expectedMessage, body.GetProperty("message").GetString());
        }

        return body;
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
