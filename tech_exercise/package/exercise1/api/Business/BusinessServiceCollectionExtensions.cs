using MediatR.Pipeline;

namespace StargateAPI.Business
{
    public static class BusinessServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the MediatR handlers and request pre-processors. Shared by the application and the
        /// test suite so both run the exact same pipeline.
        /// </summary>
        public static IServiceCollection AddStargateBusiness(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(BusinessServiceCollectionExtensions).Assembly);

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
