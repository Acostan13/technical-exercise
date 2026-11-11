using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;

namespace StargateAPI.Tests.Infrastructure;

/// <summary>
/// Provides an isolated, in-memory SQLite database per test with the real migrations applied.
/// SQLite is used instead of the EF InMemory provider so that constraints, unique indexes, collations
/// and transactions behave exactly as they do in the running application.
/// </summary>
public sealed class SqliteTestDatabase : IDisposable
{
    public SqliteTestDatabase()
    {
        // The in-memory database lives only as long as this connection stays open.
        Connection = new SqliteConnection("DataSource=:memory:");
        Connection.Open();

        using var context = CreateContext();
        context.Database.Migrate();
    }

    public SqliteConnection Connection { get; }

    /// <summary>
    /// Creates a new context over the shared connection. Use separate contexts for
    /// arrange / act / assert to avoid false positives from the change tracker.
    /// </summary>
    public StargateContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StargateContext>()
            .UseSqlite(Connection)
            .Options;

        return new StargateContext(options);
    }

    public void Dispose() => Connection.Dispose();
}
