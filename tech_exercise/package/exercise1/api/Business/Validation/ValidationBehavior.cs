using FluentValidation;
using MediatR;
using StargateAPI.Business.Exceptions;

namespace StargateAPI.Business.Validation
{
    /// <summary>
    /// Validates every request's shape before any pre-processor or handler runs. Business-rule checks that
    /// need the database stay in the pre-processors, which can then rely on well-formed input.
    /// </summary>
    public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new RequestValidationException("A request body is required.");
            }

            var errors = new List<string>();

            foreach (var validator in _validators)
            {
                var result = await validator.ValidateAsync(request, cancellationToken);
                errors.AddRange(result.Errors.Select(e => e.ErrorMessage));
            }

            if (errors.Count > 0)
            {
                throw new RequestValidationException(errors);
            }

            return await next();
        }
    }
}
