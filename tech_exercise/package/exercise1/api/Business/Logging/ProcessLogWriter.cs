using StargateAPI.Business.Data;

namespace StargateAPI.Business.Logging
{
    public interface IProcessLogWriter
    {
        /// <summary>Persists a process log entry. Never throws: logging must not change a request's outcome.</summary>
        Task WriteAsync(ProcessLog entry);
    }

    /// <summary>
    /// Writes process logs through a dedicated <see cref="StargateContext"/> in its own DI scope. Reusing the
    /// request's context is unsafe: after a failure it may still track the request's unsaved changes, and
    /// saving the log entry would save those changes too.
    /// </summary>
    public sealed class ProcessLogWriter : IProcessLogWriter
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ProcessLogWriter> _logger;

        public ProcessLogWriter(IServiceScopeFactory scopeFactory, ILogger<ProcessLogWriter> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task WriteAsync(ProcessLog entry)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<StargateContext>();

                context.ProcessLogs.Add(entry);

                // Not tied to the request's cancellation token: a cancelled request should still be recorded.
                await context.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write process log entry for {RequestName}", entry.RequestName);
            }
        }
    }
}
