using FluentValidation;
using MediatR;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Business.Exceptions;
using StargateAPI.Business.Responses;
using StargateAPI.Business.Validation;

namespace StargateAPI.Business.Commands
{
    /// <summary>
    /// Updates the Person identified by <see cref="CurrentName"/>. Name is the only mutable attribute
    /// of a Person (astronaut data is maintained through Astronaut Duties), so an update is a rename.
    /// </summary>
    public class UpdatePerson : IRequest<UpdatePersonResult>
    {
        private string _currentName = string.Empty;
        private string _name = string.Empty;

        public required string CurrentName { get => _currentName; set => _currentName = Normalize.Text(value); }

        public required string Name { get => _name; set => _name = Normalize.Text(value); }
    }

    public class UpdatePersonValidator : AbstractValidator<UpdatePerson>
    {
        public UpdatePersonValidator()
        {
            RuleFor(x => x.CurrentName).NotEmpty().WithMessage("The name of the person to update is required.");
            RuleFor(x => x.Name).ValidPersonName();
        }
    }

    public class UpdatePersonPreProcessor : IRequestPreProcessor<UpdatePerson>
    {
        private readonly StargateContext _context;

        public UpdatePersonPreProcessor(StargateContext context)
        {
            _context = context;
        }

        public async Task Process(UpdatePerson request, CancellationToken cancellationToken)
        {
            var person = await _context.People.AsNoTracking()
                .FirstOrDefaultAsync(z => z.Name == request.CurrentName, cancellationToken)
                ?? throw NotFoundException.Person(request.CurrentName);

            var nameTakenByAnother = await _context.People
                .AnyAsync(z => z.Name == request.Name && z.Id != person.Id, cancellationToken);

            if (nameTakenByAnother)
            {
                throw new ConflictException($"A person named '{request.Name}' already exists.");
            }
        }
    }

    public class UpdatePersonHandler : IRequestHandler<UpdatePerson, UpdatePersonResult>
    {
        private readonly StargateContext _context;

        public UpdatePersonHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<UpdatePersonResult> Handle(UpdatePerson request, CancellationToken cancellationToken)
        {
            var person = await _context.People
                .FirstOrDefaultAsync(z => z.Name == request.CurrentName, cancellationToken)
                ?? throw NotFoundException.Person(request.CurrentName);

            person.Name = request.Name;

            await _context.SaveChangesAsync(cancellationToken);

            return new UpdatePersonResult()
            {
                Id = person.Id
            };
        }
    }

    public class UpdatePersonResult : BaseResponse
    {
        public int Id { get; set; }
    }
}
