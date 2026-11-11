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
    public class GetPersonByName : IRequest<GetPersonByNameResult>
    {
        private string _name = string.Empty;

        public required string Name { get => _name; set => _name = Normalize.Text(value); }
    }

    public class GetPersonByNameValidator : AbstractValidator<GetPersonByName>
    {
        public GetPersonByNameValidator()
        {
            RuleFor(x => x.Name).ValidPersonName();
        }
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
                ?? throw NotFoundException.Person(request.Name);

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
