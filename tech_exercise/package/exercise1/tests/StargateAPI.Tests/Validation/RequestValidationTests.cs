using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Exceptions;
using StargateAPI.Business.Queries;
using StargateAPI.Business.Validation;
using StargateAPI.Tests.Infrastructure;

namespace StargateAPI.Tests.Validation;

/// <summary>
/// Input validation runs in the MediatR pipeline before any pre-processor or handler touches the database.
/// </summary>
public class RequestValidationTests : IDisposable
{
    private static readonly string TooLongName = new('x', ValidationRules.PersonNameMaxLength + 1);

    private readonly StargateTestHost _host = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreatePerson_WithoutAName_IsRejected(string? name)
    {
        var ex = await Assert.ThrowsAsync<RequestValidationException>(
            () => _host.SendAsync(new CreatePerson { Name = name! }));

        Assert.Equal("Name is required.", Assert.Single(ex.Errors));
    }

    [Fact]
    public async Task CreatePerson_WithAnOverlongName_IsRejected()
    {
        var ex = await Assert.ThrowsAsync<RequestValidationException>(
            () => _host.SendAsync(new CreatePerson { Name = TooLongName }));

        Assert.Contains("100 characters or fewer", Assert.Single(ex.Errors));
    }

    [Fact]
    public async Task Names_AreTrimmed_SoSurroundingWhitespaceCannotCreateADuplicate()
    {
        var id = await _host.CreatePersonAsync("  John Doe  ");

        await Assert.ThrowsAsync<ConflictException>(() => _host.CreatePersonAsync("John Doe"));

        await using var context = _host.CreateContext();
        Assert.Equal("John Doe", (await context.People.SingleAsync(p => p.Id == id)).Name);
    }

    [Fact]
    public async Task CreateAstronautDuty_ReportsEveryInvalidField()
    {
        var ex = await Assert.ThrowsAsync<RequestValidationException>(() => _host.SendAsync(new CreateAstronautDuty
        {
            Name = " ",
            Rank = "",
            DutyTitle = new string('x', ValidationRules.DutyTitleMaxLength + 1)
            // DutyStartDate omitted
        }));

        Assert.Equal(4, ex.Errors.Count);
        Assert.Contains("Name is required.", ex.Errors);
        Assert.Contains("Rank is required.", ex.Errors);
        Assert.Contains("Duty Title must be 100 characters or fewer.", ex.Errors);
        Assert.Contains("Duty Start Date is required.", ex.Errors);
    }

    [Fact]
    public async Task CreateAstronautDuty_ValidationRunsBeforeTheDatabaseIsQueried()
    {
        // "Nobody" does not exist, but the missing date is reported first: no lookup is attempted.
        await Assert.ThrowsAsync<RequestValidationException>(() => _host.SendAsync(new CreateAstronautDuty
        {
            Name = "Nobody",
            Rank = "1LT",
            DutyTitle = "Commander"
        }));
    }

    [Fact]
    public async Task UpdatePerson_ToABlankName_IsRejected()
    {
        await _host.CreatePersonAsync("John Doe");

        var ex = await Assert.ThrowsAsync<RequestValidationException>(
            () => _host.SendAsync(new UpdatePerson { CurrentName = "John Doe", Name = "  " }));

        Assert.Equal("Name is required.", Assert.Single(ex.Errors));
    }

    [Fact]
    public async Task Queries_WithABlankName_AreRejected()
    {
        await Assert.ThrowsAsync<RequestValidationException>(
            () => _host.SendAsync(new GetPersonByName { Name = " " }));

        await Assert.ThrowsAsync<RequestValidationException>(
            () => _host.SendAsync(new GetAstronautDutiesByName { Name = " " }));
    }

    public void Dispose() => _host.Dispose();
}
