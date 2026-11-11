using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Business.Dtos;
using StargateAPI.Business.Exceptions;
using StargateAPI.Business.Responses;
using StargateAPI.Business.Validation;

namespace StargateAPI.Business.Queries
{
    public class GetAstronautDutiesByName : IRequest<GetAstronautDutiesByNameResult>
    {
        private string _name = string.Empty;

        public required string Name { get => _name; set => _name = Normalize.Text(value); }
    }

    public class GetAstronautDutiesByNameValidator : AbstractValidator<GetAstronautDutiesByName>
    {
        public GetAstronautDutiesByNameValidator()
        {
            RuleFor(x => x.Name).ValidPersonName();
        }
    }

    public class GetAstronautDutiesByNameHandler : IRequestHandler<GetAstronautDutiesByName, GetAstronautDutiesByNameResult>
    {
        private readonly StargateContext _context;

        public GetAstronautDutiesByNameHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<GetAstronautDutiesByNameResult> Handle(GetAstronautDutiesByName request, CancellationToken cancellationToken)
        {
            var person = await _context.People.AsNoTracking()
                .Where(z => z.Name == request.Name)
                .Select(PersonAstronaut.Projection)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw NotFoundException.Person(request.Name);

            // A Person with no astronaut assignments returns an empty list rather than an error.
            var duties = await _context.AstronautDuties.AsNoTracking()
                .Where(z => z.PersonId == person.PersonId)
                .OrderByDescending(z => z.DutyStartDate)
                .Select(AstronautDutyDto.Projection)
                .ToListAsync(cancellationToken);

            return new GetAstronautDutiesByNameResult()
            {
                Person = person,
                AstronautDuties = duties
            };
        }
    }

    public class GetAstronautDutiesByNameResult : BaseResponse
    {
        public PersonAstronaut? Person { get; set; }

        public List<AstronautDutyDto> AstronautDuties { get; set; } = new List<AstronautDutyDto>();
    }
}
