using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Data;
using StargateAPI.Business.Exceptions;
using StargateAPI.Business.Logging;
using StargateAPI.Business.Queries;
using StargateAPI.Tests.Infrastructure;

namespace StargateAPI.Tests.Logging;

public class ProcessLoggingTests : IDisposable
{
    private readonly StargateTestHost _host = new();

    [Fact]
    public async Task Success_IsLoggedToTheDatabase()
    {
        await _host.CreatePersonAsync("John Doe");

        var log = Assert.Single(await LoadLogsAsync());
        Assert.Equal(ProcessLogLevel.Information, log.Level);
        Assert.True(log.Succeeded);
        Assert.Equal(nameof(CreatePerson), log.RequestName);
        Assert.Equal("CreatePerson succeeded.", log.Message);
        Assert.Contains("John Doe", log.RequestData);
        Assert.Null(log.ExceptionType);
        Assert.True(log.DurationMs >= 0);
        Assert.Equal(DateTimeKind.Utc, log.TimestampUtc.Kind);
        Assert.True(log.TimestampUtc > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task ValidationFailure_IsLoggedAsAWarning()
    {
        await Assert.ThrowsAsync<RequestValidationException>(() => _host.CreatePersonAsync(" "));

        var log = Assert.Single(await LoadLogsAsync());
        Assert.Equal(ProcessLogLevel.Warning, log.Level);
        Assert.False(log.Succeeded);
        Assert.Equal("Name is required.", log.Message);
        Assert.Equal(nameof(RequestValidationException), log.ExceptionType);
    }

    [Fact]
    public async Task BusinessRuleRejection_IsLoggedAsAWarning()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _host.AddDutyAsync("Nobody", "1LT", "Commander", new DateTime(2020, 1, 1)));

        var log = Assert.Single(await LoadLogsAsync());
        Assert.Equal(ProcessLogLevel.Warning, log.Level);
        Assert.Equal(nameof(CreateAstronautDuty), log.RequestName);
        Assert.Equal("No person named 'Nobody' was found.", log.Message);
        Assert.Equal(nameof(NotFoundException), log.ExceptionType);
    }

    [Fact]
    public async Task UnexpectedException_IsLoggedAsAnError_WithFullDetail_AndRethrown()
    {
        var writer = new CapturingWriter();
        var behavior = new ProcessLoggingBehavior<CreatePerson, CreatePersonResult>(
            writer, NullLogger<ProcessLoggingBehavior<CreatePerson, CreatePersonResult>>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(
            new CreatePerson { Name = "John Doe" },
            () => throw new InvalidOperationException("Database unavailable"),
            CancellationToken.None));

        var log = Assert.Single(writer.Entries);
        Assert.Equal(ProcessLogLevel.Error, log.Level);
        Assert.False(log.Succeeded);
        Assert.Equal("CreatePerson failed with an unexpected error.", log.Message);
        Assert.Equal(nameof(InvalidOperationException), log.ExceptionType);
        Assert.Contains("Database unavailable", log.ExceptionDetail);
        Assert.Contains(" at ", log.ExceptionDetail); // stack trace retained server-side
    }

    [Fact]
    public async Task OversizedRequestData_IsTruncated()
    {
        var writer = new CapturingWriter();
        var behavior = new ProcessLoggingBehavior<CreatePerson, CreatePersonResult>(
            writer, NullLogger<ProcessLoggingBehavior<CreatePerson, CreatePersonResult>>.Instance);

        await behavior.Handle(
            new CreatePerson { Name = new string('x', ProcessLog.RequestDataMaxLength * 2) },
            () => Task.FromResult(new CreatePersonResult()),
            CancellationToken.None);

        Assert.Equal(ProcessLog.RequestDataMaxLength, Assert.Single(writer.Entries).RequestData!.Length);
    }

    [Fact]
    public async Task ReadingTheProcessLog_IsNotItselfLogged_AndReturnsNewestFirst()
    {
        await _host.CreatePersonAsync("First");
        await _host.CreatePersonAsync("Second");

        var result = await _host.SendAsync(new GetProcessLogs { Take = 10 });

        Assert.Equal(2, result.ProcessLogs.Count);
        Assert.Contains("Second", result.ProcessLogs[0].RequestData);
        Assert.Equal(2, (await LoadLogsAsync()).Count);
    }

    [Fact]
    public async Task GetProcessLogs_HonoursTake()
    {
        await _host.CreatePersonAsync("First");
        await _host.CreatePersonAsync("Second");

        var result = await _host.SendAsync(new GetProcessLogs { Take = 1 });

        Assert.Single(result.ProcessLogs);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(GetProcessLogs.MaxTake + 1)]
    public async Task GetProcessLogs_RejectsAnOutOfRangeTake(int take)
    {
        await Assert.ThrowsAsync<RequestValidationException>(() => _host.SendAsync(new GetProcessLogs { Take = take }));
    }

    [Fact]
    public async Task Writer_DoesNotPersist_TheFailedRequestsUnsavedChanges()
    {
        // Simulates a request that failed after modifying its DbContext: the log entry must be written
        // without also saving the request's pending (failed) changes.
        await using var requestScope = _host.Services.CreateAsyncScope();
        var requestContext = requestScope.ServiceProvider.GetRequiredService<StargateContext>();
        requestContext.People.Add(new Person { Name = "Should Not Be Saved" });

        var writer = _host.Services.GetRequiredService<IProcessLogWriter>();
        await writer.WriteAsync(new ProcessLog { RequestName = "Test", Message = "Failed", Level = ProcessLogLevel.Error });

        await using var context = _host.CreateContext();
        Assert.Empty(await context.People.ToListAsync());
        Assert.Single(await context.ProcessLogs.ToListAsync());
    }

    [Fact]
    public async Task Writer_SwallowsItsOwnFailures_SoLoggingCannotFailARequest()
    {
        // No StargateContext is registered, so the write itself fails.
        var services = new ServiceCollection().BuildServiceProvider();
        var writer = new ProcessLogWriter(
            services.GetRequiredService<IServiceScopeFactory>(), NullLogger<ProcessLogWriter>.Instance);

        var exception = await Record.ExceptionAsync(() => writer.WriteAsync(new ProcessLog { RequestName = "Test" }));

        Assert.Null(exception);
    }

    private async Task<List<ProcessLog>> LoadLogsAsync()
    {
        await using var context = _host.CreateContext();
        return await context.ProcessLogs.OrderBy(l => l.Id).ToListAsync();
    }

    private sealed class CapturingWriter : IProcessLogWriter
    {
        public List<ProcessLog> Entries { get; } = new();

        public Task WriteAsync(ProcessLog entry)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }
    }

    public void Dispose() => _host.Dispose();
}
