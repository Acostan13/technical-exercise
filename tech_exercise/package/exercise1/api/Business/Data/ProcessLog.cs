using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace StargateAPI.Business.Data
{
    public enum ProcessLogLevel
    {
        Information,
        Warning,
        Error
    }

    /// <summary>
    /// A record of one business process (MediatR request): what was requested, whether it succeeded,
    /// how long it took and, on failure, why.
    /// </summary>
    [Table("ProcessLog")]
    public class ProcessLog
    {
        public const int RequestDataMaxLength = 4000;

        public int Id { get; set; }

        public DateTime TimestampUtc { get; set; }

        public ProcessLogLevel Level { get; set; }

        public string RequestName { get; set; } = string.Empty;

        public bool Succeeded { get; set; }

        public string Message { get; set; } = string.Empty;

        public string? RequestData { get; set; }

        public string? ExceptionType { get; set; }

        public string? ExceptionDetail { get; set; }

        public long DurationMs { get; set; }
    }

    public class ProcessLogConfiguration : IEntityTypeConfiguration<ProcessLog>
    {
        public void Configure(EntityTypeBuilder<ProcessLog> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();
            // SQLite does not store DateTimeKind; mark values read back as UTC so they serialize with a 'Z'
            // suffix and clients do not misinterpret them as local time.
            builder.Property(x => x.TimestampUtc)
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            builder.Property(x => x.Level).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.RequestName).HasMaxLength(200);
            builder.HasIndex(x => x.TimestampUtc);
        }
    }
}
