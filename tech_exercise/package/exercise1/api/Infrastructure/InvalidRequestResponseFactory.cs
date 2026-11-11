using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using StargateAPI.Business.Responses;

namespace StargateAPI.Infrastructure
{
    /// <summary>
    /// Requests whose body cannot be read are rejected by ASP.NET Core before reaching MediatR. This returns
    /// them in the same envelope as every other error, with messages that are actionable for a client and
    /// do not expose parser internals or .NET type names.
    /// </summary>
    public static class InvalidRequestResponseFactory
    {
        public const string InvalidBodyMessage = "The request body is missing or is not valid JSON.";

        public static IActionResult Create(ActionContext context)
        {
            var bodyParameters = context.ActionDescriptor.Parameters
                .Where(p => p.BindingInfo?.BindingSource == BindingSource.Body)
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var errors = context.ModelState
                .Where(entry => entry.Value is { Errors.Count: > 0 })
                .SelectMany(entry => Describe(entry.Key, bodyParameters))
                .Distinct()
                .ToList();

            // ASP.NET Core reports the body parameter alongside any field-level error; the specific one is enough.
            if (errors.Count > 1)
            {
                errors.Remove(InvalidBodyMessage);
            }

            return new BadRequestObjectResult(new ValidationErrorResponse
            {
                Success = false,
                Message = errors.Count == 1 ? errors[0] : "One or more validation errors occurred.",
                ResponseCode = StatusCodes.Status400BadRequest,
                Errors = errors
            });
        }

        private static IEnumerable<string> Describe(string key, ISet<string> bodyParameters)
        {
            // The body as a whole could not be read (empty, truncated or not JSON).
            if (string.IsNullOrEmpty(key) || key == "$" || bodyParameters.Contains(key))
            {
                return new[] { InvalidBodyMessage };
            }

            // A specific JSON property had a value of the wrong type, e.g. "$.dutyStartDate".
            if (key.StartsWith("$.", StringComparison.Ordinal))
            {
                return new[] { $"'{key[2..]}' has an invalid value." };
            }

            // Route or query values, e.g. "take".
            return new[] { $"'{key}' has an invalid value." };
        }
    }
}
