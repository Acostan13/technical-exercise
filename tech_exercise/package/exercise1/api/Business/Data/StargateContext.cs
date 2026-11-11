using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Exceptions;

namespace StargateAPI.Business.Data
{
    public class StargateContext : DbContext
    {
        private const int SqliteConstraintErrorCode = 19;

        public const string ConstraintConflictMessage = "The request conflicts with existing data. Refresh and try again.";

        public DbSet<Person> People { get; set; }
        public DbSet<AstronautDetail> AstronautDetails { get; set; }
        public DbSet<AstronautDuty> AstronautDuties { get; set; }
        public DbSet<ProcessLog> ProcessLogs { get; set; }

        public StargateContext(DbContextOptions<StargateContext> options)
        : base(options)
        {
        }

        /// <summary>
        /// Pre-processors reject duplicates up front, but two concurrent requests can both pass those checks
        /// before either saves. The database constraints then reject the later one; that is a business
        /// conflict (409), not an unexpected failure, so it is surfaced as a <see cref="ConflictException"/>.
        /// </summary>
        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            try
            {
                return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: SqliteConstraintErrorCode })
            {
                throw new ConflictException(ConstraintConflictMessage, ex);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(StargateContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}
