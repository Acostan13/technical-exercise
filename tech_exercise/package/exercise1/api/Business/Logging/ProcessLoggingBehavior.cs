using MediatR;
using StargateAPI.Business.Data;
using StargateAPI.Business.Exceptions;
using System.Diagnostics;
using System.Text.Json;

namespace StargateAPI.Business.Logging
{
    /// <summary>
    /// Marks a request that should not be written to the process log (e.g. reading the log itself).
    /// </summary>
    public interface IExcludeFromProcessLog
    {
    }

    /// <summary>
    /// Outermost pipeline behavior: records the outcome of every request, including validation and business
    /// rule rejections, to the ProcessLog table and to the application logger.
    /// </summary>
    public sealed class ProcessLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly IProcessLogWriter _writer;
        private readonly ILogger<ProcessLoggingBehavior<TRequest, TResponse>> _logger;

        public ProcessLoggingBehavior(IProcessLogWriter writer, ILogger<ProcessLoggingBehavior<TRequest, TResponse>> logger)
        {
            _writer = writer;
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (request is IExcludeFromProcessLog)
            {
                return await next();
            }

            var requestName = typeof(TRequest).Name;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var response = await next();
                stopwatch.Stop();

                _logger.LogInformation("{RequestName} succeeded in {DurationMs} ms", requestName, stopwatch.ElapsedMilliseconds);

                await _writer.WriteAsync(CreateEntry(request, requestName, stopwatch, ProcessLogLevel.Information,
                    succeeded: true, $"{requestName} succeeded."));

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Expected business outcomes (not found, conflict, invalid input) are warnings; anything else is an error.
                if (ex is StargateException)
                {
                    _logger.LogWarning("{RequestName} was rejected: {Reason}", requestName, ex.Message);

                    await _writer.WriteAsync(CreateEntry(request, requestName, stopwatch, ProcessLogLevel.Warning,
                        succeeded: false, ex.Message, ex, includeStackTrace: false));
                }
                else
                {
                    _logger.LogError(ex, "{RequestName} failed with an unexpected error", requestName);

                    await _writer.WriteAsync(CreateEntry(request, requestName, stopwatch, ProcessLogLevel.Error,
                        succeeded: false, $"{requestName} failed with an unexpected error.", ex, includeStackTrace: true));
                }

                throw;
            }
        }

        private static ProcessLog CreateEntry(
            TRequest request,
            string requestName,
            Stopwatch stopwatch,
            ProcessLogLevel level,
            bool succeeded,
            string message,
            Exception? exception = null,
            bool includeStackTrace = false) => new()
        {
            TimestampUtc = DateTime.UtcNow,
            Level = level,
            RequestName = requestName,
            Succeeded = succeeded,
            Message = message,
            RequestData = Serialize(request),
            ExceptionType = exception?.GetType().Name,
            // Full detail (including inner exceptions and stack trace) stays server-side, never in API responses.
            ExceptionDetail = exception is null ? null : includeStackTrace ? exception.ToString() : exception.Message,
            DurationMs = stopwatch.ElapsedMilliseconds
        };

        private static string? Serialize(TRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request, SerializerOptions);
                return json.Length <= ProcessLog.RequestDataMaxLength ? json : json[..ProcessLog.RequestDataMaxLength];
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
