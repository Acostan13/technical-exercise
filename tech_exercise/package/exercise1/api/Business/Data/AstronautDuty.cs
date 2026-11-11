using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace StargateAPI.Business.Data
{
    [Table("AstronautDuty")]
    public class AstronautDuty
    {
        public int Id { get; set; }

        public int PersonId { get; set; }

        public string Rank { get; set; } = string.Empty;

        public string DutyTitle { get; set; } = string.Empty;

        public DateTime DutyStartDate { get; set; }

        public DateTime? DutyEndDate { get; set; }

        public virtual Person Person { get; set; } = null!;
    }

    public class AstronautDutyConfiguration : IEntityTypeConfiguration<AstronautDuty>
    {
        public void Configure(EntityTypeBuilder<AstronautDuty> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            // Rules: a Person holds only one current duty at a time, and the current duty has no end date.
            // Enforced in the database so concurrent requests cannot create two open-ended duties.
            builder.HasIndex(x => x.PersonId, "IX_AstronautDuty_PersonId_Current")
                .IsUnique()
                .HasFilter("\"DutyEndDate\" IS NULL");

            // A Person cannot start two duties on the same day.
            builder.HasIndex(x => new { x.PersonId, x.DutyStartDate }, "IX_AstronautDuty_PersonId_DutyStartDate")
                .IsUnique();
        }
    }
}
