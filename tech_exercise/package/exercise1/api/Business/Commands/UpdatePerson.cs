using MediatR;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Commands
{
    /// <summary>
    /// Updates the Person identified by <see cref="CurrentName"/>. Name is the only mutable attribute
    /// of a Person (astronaut data is maintained through Astronaut Duties), so an update is a rename.
    /// </summary>
    public class UpdatePerson : IRequest<UpdatePersonResult>
    {
        public required string CurrentName { get; set; } = string.Empty;

        public required string Name { get; set; } = string.Empty;
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
                .FirstOrDefaultAsync(z => z.Name == request.CurrentName, cancellationToken);

            if (person is null)
            {
                throw new BadHttpRequestException(
                    $"No person named '{request.CurrentName}' was found.", StatusCodes.Status404NotFound);
            }

            var nameTakenByAnother = await _context.People
                .AnyAsync(z => z.Name == request.Name && z.Id != person.Id, cancellationToken);

            if (nameTakenByAnother)
            {
                throw new BadHttpRequestException(
                    $"A person named '{request.Name}' already exists.", StatusCodes.Status409Conflict);
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
                ?? throw new BadHttpRequestException(
                    $"No person named '{request.CurrentName}' was found.", StatusCodes.Status404NotFound);

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
