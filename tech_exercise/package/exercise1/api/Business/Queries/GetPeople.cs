using MediatR;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Business.Dtos;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Queries
{
    public class GetPeople : IRequest<GetPeopleResult>
    {
    }

    public class GetPeopleHandler : IRequestHandler<GetPeople, GetPeopleResult>
    {
        private readonly StargateContext _context;

        public GetPeopleHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<GetPeopleResult> Handle(GetPeople request, CancellationToken cancellationToken)
        {
            var people = await _context.People.AsNoTracking()
                .OrderBy(z => z.Name)
                .Select(PersonAstronaut.Projection)
                .ToListAsync(cancellationToken);

            return new GetPeopleResult()
            {
                People = people
            };
        }
    }

    public class GetPeopleResult : BaseResponse
    {
        public List<PersonAstronaut> People { get; set; } = new List<PersonAstronaut>();
    }
}
