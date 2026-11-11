using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StargateAPI.Business.Data;
using StargateAPI.Tests.Infrastructure;

namespace StargateAPI.Tests.Api;

/// <summary>
/// Pre-processor checks alone cannot prevent duplicates under concurrency: two requests can both pass the
/// "does this name exist?" check before either saves. The database constraints are the final guard, and
/// the exception handler turns their violation into a 409 rather than a 500.
/// </summary>
public class ConcurrencyTests : IDisposable
{
    private const int ConcurrentRequests = 25;

    private readonly StargateApiFactory _factory = StargateApiFactory.WithFileDatabase();

    [Fact]
    public async Task ConcurrentCreatesOfTheSamePerson_CreateExactlyOne_AndRejectTheRestAsConflicts()
    {
        var client = _factory.CreateClient();

        var responses = await Task.WhenAll(Enumerable.Range(0, ConcurrentRequests)
            .Select(_ => client.PostAsJsonAsync("/Person", new { name = "John Doe" })));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.OK),
            r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));

        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<StargateContext>();
        Assert.Equal(1, await context.People.CountAsync());

        // A lost race is an expected conflict: logged as a warning, never as an unexpected error.
        Assert.DoesNotContain(await context.ProcessLogs.ToListAsync(), l => l.Level == ProcessLogLevel.Error);
    }

    [Fact]
    public async Task ConcurrentDutiesForThePerson_LeaveExactlyOneCurrentDuty()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/Person", new { name = "John Doe" });

        var responses = await Task.WhenAll(Enumerable.Range(0, ConcurrentRequests)
            .Select(i => client.PostAsJsonAsync("/AstronautDuty", new
            {
                name = "John Doe",
                rank = "1LT",
                dutyTitle = $"Duty {i}",
                dutyStartDate = new DateTime(2020, 1, 1).AddDays(i)
            })));

        // Requests may legitimately be rejected (e.g. one started before another that already saved),
        // but none may fail with a server error, and the rules must hold afterwards.
        Assert.DoesNotContain(responses, r => (int)r.StatusCode >= 500);
        Assert.Contains(responses, r => r.StatusCode == HttpStatusCode.OK);

        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<StargateContext>();
        var duties = await context.AstronautDuties.OrderBy(d => d.DutyStartDate).ToListAsync();
        Assert.Single(duties, d => d.DutyEndDate is null);
        Assert.Null(duties[^1].DutyEndDate);
        Assert.DoesNotContain(await context.ProcessLogs.ToListAsync(), l => l.Level == ProcessLogLevel.Error);

        for (var i = 1; i < duties.Count; i++)
        {
            Assert.Equal(duties[i].DutyStartDate.AddDays(-1), duties[i - 1].DutyEndDate);
        }
    }

    public void Dispose() => _factory.Dispose();
}
