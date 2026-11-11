using MediatR;
using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using StargateAPI.Business;
using StargateAPI.Business.Commands;

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
}
