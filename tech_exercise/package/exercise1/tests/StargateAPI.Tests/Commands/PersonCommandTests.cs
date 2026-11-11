using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Commands;
using StargateAPI.Tests.Infrastructure;

namespace StargateAPI.Tests.Commands;

public class PersonCommandTests : IDisposable
{
    private readonly StargateTestHost _host = new();

    [Fact]
    public async Task CreatePerson_PersistsPerson_WithoutAstronautRecords()
    {
        var id = await _host.CreatePersonAsync("John Doe");

        await using var context = _host.CreateContext();
        var person = await context.People
            .Include(p => p.AstronautDetail)
            .Include(p => p.AstronautDuties)
            .SingleAsync(p => p.Id == id);

        Assert.Equal("John Doe", person.Name);
        // Rule: a Person who has not had an astronaut assignment will not have Astronaut records.
        Assert.Null(person.AstronautDetail);
        Assert.Empty(person.AstronautDuties);
    }

    [Theory]
    [InlineData("John Doe")]
    [InlineData("JOHN DOE")]
    public async Task CreatePerson_WithExistingName_IsRejectedAsConflict(string duplicateName)
    {
        await _host.CreatePersonAsync("John Doe");

        // Before the fix, CreatePersonPreProcessor was never registered, so this check never ran.
        var ex = await Assert.ThrowsAsync<BadHttpRequestException>(
            () => _host.CreatePersonAsync(duplicateName));

        Assert.Equal(StatusCodes.Status409Conflict, ex.StatusCode);
        await using var context = _host.CreateContext();
        Assert.Equal(1, await context.People.CountAsync());
    }

    [Fact]
    public async Task UpdatePerson_RenamesPerson()
    {
        var id = await _host.CreatePersonAsync("John Doe");

        var result = await _host.SendAsync(new UpdatePerson { CurrentName = "John Doe", Name = "John Smith" });

        Assert.Equal(id, result.Id);
        await using var context = _host.CreateContext();
        Assert.Equal("John Smith", (await context.People.SingleAsync(p => p.Id == id)).Name);
    }

    [Fact]
    public async Task UpdatePerson_CanChangeCapitalisationOfOwnName()
    {
        var id = await _host.CreatePersonAsync("john doe");

        await _host.SendAsync(new UpdatePerson { CurrentName = "john doe", Name = "John Doe" });

        await using var context = _host.CreateContext();
        Assert.Equal("John Doe", (await context.People.SingleAsync(p => p.Id == id)).Name);
    }

    [Fact]
    public async Task UpdatePerson_WhenPersonDoesNotExist_IsRejectedAsNotFound()
    {
        var ex = await Assert.ThrowsAsync<BadHttpRequestException>(
            () => _host.SendAsync(new UpdatePerson { CurrentName = "Nobody", Name = "Somebody" }));

        Assert.Equal(StatusCodes.Status404NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task UpdatePerson_ToAnotherPersonsName_IsRejectedAsConflict()
    {
        await _host.CreatePersonAsync("John Doe");
        await _host.CreatePersonAsync("Jane Doe");

        var ex = await Assert.ThrowsAsync<BadHttpRequestException>(
            () => _host.SendAsync(new UpdatePerson { CurrentName = "Jane Doe", Name = "John Doe" }));

        Assert.Equal(StatusCodes.Status409Conflict, ex.StatusCode);
    }

    public void Dispose() => _host.Dispose();
}
