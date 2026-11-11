using FluentValidation;
using MediatR;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Business.Exceptions;
using StargateAPI.Business.Responses;
using StargateAPI.Business.Rules;
using StargateAPI.Business.Validation;

namespace StargateAPI.Business.Commands
{
    public class CreateAstronautDuty : IRequest<CreateAstronautDutyResult>
    {
        private string _name = string.Empty;
        private string _rank = string.Empty;
        private string _dutyTitle = string.Empty;

        public string Name { get => _name; set => _name = Normalize.Text(value); }

        public string Rank { get => _rank; set => _rank = Normalize.Text(value); }

        public string DutyTitle { get => _dutyTitle; set => _dutyTitle = Normalize.Text(value); }

        public DateTime DutyStartDate { get; set; }
    }

    public class CreateAstronautDutyValidator : AbstractValidator<CreateAstronautDuty>
    {
        public CreateAstronautDutyValidator()
        {
            RuleFor(x => x.Name).ValidPersonName();
            RuleFor(x => x.Rank).ValidRank();
            RuleFor(x => x.DutyTitle).ValidDutyTitle();

            // An omitted date deserializes to DateTime.MinValue; reject it rather than store 0001-01-01.
            RuleFor(x => x.DutyStartDate).NotEqual(default(DateTime)).WithMessage("Duty Start Date is required.");
        }
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
                .FirstOrDefaultAsync(z => z.Name == request.Name, cancellationToken)
                ?? throw NotFoundException.Person(request.Name);

            var dutyStartDate = request.DutyStartDate.Date;

            // Scoped to this Person: other people may legitimately hold the same title on the same date.
            var isDuplicate = await _context.AstronautDuties.AsNoTracking()
                .AnyAsync(z => z.PersonId == person.Id
                               && z.DutyTitle == request.DutyTitle
                               && z.DutyStartDate == dutyStartDate, cancellationToken);

            if (isDuplicate)
            {
                throw new ConflictException(
                    $"'{request.Name}' already has a '{request.DutyTitle}' duty starting {dutyStartDate:yyyy-MM-dd}.");
            }

            await DutyHistory.EnsureStartsAfterLatestDutyAsync(_context, person.Id, dutyStartDate, cancellationToken);
        }
    }

    internal static class DutyHistory
    {
        /// <summary>
        /// The previous duty ends the day before the new one starts, so a new duty must start after the
        /// Person's latest duty. Otherwise the previous duty would end before it began.
        /// </summary>
        public static async Task EnsureStartsAfterLatestDutyAsync(
            StargateContext context, int personId, DateTime dutyStartDate, CancellationToken cancellationToken)
        {
            var latestDutyStartDate = await context.AstronautDuties.AsNoTracking()
                .Where(z => z.PersonId == personId)
                .OrderByDescending(z => z.DutyStartDate)
                .Select(z => (DateTime?)z.DutyStartDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestDutyStartDate is not null && dutyStartDate <= latestDutyStartDate.Value)
            {
                throw new RequestValidationException(
                    $"Duty Start Date must be after the current duty start date ({latestDutyStartDate:yyyy-MM-dd}).");
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
                ?? throw NotFoundException.Person(request.Name);

            var dutyStartDate = request.DutyStartDate.Date;
            var isRetirement = DutyTitles.IsRetired(request.DutyTitle);
            var dutyTitle = isRetirement ? DutyTitles.Retired : request.DutyTitle;

            // Rule: the Previous Duty End Date is the day before the New Astronaut Duty Start Date.
            // Closing the previous duty and opening the new one happen in one transaction; they are saved
            // in two steps so the "one current duty per person" index is never violated mid-statement.
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            // Re-checked inside the transaction (SQLite BEGIN IMMEDIATE: holds the write lock). The pre-processor
            // check runs before the lock is taken, so a concurrent request may have added a later duty since.
            await DutyHistory.EnsureStartsAfterLatestDutyAsync(_context, person.Id, dutyStartDate, cancellationToken);

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
