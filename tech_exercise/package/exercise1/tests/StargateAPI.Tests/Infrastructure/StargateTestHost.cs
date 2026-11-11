using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StargateAPI.Business;
using StargateAPI.Business.Data;

namespace StargateAPI.Tests.Infrastructure;

/// <summary>
/// Hosts the real MediatR pipeline (handlers and pre-processors, registered exactly as the API does)
/// over an isolated SQLite database. Each request runs in its own DI scope, mirroring an HTTP request.
/// </summary>
public sealed class StargateTestHost : IDisposable
{
    private readonly SqliteTestDatabase _database = new();
    private readonly ServiceProvider _services;

    public StargateTestHost()
    {
        var services = new ServiceCollection();
        services.AddDbContext<StargateContext>(options => options.UseSqlite(_database.Connection));
        services.AddStargateBusiness();

        _services = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
    }

    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        await using var scope = _services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(request);
    }

    /// <summary>Creates a context outside the pipeline, for arranging and asserting on data.</summary>
    public StargateContext CreateContext() => _database.CreateContext();

    public IMediator CreateScopedMediator(out IServiceScope scope)
    {
        scope = _services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IMediator>();
    }

    public void Dispose()
    {
        _services.Dispose();
        _database.Dispose();
    }
}
