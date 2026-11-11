using StargateAPI.Business.Commands;

namespace StargateAPI.Tests.Infrastructure;

public static class TestData
{
    public static async Task<int> CreatePersonAsync(this StargateTestHost host, string name) =>
        (await host.SendAsync(new CreatePerson { Name = name })).Id;

    public static async Task<int?> AddDutyAsync(
        this StargateTestHost host, string name, string rank, string dutyTitle, DateTime dutyStartDate) =>
        (await host.SendAsync(new CreateAstronautDuty
        {
            Name = name,
            Rank = rank,
            DutyTitle = dutyTitle,
            DutyStartDate = dutyStartDate
        })).Id;
}
