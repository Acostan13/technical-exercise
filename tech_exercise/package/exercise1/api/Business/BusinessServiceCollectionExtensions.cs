using FluentValidation;
using MediatR.Pipeline;
using StargateAPI.Business.Validation;

namespace StargateAPI.Business
{
    public static class BusinessServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the MediatR pipeline: validators, handlers and request pre-processors. Shared by the
        /// application and the test suite so both run the exact same pipeline.
        /// </summary>
        public static IServiceCollection AddStargateBusiness(this IServiceCollection services)
        {
            var assembly = typeof(BusinessServiceCollectionExtensions).Assembly;

            services.AddValidatorsFromAssembly(assembly);

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(assembly);

                // Behaviors run in registration order:
                //   1. ValidationBehavior          - rejects malformed input before anything touches the database
                //   2. RequestPreProcessorBehavior - business-rule checks against existing data
                //   3. the request handler
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));

                // MediatR does not register IRequestPreProcessor implementations by default. Previously only
                // CreateAstronautDutyPreProcessor was registered by hand, so CreatePersonPreProcessor never ran.
                // Auto-registration discovers every pre-processor; the behavior below is what executes them
                // (MediatR 12.2 only adds it implicitly when a pre-processor is registered by hand).
                cfg.AutoRegisterRequestProcessors = true;
                cfg.AddOpenBehavior(typeof(RequestPreProcessorBehavior<,>));
            });

            return services;
        }
    }
}
