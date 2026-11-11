using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Business.Logging;
using StargateAPI.Business.Responses;

namespace StargateAPI.Business.Queries
{
    /// <summary>
    /// Retrieves the most recent process log entries. Excluded from the process log itself so that reading
    /// the log does not add to it.
    /// </summary>
    public class GetProcessLogs : IRequest<GetProcessLogsResult>, IExcludeFromProcessLog
    {
        public const int DefaultTake = 50;
        public const int MaxTake = 500;

        public int Take { get; set; } = DefaultTake;
    }

    public class GetProcessLogsValidator : AbstractValidator<GetProcessLogs>
    {
        public GetProcessLogsValidator()
        {
            RuleFor(x => x.Take).InclusiveBetween(1, GetProcessLogs.MaxTake)
                .WithMessage($"Take must be between 1 and {GetProcessLogs.MaxTake}.");
        }
    }

    public class GetProcessLogsHandler : IRequestHandler<GetProcessLogs, GetProcessLogsResult>
    {
        private readonly StargateContext _context;

        public GetProcessLogsHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<GetProcessLogsResult> Handle(GetProcessLogs request, CancellationToken cancellationToken)
        {
            var logs = await _context.ProcessLogs.AsNoTracking()
                .OrderByDescending(z => z.TimestampUtc)
                .ThenByDescending(z => z.Id)
                .Take(request.Take)
                .ToListAsync(cancellationToken);

            return new GetProcessLogsResult()
            {
                ProcessLogs = logs
            };
        }
    }

    public class GetProcessLogsResult : BaseResponse
    {
        public List<ProcessLog> ProcessLogs { get; set; } = new List<ProcessLog>();
    }
}
