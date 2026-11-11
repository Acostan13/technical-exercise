using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Business.Exceptions;
using StargateAPI.Tests.Infrastructure;

namespace StargateAPI.Tests.Commands;

/// <summary>
/// Covers every README rule that governs Astronaut Duties and Astronaut Detail.
/// </summary>
public class CreateAstronautDutyTests : IDisposable
{
    private const string Name = "John Doe";
    private static readonly DateTime FirstStart = new(2020, 1, 15);
    private static readonly DateTime SecondStart = new(2022, 6, 1);

    private readonly StargateTestHost _host = new();

    public CreateAstronautDutyTests()
    {
        _host.CreatePersonAsync(Name).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task FirstDuty_CreatesCurrentDutyAndAstronautDetail()
    {
        var dutyId = await _host.AddDutyAsync(Name, "1LT", "Commander", FirstStart.AddHours(13));

        var (detail, duties) = await LoadAsync();

        var duty = Assert.Single(duties);
        Assert.Equal(dutyId, duty.Id);
        Assert.Equal(FirstStart, duty.DutyStartDate); // time component is discarded
        Assert.Null(duty.DutyEndDate); // Rule: the Current Duty has no Duty End Date

        Assert.NotNull(detail);
        Assert.Equal("1LT", detail.CurrentRank);
        Assert.Equal("Commander", detail.CurrentDutyTitle);
        Assert.Equal(FirstStart, detail.CareerStartDate);
        Assert.Null(detail.CareerEndDate);
    }

    [Fact]
    public async Task NewDuty_EndsPreviousDutyTheDayBefore_AndBecomesTheOnlyCurrentDuty()
    {
        await _host.AddDutyAsync(Name, "1LT", "Commander", FirstStart);
        await _host.AddDutyAsync(Name, "CPT", "Pilot", SecondStart);

        var (detail, duties) = await LoadAsync();

        var previous = duties.Single(d => d.DutyTitle == "Commander");
        var current = duties.Single(d => d.DutyTitle == "Pilot");

        // Rule: the Previous Duty End Date is the day before the New Duty Start Date.
        Assert.Equal(SecondStart.AddDays(-1), previous.DutyEndDate);
        // Rule: only one current duty, which has no end date.
        Assert.Null(current.DutyEndDate);
        Assert.Single(duties, d => d.DutyEndDate is null);

        // Rule: Astronaut Detail holds the one current Duty Title and Rank; the career start is unchanged.
        Assert.Equal("CPT", detail!.CurrentRank);
        Assert.Equal("Pilot", detail.CurrentDutyTitle);
        Assert.Equal(FirstStart, detail.CareerStartDate);
    }

    [Theory]
    [InlineData("RETIRED")]
    [InlineData("retired")]
    [InlineData(" Retired ")]
    public async Task RetiredDuty_SetsCareerEndDateToTheDayBefore(string retiredTitle)
    {
        await _host.AddDutyAsync(Name, "1LT", "Commander", FirstStart);
        await _host.AddDutyAsync(Name, "CPT", retiredTitle, SecondStart);

        var (detail, duties) = await LoadAsync();

        // Rules: 'RETIRED' classifies the Person as retired; Career End Date is the day before it starts.
        Assert.Equal("RETIRED", detail!.CurrentDutyTitle);
        Assert.Equal(SecondStart.AddDays(-1), detail.CareerEndDate);
        Assert.Equal(SecondStart.AddDays(-1), duties.Single(d => d.DutyTitle == "Commander").DutyEndDate);
    }

    [Fact]
    public async Task RetiredAsFirstDuty_StillSetsCareerEndDateToTheDayBefore()
    {
        // Previously this path set the Career End Date to the Retired start date itself.
        await _host.AddDutyAsync(Name, "CPT", "RETIRED", SecondStart);

        var (detail, _) = await LoadAsync();

        Assert.Equal(SecondStart, detail!.CareerStartDate);
        Assert.Equal(SecondStart.AddDays(-1), detail.CareerEndDate);
    }

    [Fact]
    public async Task DutyAfterRetirement_ClearsCareerEndDate()
    {
        await _host.AddDutyAsync(Name, "1LT", "Commander", FirstStart);
        await _host.AddDutyAsync(Name, "1LT", "RETIRED", SecondStart);
        await _host.AddDutyAsync(Name, "MAJ", "Flight Director", SecondStart.AddYears(1));

        var (detail, _) = await LoadAsync();

        Assert.Equal("Flight Director", detail!.CurrentDutyTitle);
        Assert.Null(detail.CareerEndDate);
    }

    [Fact]
    public async Task Duty_ForUnknownPerson_IsRejectedAsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _host.AddDutyAsync("Nobody", "1LT", "Commander", FirstStart));
    }

    [Fact]
    public async Task DuplicateDuty_IsRejectedAsConflict()
    {
        await _host.AddDutyAsync(Name, "1LT", "Commander", FirstStart);

        await Assert.ThrowsAsync<ConflictException>(
            () => _host.AddDutyAsync(Name, "1LT", "Commander", FirstStart));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    public async Task Duty_StartingOnOrBeforeTheCurrentDuty_IsRejected(int daysFromCurrentStart)
    {
        await _host.AddDutyAsync(Name, "1LT", "Commander", SecondStart);

        await Assert.ThrowsAsync<RequestValidationException>(
            () => _host.AddDutyAsync(Name, "CPT", "Pilot", SecondStart.AddDays(daysFromCurrentStart)));

        var (_, duties) = await LoadAsync();
        Assert.Null(Assert.Single(duties).DutyEndDate);
    }

    [Fact]
    public async Task SameTitleAndStartDate_ForDifferentPeople_IsAllowed()
    {
        // Previously the duplicate check ignored the Person, blocking this legitimate case.
        await _host.CreatePersonAsync("Jane Doe");
        await _host.AddDutyAsync(Name, "1LT", "Commander", FirstStart);

        var id = await _host.AddDutyAsync("Jane Doe", "1LT", "Commander", FirstStart);

        Assert.NotNull(id);
    }

    [Fact]
    public async Task CurrentDutyRule_IsAlsoEnforcedByTheDatabase()
    {
        await _host.AddDutyAsync(Name, "1LT", "Commander", FirstStart);
        await using var context = _host.CreateContext();
        var personId = (await context.People.SingleAsync(p => p.Name == Name)).Id;

        context.AstronautDuties.Add(new AstronautDuty
        {
            PersonId = personId, Rank = "CPT", DutyTitle = "Pilot", DutyStartDate = SecondStart
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private async Task<(AstronautDetail? Detail, List<AstronautDuty> Duties)> LoadAsync()
    {
        await using var context = _host.CreateContext();
        var person = await context.People
            .Include(p => p.AstronautDetail)
            .Include(p => p.AstronautDuties)
            .SingleAsync(p => p.Name == Name);

        return (person.AstronautDetail, person.AstronautDuties.ToList());
    }

    public void Dispose() => _host.Dispose();
}
