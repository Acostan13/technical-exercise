using StargateAPI.Business.Exceptions;
using StargateAPI.Business.Queries;
using StargateAPI.Tests.Infrastructure;

namespace StargateAPI.Tests.Queries;

public class QueryTests : IDisposable
{
    private readonly StargateTestHost _host = new();

    [Fact]
    public async Task GetPeople_ReturnsAstronautsAndNonAstronauts_OrderedByName()
    {
        await _host.CreatePersonAsync("Zed Astronaut");
        await _host.CreatePersonAsync("Amy Civilian");
        await _host.AddDutyAsync("Zed Astronaut", "1LT", "Commander", new DateTime(2020, 1, 1));

        var result = await _host.SendAsync(new GetPeople());

        Assert.Collection(result.People,
            civilian =>
            {
                Assert.Equal("Amy Civilian", civilian.Name);
                Assert.Null(civilian.CurrentDutyTitle);
                Assert.Null(civilian.CareerStartDate);
            },
            astronaut =>
            {
                Assert.Equal("Zed Astronaut", astronaut.Name);
                Assert.Equal("Commander", astronaut.CurrentDutyTitle);
                Assert.Equal(new DateTime(2020, 1, 1), astronaut.CareerStartDate);
            });
    }

    [Fact]
    public async Task GetPersonByName_IsCaseInsensitive()
    {
        var id = await _host.CreatePersonAsync("John Doe");

        var result = await _host.SendAsync(new GetPersonByName { Name = "john doe" });

        Assert.Equal(id, result.Person!.PersonId);
    }

    [Fact]
    public async Task GetPersonByName_WhenNotFound_IsRejectedAsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _host.SendAsync(new GetPersonByName { Name = "Nobody" }));
    }

    [Fact]
    public async Task Names_AreTreatedAsData_NotSql()
    {
        // Previously names were interpolated into SQL: an apostrophe broke the query and
        // a crafted name could match (or modify) other rows.
        await _host.CreatePersonAsync("Miles O'Brien");
        await _host.CreatePersonAsync("John Doe");

        var obrien = await _host.SendAsync(new GetPersonByName { Name = "Miles O'Brien" });
        Assert.Equal("Miles O'Brien", obrien.Person!.Name);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _host.SendAsync(new GetPersonByName { Name = "x' OR '1'='1" }));
    }

    [Fact]
    public async Task GetAstronautDutiesByName_ReturnsPersonAndDuties_MostRecentFirst()
    {
        await _host.CreatePersonAsync("John Doe");
        await _host.AddDutyAsync("John Doe", "1LT", "Commander", new DateTime(2020, 1, 1));
        await _host.AddDutyAsync("John Doe", "CPT", "Pilot", new DateTime(2022, 1, 1));

        var result = await _host.SendAsync(new GetAstronautDutiesByName { Name = "John Doe" });

        Assert.Equal("Pilot", result.Person!.CurrentDutyTitle);
        Assert.Collection(result.AstronautDuties,
            current => Assert.Equal("Pilot", current.DutyTitle),
            previous => Assert.Equal("Commander", previous.DutyTitle));
    }

    [Fact]
    public async Task GetAstronautDutiesByName_ForPersonWithNoDuties_ReturnsEmptyList()
    {
        await _host.CreatePersonAsync("Amy Civilian");

        var result = await _host.SendAsync(new GetAstronautDutiesByName { Name = "Amy Civilian" });

        Assert.Equal("Amy Civilian", result.Person!.Name);
        Assert.Empty(result.AstronautDuties);
    }

    [Fact]
    public async Task GetAstronautDutiesByName_WhenNotFound_IsRejectedAsNotFound()
    {
        // Previously this dereferenced a null person and failed with a NullReferenceException.
        await Assert.ThrowsAsync<NotFoundException>(
            () => _host.SendAsync(new GetAstronautDutiesByName { Name = "Nobody" }));
    }

    public void Dispose() => _host.Dispose();
}
