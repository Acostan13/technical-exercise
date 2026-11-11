using MediatR;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Business.Rules;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Commands
{
    public class CreateAstronautDuty : IRequest<CreateAstronautDutyResult>
    {
        public required string Name { get; set; }

        public required string Rank { get; set; }

        public required string DutyTitle { get; set; }

        public DateTime DutyStartDate { get; set; }
    }

    /// <summary>
    /// Verifies the request is consistent with the Person's existing duty history before it is handled.
    /// </summary>
    public class CreateAstronautDutyPreProcessor : IRequestPreProcessor<CreateAstronautDuty>
    {
        private readonly StargateContext _context;

        public CreateAstronautDutyPreProcessor(StargateContext context)
        {
            _context = context;
        }

        public async Task Process(CreateAstronautDuty request, CancellationToken cancellationToken)
        {
            var person = await _context.People.AsNoTracking()
                .FirstOrDefaultAsync(z => z.Name == request.Name, cancellationToken);

            if (person is null)
            {
                throw new BadHttpRequestException(
                    $"No person named '{request.Name}' was found.", StatusCodes.Status404NotFound);
            }

            var dutyStartDate = request.DutyStartDate.Date;

            // Scoped to this Person: other people may legitimately hold the same title on the same date.
            var isDuplicate = await _context.AstronautDuties.AsNoTracking()
                .AnyAsync(z => z.PersonId == person.Id
                               && z.DutyTitle == request.DutyTitle
                               && z.DutyStartDate == dutyStartDate, cancellationToken);

            if (isDuplicate)
            {
                throw new BadHttpRequestException(
                    $"'{request.Name}' already has a '{request.DutyTitle}' duty starting {dutyStartDate:yyyy-MM-dd}.",
                    StatusCodes.Status409Conflict);
            }

            // The previous duty ends the day before the new one starts, so the new duty must start
            // after the current one. Otherwise the previous duty would end before it began.
            var latestDutyStartDate = await _context.AstronautDuties.AsNoTracking()
                .Where(z => z.PersonId == person.Id)
                .OrderByDescending(z => z.DutyStartDate)
                .Select(z => (DateTime?)z.DutyStartDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestDutyStartDate is not null && dutyStartDate <= latestDutyStartDate.Value)
            {
                throw new BadHttpRequestException(
                    $"The duty start date must be after the current duty start date ({latestDutyStartDate:yyyy-MM-dd}).",
                    StatusCodes.Status400BadRequest);
            }
        }
    }

    public class CreateAstronautDutyHandler : IRequestHandler<CreateAstronautDuty, CreateAstronautDutyResult>
    {
        private readonly StargateContext _context;

        public CreateAstronautDutyHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<CreateAstronautDutyResult> Handle(CreateAstronautDuty request, CancellationToken cancellationToken)
        {
            var person = await _context.People
                .Include(z => z.AstronautDetail)
                .FirstOrDefaultAsync(z => z.Name == request.Name, cancellationToken)
                ?? throw new BadHttpRequestException(
                    $"No person named '{request.Name}' was found.", StatusCodes.Status404NotFound);

            var dutyStartDate = request.DutyStartDate.Date;
            var isRetirement = DutyTitles.IsRetired(request.DutyTitle);
            var dutyTitle = isRetirement ? DutyTitles.Retired : request.DutyTitle;

            // Rule: the Previous Duty End Date is the day before the New Astronaut Duty Start Date.
            // Closing the previous duty and opening the new one happen in one transaction; they are saved
            // in two steps so the "one current duty per person" index is never violated mid-statement.
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var currentDuty = await _context.AstronautDuties
                .Where(z => z.PersonId == person.Id && z.DutyEndDate == null)
                .OrderByDescending(z => z.DutyStartDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (currentDuty is not null)
            {
                currentDuty.DutyEndDate = dutyStartDate.AddDays(-1);
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Rule: a Person's Current Duty has no Duty End Date.
            var newAstronautDuty = new AstronautDuty()
            {
                PersonId = person.Id,
                Rank = request.Rank,
                DutyTitle = dutyTitle,
                DutyStartDate = dutyStartDate,
                DutyEndDate = null
            };

            await _context.AstronautDuties.AddAsync(newAstronautDuty, cancellationToken);

            // Rule: a Person's current astronaut information is stored in the Astronaut Detail table,
            // and they only ever hold one current Duty Title, Start Date and Rank at a time.
            var astronautDetail = person.AstronautDetail;

            if (astronautDetail is null)
            {
                astronautDetail = new AstronautDetail()
                {
                    PersonId = person.Id,
                    CareerStartDate = dutyStartDate
                };

                await _context.AstronautDetails.AddAsync(astronautDetail, cancellationToken);
            }

            astronautDetail.CurrentDutyTitle = dutyTitle;
            astronautDetail.CurrentRank = request.Rank;

            // Rule: a Person's Career End Date is one day before the Retired Duty Start Date.
            // A non-retirement duty clears it, keeping Astronaut Detail consistent with the current duty.
            astronautDetail.CareerEndDate = isRetirement ? dutyStartDate.AddDays(-1) : null;

            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new CreateAstronautDutyResult()
            {
                Id = newAstronautDuty.Id
            };
        }
    }

    public class CreateAstronautDutyResult : BaseResponse
    {
        public int? Id { get; set; }
    }
}
