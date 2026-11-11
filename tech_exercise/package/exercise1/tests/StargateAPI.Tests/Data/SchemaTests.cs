using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Tests.Infrastructure;

namespace StargateAPI.Tests.Data;

public class SchemaTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    [Fact]
    public void Migrations_ApplyCleanly()
    {
        using var context = _database.CreateContext();

        Assert.NotEmpty(context.Database.GetAppliedMigrations());
        Assert.Empty(context.Database.GetPendingMigrations());
    }

    [Fact]
    public async Task PersonName_IsUnique_AtTheDatabaseLevel()
    {
        await using (var context = _database.CreateContext())
        {
            context.People.Add(new Person { Name = "John Doe" });
            await context.SaveChangesAsync();
        }

        await using (var context = _database.CreateContext())
        {
            context.People.Add(new Person { Name = "John Doe" });

            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task AstronautDetail_IsLimitedToOnePerPerson()
    {
        int personId;
        await using (var context = _database.CreateContext())
        {
            var person = new Person { Name = "Jane Doe" };
            context.People.Add(person);
            context.AstronautDetails.Add(new AstronautDetail
            {
                Person = person,
                CurrentRank = "1LT",
                CurrentDutyTitle = "Commander",
                CareerStartDate = new DateTime(2020, 1, 1)
            });
            await context.SaveChangesAsync();
            personId = person.Id;
        }

        await using (var context = _database.CreateContext())
        {
            context.AstronautDetails.Add(new AstronautDetail
            {
                PersonId = personId,
                CurrentRank = "CPT",
                CurrentDutyTitle = "Pilot",
                CareerStartDate = new DateTime(2021, 1, 1)
            });

            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }
    }

    public void Dispose() => _database.Dispose();
}
