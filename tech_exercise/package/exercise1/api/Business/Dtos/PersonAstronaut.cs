using StargateAPI.Business.Data;
using System.Linq.Expressions;

namespace StargateAPI.Business.Dtos
{
    /// <summary>
    /// A Person together with their current astronaut information. The astronaut fields are null
    /// for a Person who has never had an astronaut assignment.
    /// </summary>
    public class PersonAstronaut
    {
        public int PersonId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? CurrentRank { get; set; }

        public string? CurrentDutyTitle { get; set; }

        public DateTime? CareerStartDate { get; set; }

        public DateTime? CareerEndDate { get; set; }

        /// <summary>
        /// Single, reusable projection translated to SQL by EF Core (a LEFT JOIN onto AstronautDetail).
        /// </summary>
        public static readonly Expression<Func<Person, PersonAstronaut>> Projection = person => new PersonAstronaut
        {
            PersonId = person.Id,
            Name = person.Name,
            CurrentRank = person.AstronautDetail != null ? person.AstronautDetail.CurrentRank : null,
            CurrentDutyTitle = person.AstronautDetail != null ? person.AstronautDetail.CurrentDutyTitle : null,
            CareerStartDate = person.AstronautDetail != null ? person.AstronautDetail.CareerStartDate : null,
            CareerEndDate = person.AstronautDetail != null ? person.AstronautDetail.CareerEndDate : null
        };
    }
}
