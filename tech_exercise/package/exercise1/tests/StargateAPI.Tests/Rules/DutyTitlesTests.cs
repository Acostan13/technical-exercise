using StargateAPI.Business.Rules;

namespace StargateAPI.Tests.Rules;

public class DutyTitlesTests
{
    [Theory]
    [InlineData("RETIRED", true)]
    [InlineData("retired", true)]
    [InlineData("  Retired  ", true)]
    [InlineData("RETIRED COMMANDER", false)]
    [InlineData("Commander", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsRetired_MatchesOnlyTheRetiredTitle(string? dutyTitle, bool expected)
    {
        Assert.Equal(expected, DutyTitles.IsRetired(dutyTitle));
    }
}
