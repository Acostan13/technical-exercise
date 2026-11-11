namespace StargateAPI.Business.Exceptions
{
    /// <summary>
    /// Base type for expected business failures. These carry client-safe messages and are translated to
    /// HTTP responses at the API boundary, keeping the business layer free of HTTP concerns.
    /// </summary>
    public abstract class StargateException : Exception
    {
        protected StargateException(string message, Exception? innerException = null) : base(message, innerException)
        {
        }
    }

    /// <summary>The requested resource does not exist.</summary>
    public sealed class NotFoundException : StargateException
    {
        public NotFoundException(string message) : base(message)
        {
        }

        public static NotFoundException Person(string name) => new($"No person named '{name}' was found.");
    }

    /// <summary>The request conflicts with existing data (for example, a duplicate).</summary>
    public sealed class ConflictException : StargateException
    {
        public ConflictException(string message, Exception? innerException = null) : base(message, innerException)
        {
        }
    }

    /// <summary>The request is malformed or breaks a business rule.</summary>
    public sealed class RequestValidationException : StargateException
    {
        public RequestValidationException(IReadOnlyList<string> errors)
            : base(errors.Count == 1 ? errors[0] : "One or more validation errors occurred.")
        {
            Errors = errors;
        }

        public RequestValidationException(string error) : this(new[] { error })
        {
        }

        public IReadOnlyList<string> Errors { get; }
    }
}
