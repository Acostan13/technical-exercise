using Microsoft.AspNetCore.Mvc;
using StargateAPI.Business.Queries;
using StargateAPI.Controllers;
using StargateAPI.Tests.Infrastructure;

namespace StargateAPI.Tests.Controllers;

public class AstronautDutyControllerTests : IDisposable
{
    private readonly StargateTestHost _host = new();

    [Fact]
    public async Task GetAstronautDutiesByName_ReturnsTheDutyHistory()
    {
        // Previously this endpoint sent GetPersonByName, so duties were never returned.
        await _host.CreatePersonAsync("John Doe");
        await _host.AddDutyAsync("John Doe", "1LT", "Commander", new DateTime(2020, 1, 1));

        var mediator = _host.CreateScopedMediator(out var scope);
        using (scope)
        {
            var response = await new AstronautDutyController(mediator)
                .GetAstronautDutiesByName("John Doe", CancellationToken.None);

            var objectResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(200, objectResult.StatusCode);
            var result = Assert.IsType<GetAstronautDutiesByNameResult>(objectResult.Value);
            Assert.Single(result.AstronautDuties);
        }
    }

    [Fact]
    public async Task GetAstronautDutiesByName_ForUnknownPerson_Returns404WithMessage()
    {
        var mediator = _host.CreateScopedMediator(out var scope);
        using (scope)
        {
            var response = await new AstronautDutyController(mediator)
                .GetAstronautDutiesByName("Nobody", CancellationToken.None);

            var objectResult = Assert.IsType<ObjectResult>(response);
            Assert.Equal(404, objectResult.StatusCode);
            var body = Assert.IsType<BaseResponse>(objectResult.Value);
            Assert.False(body.Success);
            Assert.Contains("Nobody", body.Message);
        }
    }

    public void Dispose() => _host.Dispose();
}
