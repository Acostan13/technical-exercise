using MediatR;
using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using StargateAPI.Business;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Validation;

namespace StargateAPI.Tests.Infrastructure;

public class PipelineRegistrationTests
{
    private readonly IServiceCollection _services = new ServiceCollection().AddStargateBusiness();

    [Theory]
    [InlineData(typeof(IRequestPreProcessor<CreatePerson>), typeof(CreatePersonPreProcessor))]
    [InlineData(typeof(IRequestPreProcessor<UpdatePerson>), typeof(UpdatePersonPreProcessor))]
    [InlineData(typeof(IRequestPreProcessor<CreateAstronautDuty>), typeof(CreateAstronautDutyPreProcessor))]
    public void EveryPreProcessor_IsRegisteredExactlyOnce(Type serviceType, Type implementationType)
    {
        Assert.Single(_services, d => d.ServiceType == serviceType && d.ImplementationType == implementationType);
    }

    [Fact]
    public void PreProcessorBehavior_IsRegisteredExactlyOnce()
    {
        Assert.Single(_services, d =>
            d.ServiceType == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType == typeof(RequestPreProcessorBehavior<,>));
    }

    [Fact]
    public void Validation_RunsBefore_PreProcessors()
    {
        // MediatR executes behaviors in registration order, outermost first.
        var behaviors = _services
            .Where(d => d.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(d => d.ImplementationType)
            .ToList();

        Assert.True(
            behaviors.IndexOf(typeof(ValidationBehavior<,>)) < behaviors.IndexOf(typeof(RequestPreProcessorBehavior<,>)),
            "ValidationBehavior must be registered before RequestPreProcessorBehavior.");
    }
}
