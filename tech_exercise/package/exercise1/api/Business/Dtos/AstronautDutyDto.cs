using StargateAPI.Business.Data;
using System.Linq.Expressions;

namespace StargateAPI.Business.Dtos
{
    /// <summary>
    /// API representation of an Astronaut Duty. Decouples the response contract from the EF entity
    /// (and its navigation properties).
    /// </summary>
    public class AstronautDutyDto
    {
        public int Id { get; set; }

        public int PersonId { get; set; }

        public string Rank { get; set; } = string.Empty;

        public string DutyTitle { get; set; } = string.Empty;

        public DateTime DutyStartDate { get; set; }

        public DateTime? DutyEndDate { get; set; }

        public static readonly Expression<Func<AstronautDuty, AstronautDutyDto>> Projection = duty => new AstronautDutyDto
        {
            Id = duty.Id,
            PersonId = duty.PersonId,
            Rank = duty.Rank,
            DutyTitle = duty.DutyTitle,
            DutyStartDate = duty.DutyStartDate,
            DutyEndDate = duty.DutyEndDate
        };
    }
}
