using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Exceptions;
using StargateAPI.Business.Responses;

namespace StargateAPI.Infrastructure
{
    /// <summary>
    /// The single place where exceptions become HTTP responses. Expected business failures map to 4xx with
    /// their client-safe message; anything else becomes a generic 500 so internal details are never leaked.
    /// </summary>
    public sealed class StargateExceptionHandler : IExceptionHandler
    {
        private const int SqliteConstraintErrorCode = 19;

        private readonly ILogger<StargateExceptionHandler> _logger;

        public StargateExceptionHandler(ILogger<StargateExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var response = ToResponse(exception);

            if (response.ResponseCode >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
                    httpContext.Request.Method, httpContext.Request.Path);
            }
            else
            {
                _logger.LogWarning("Request {Method} {Path} rejected with {StatusCode}: {Message}",
                    httpContext.Request.Method, httpContext.Request.Path, response.ResponseCode, response.Message);
            }

            httpContext.Response.StatusCode = response.ResponseCode;
            await httpContext.Response.WriteAsJsonAsync(response, response.GetType(), cancellationToken);
            return true;
        }

        public static BaseResponse ToResponse(Exception exception) => exception switch
        {
            RequestValidationException validation => new ValidationErrorResponse
            {
                Success = false,
                Message = validation.Message,
                ResponseCode = StatusCodes.Status400BadRequest,
                Errors = validation.Errors
            },
            NotFoundException notFound => Failure(StatusCodes.Status404NotFound, notFound.Message),
            ConflictException conflict => Failure(StatusCodes.Status409Conflict, conflict.Message),

            // A concurrent request can pass the pre-processor checks and then hit a database constraint
            // (e.g. two requests creating the same Person at once). That is a conflict, not a server fault.
            DbUpdateException { InnerException: SqliteException { SqliteErrorCode: SqliteConstraintErrorCode } } =>
                Failure(StatusCodes.Status409Conflict, "The request conflicts with existing data. Refresh and try again."),

            OperationCanceledException => Failure(499, "The request was cancelled."),

            _ => Failure(StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        private static BaseResponse Failure(int statusCode, string message) => new()
        {
            Success = false,
            Message = message,
            ResponseCode = statusCode
        };
    }
}
