using MediatR;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Business.Dtos;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Queries
{
    public class GetPersonByName : IRequest<GetPersonByNameResult>
    {
        public required string Name { get; set; } = string.Empty;
    }

    public class GetPersonByNameHandler : IRequestHandler<GetPersonByName, GetPersonByNameResult>
    {
        private readonly StargateContext _context;

        public GetPersonByNameHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<GetPersonByNameResult> Handle(GetPersonByName request, CancellationToken cancellationToken)
        {
            var person = await _context.People.AsNoTracking()
                .Where(z => z.Name == request.Name)
                .Select(PersonAstronaut.Projection)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new BadHttpRequestException(
                    $"No person named '{request.Name}' was found.", StatusCodes.Status404NotFound);

            return new GetPersonByNameResult()
            {
                Person = person
            };
        }
    }

    public class GetPersonByNameResult : BaseResponse
    {
        public PersonAstronaut? Person { get; set; }
    }
}
