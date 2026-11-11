using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;

namespace StargateAPI.Tests.Infrastructure;

/// <summary>
/// Provides an isolated, in-memory SQLite database per test with the real migrations applied.
/// SQLite is used instead of the EF InMemory provider so that raw SQL, constraints and
/// unique indexes behave exactly as they do in the running application.
/// </summary>
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteTestDatabase()
    {
        // The in-memory database lives only as long as this connection stays open.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.Migrate();
    }

    /// <summary>
    /// Creates a new context over the shared connection. Use separate contexts for
    /// arrange / act / assert to avoid false positives from the change tracker.
    /// </summary>
    public StargateContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StargateContext>()
            .UseSqlite(_connection)
            .Options;

        return new StargateContext(options);
    }

    public void Dispose() => _connection.Dispose();
}
