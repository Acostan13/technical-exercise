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
    public class CreatePerson : IRequest<CreatePersonResult>
    {
        private string _name = string.Empty;

        public string Name { get => _name; set => _name = Normalize.Text(value); }
    }

    public class CreatePersonValidator : AbstractValidator<CreatePerson>
    {
        public CreatePersonValidator()
        {
            RuleFor(x => x.Name).ValidPersonName();
        }
    }

    public class CreatePersonPreProcessor : IRequestPreProcessor<CreatePerson>
    {
        private readonly StargateContext _context;

        public CreatePersonPreProcessor(StargateContext context)
        {
            _context = context;
        }

        public async Task Process(CreatePerson request, CancellationToken cancellationToken)
        {
            var nameTaken = await _context.People
                .AnyAsync(z => z.Name == request.Name, cancellationToken);

            if (nameTaken)
            {
                throw new ConflictException($"A person named '{request.Name}' already exists.");
            }
        }
    }

    public class CreatePersonHandler : IRequestHandler<CreatePerson, CreatePersonResult>
    {
        private readonly StargateContext _context;

        public CreatePersonHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<CreatePersonResult> Handle(CreatePerson request, CancellationToken cancellationToken)
        {
            var newPerson = new Person()
            {
                Name = request.Name
            };

            await _context.People.AddAsync(newPerson, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return new CreatePersonResult()
            {
                Id = newPerson.Id
            };
        }
    }

    public class CreatePersonResult : BaseResponse
    {
        public int Id { get; set; }
    }
}
