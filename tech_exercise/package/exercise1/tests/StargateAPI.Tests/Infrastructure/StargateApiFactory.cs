using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StargateAPI.Business.Data;

namespace StargateAPI.Tests.Infrastructure;

/// <summary>
/// Hosts the real application (routing, model binding, exception handling, MediatR, EF Core) in memory.
/// By default the database is an isolated in-memory SQLite instance on one shared connection. Tests that
/// issue concurrent requests must use <see cref="WithFileDatabase"/>, because a single SqliteConnection
/// is not thread-safe, whereas a file lets each request open its own connection, exactly as in production.
/// </summary>
public sealed class StargateApiFactory : WebApplicationFactory<Program>
{
    private readonly string _environment;
    private readonly string? _databasePath;
    private readonly SqliteConnection? _connection;

    public StargateApiFactory(string environment = "Testing")
        : this(environment, databasePath: null)
    {
    }

    private StargateApiFactory(string environment, string? databasePath)
    {
        _environment = environment;
        _databasePath = databasePath;

        if (databasePath is null)
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
        }
    }

    public static StargateApiFactory WithFileDatabase() =>
        new("Testing", Path.Combine(Path.GetTempPath(), $"stargate-tests-{Guid.NewGuid():N}.db"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<StargateContext>>();
            services.AddDbContext<StargateContext>(options =>
            {
                if (_connection is not null)
                {
                    options.UseSqlite(_connection);
                }
                else
                {
                    options.UseSqlite($"Data Source={_databasePath}");
                }
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        _connection?.Dispose();

        if (_databasePath is not null)
        {
            SqliteConnection.ClearAllPools();
            File.Delete(_databasePath);
        }
    }
}
